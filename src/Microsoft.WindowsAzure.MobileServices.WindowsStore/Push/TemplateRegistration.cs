﻿// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

using Windows.Data.Xml.Dom;

using Newtonsoft.Json;

namespace Microsoft.WindowsAzure.MobileServices
{
    /// <summary>
    /// Registration is used to define a target that is registered for notifications. A TemplateRegistration allows the client application
    /// to define the format of the registration.
    /// </summary>
    [JsonObject]
    public sealed class TemplateRegistration : Registration
    {
        private const string WnsTypeName = "X-WNS-Type";

        internal TemplateRegistration()
        {            
        }

        /// <summary>
        /// Create a TemplateRegistration
        /// </summary>
        /// <param name="channelUri">The channel uri</param>
        /// <param name="bodyTemplate">The template xml in string format</param>
        /// <param name="templateName">The template name</param>
        public TemplateRegistration(string channelUri, string bodyTemplate, string templateName)
            : this(channelUri, bodyTemplate, templateName, null, null)
        {
        }

        /// <summary>
        /// Create a TemplateRegistration
        /// </summary>
        /// <param name="channelUri">The channel uri</param>
        /// <param name="bodyTemplate">The template xml in string format</param>
        /// <param name="templateName">The template name</param>
        /// <param name="tags">The tags that restrict which notifications this registration will receive</param>
        public TemplateRegistration(string channelUri, string bodyTemplate, string templateName, IEnumerable<string> tags)
            : this(channelUri, bodyTemplate, templateName, tags, null)
        {
        }

        /// <summary>
        /// Create a TemplateRegistration
        /// </summary>
        /// <param name="channelUri">The channel uri</param>
        /// <param name="bodyTemplate">The template xml in string format</param>
        /// <param name="templateName">The template name</param>
        /// <param name="tags">The tags that restrict which notifications this registration will receive</param>
        /// <param name="additionalHeaders">Additional headers</param>
        public TemplateRegistration(string channelUri, string bodyTemplate, string templateName, IEnumerable<string> tags, IEnumerable<KeyValuePair<string, string>> additionalHeaders)
            : base(channelUri, tags)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentNullException("templateName");
            }

            if (templateName.Equals(Registration.NativeRegistrationName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ConflictWithReservedName, Registration.NativeRegistrationName));
            }

            if (templateName.Contains(":") || templateName.Contains(";"))
            {
                throw new ArgumentException(Resources.InvalidTemplateName);
            }

            if (string.IsNullOrWhiteSpace(bodyTemplate))
            {
                throw new ArgumentNullException("bodyTemplate");
            }

            this.TemplateName = templateName;
            this.WnsHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);            
            if (additionalHeaders != null)
            {
                foreach (var item in additionalHeaders)
                {
                    this.WnsHeaders.Add(item.Key, item.Value);
                }
            }

            this.BodyTemplate = bodyTemplate;

            // We only support xml as bodyTemplate even for wns/raw
            // TODO: Double check we are validating correctly
            if (!this.WnsHeaders.ContainsKey(WnsTypeName))
            {
                XmlDocument xmlDocument = new XmlDocument();
                try
                {
                    xmlDocument.LoadXml(bodyTemplate);                    
                }
                catch (Exception e)
                {
                    // TODO: Resource
                    throw new ArgumentException("Parameter must be valid XML.", "bodyTemplate", e);
                }

                var payloadType = TemplateRegistration.DetectBodyType(xmlDocument);
                if (payloadType != null)
                {
                    this.WnsHeaders.Add(WnsTypeName, payloadType);
                }
                else
                {
                    // TODO: Resource
                    throw new ArgumentException("Cannot autodetect X-WNS type from bodyTemplate: provide a body template with a valid toast/tile/badge content or specify a X-WNS-Type header.");
                }
            }            

            this.WnsHeaders = new ReadOnlyDictionary<string, string>(this.WnsHeaders);
        }

        /// <summary>
        /// Gets headers that should be sent to WNS with the notification
        /// </summary>
        [JsonProperty(PropertyName = "headers")]
        public IDictionary<string, string> WnsHeaders { get; internal set; }        

        /// <summary>
        /// Get templateName
        /// </summary>
        [JsonProperty(PropertyName = "templateName")]
        public string TemplateName { get; internal set; }

        /// <summary>
        /// Gets bodyTemplate as string
        /// </summary>
        [JsonProperty(PropertyName = "templateBody")]
        public string BodyTemplate { get; internal set; }

        private static string DetectBodyType(XmlDocument template)
        {
            TemplateRegistrationType registrationType;
            if (template.FirstChild == null ||
                !Enum.TryParse(template.FirstChild.NodeName, true, out registrationType))
            {
                // First node of the body template should be toast/tile/badge
                throw new ArgumentException(Resources.NotSupportedXMLFormatAsBodyTemplate);
            }

            return "wns/" + template.FirstChild.NodeName.ToLowerInvariant();
        }

        internal override string Name
        {
            get
            {
                return this.TemplateName;
            }
        }
    }

    enum TemplateRegistrationType
    {
        Toast,
        Tile,
        Badge
    }
}