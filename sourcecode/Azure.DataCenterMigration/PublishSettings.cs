/*******************************************************************************
 * Copyright 2014 Persistent Systems Ltd.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 ******************************************************************************/
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Model class for PublishSettings files.
    /// </summary>
    internal class PublishSetting
    {
        /// <summary>
        /// Read-Write property for Subscription ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Read-Write property for Subscription Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Read-Write property for Subscription ServiceUrl
        /// </summary>
        public Uri ServiceUrl { get; set; }

        /// <summary>
        /// Read-Write property for cloud credentials for subscription
        /// </summary>
        public CertificateCloudCredentials Credentials { get; set; }
    }

    /// <summary>
    /// Class retrives information from PublishSettingsFilePath and stores into <see cref="PublishSetting"/>
    /// </summary>
    internal class PublishSettings
    {
        /// <summary>
        /// Constructor class
        /// </summary>
        /// <param name="fileContents">Content of Publish Settings file</param>
        public PublishSettings(string fileContents)
        {
            var document = XDocument.Parse(fileContents);

            var publishProfile = document.Descendants(Constants.PublishProfile).FirstOrDefault();

            Uri defaultServiceUrl = null;
            if (!string.IsNullOrEmpty(Get(publishProfile, Constants.StringUrl)))
            {
                defaultServiceUrl = GetUri(publishProfile, Constants.StringUrl);
            }

            X509Certificate2 defaultCertificate = null;
            if (!string.IsNullOrEmpty(Get(publishProfile, Constants.ManagementCertificate)))
            {
                defaultCertificate = GetCertificate(publishProfile, Constants.ManagementCertificate);
            }

            _subscriptions = document.Descendants(Constants.Subscription)
                .Select(subscription => ToPublishSetting(subscription, defaultServiceUrl, defaultCertificate)).ToList();
        }

        /// <summary>
        /// Retrieves publish settings file details
        /// </summary>
        /// <param name="element">Subscription element of publishSettings file</param>
        /// <param name="defaultServiceUrl">Default service URL of subscription</param>
        /// <param name="defaultCertificate">Default certificate of subscription</param>
        /// <returns></returns>
        private PublishSetting ToPublishSetting(XElement element, Uri defaultServiceUrl, X509Certificate2 defaultCertificate)
        {
            var settings = new PublishSetting();
            settings.Id = Get(element, Constants.Id);
            settings.Name = Get(element, Constants.StringName);
            settings.ServiceUrl = GetUri(element, Constants.ServiceManagementUrl) ?? defaultServiceUrl;
            settings.Credentials = new CertificateCloudCredentials(settings.Id, GetCertificate(element, Constants.ManagementCertificate) ?? defaultCertificate);
            return settings;
        }

        /// <summary>
        /// Gets value of element's attribute 
        /// </summary>
        /// <param name="element">Element whose attribute value has to retrieve</param>
        /// <param name="name">Attribute name</param>
        /// <returns>Value of attribute</returns>
        private string Get(XElement element, string name)
        {
            return (string)element.Attribute(name);
        }

        /// <summary>
        /// Gets Uri value of attribute from Element
        /// </summary>
        /// <param name="element">Element whose URL value has to retrieve</param>
        /// <param name="name">Name of URL attribute</param>
        /// <returns>Uri value of attribute</returns>
        private Uri GetUri(XElement element, string name)
        {
            string value = Get(element, name);
            return string.IsNullOrEmpty(value) ? null : new Uri(value);
        }

        /// <summary>
        /// Gets certificate value of attribute from Element
        /// </summary>
        /// <param name="element">Element whose certificate value has to retrieve</param>
        /// <param name="name">Name of certificate attribute</param>
        /// <returns>Certificate value of attribute</returns>
        private X509Certificate2 GetCertificate(XElement element, string name)
        {
            var encodedData = Get(element, name);
            return string.IsNullOrEmpty(encodedData) ? null : new X509Certificate2(Convert.FromBase64String(encodedData));
        }

        /// <summary>
        /// Readonly property for getting subscription details from PublishSettings file
        /// </summary>
        public IEnumerable<PublishSetting> Subscriptions
        {
            get
            {
                return _subscriptions;
            }
        }

        private readonly IList<PublishSetting> _subscriptions;
    }
}
