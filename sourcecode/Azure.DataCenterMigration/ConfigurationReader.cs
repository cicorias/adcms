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

using Azure.DataCenterMigration.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Models;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Class to read the configuration file and convert the configurations into specified class.
    /// </summary>
    internal static class ConfigurationReader
    {
        /// <summary>
        /// Validates and converts input paramters into <see cref="ExportParameters"/> class.
        /// </summary>
        /// <param name="parameters">Collection of input parameters stored in key value pairs 
        /// <example> Operation "Export" SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" SourceDCName "East Asia" 
        /// ExportMetadataFolderPath "D:\\DataCenterMigration" SourcePublishSettingsFilePath  "D:\\PublishSettings.PublishSettings"
        /// QuietMode "True" </example> </param>
        /// <returns>Parameters required for export functionality</returns>
        internal static ExportParameters ValidateAndConvertExportParameters(IDictionary<string, string> parameters)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;

            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            Logger.Info(methodName, ProgressResources.ParameterValidationStarted);

            #region Check for missing required parameters
            // PublishSettingsFilePath / CertificateThumbprint
            if (!parameters.Keys.Contains(Constants.Parameters.SourcePublishSettingsFilePath) && !parameters.Keys.Contains(
                Constants.Parameters.SourceCertificateThumbprint))
            {
                throw new ValidationException(string.Format(StringResources.MissingCredentialsFile, StringResources.Source,
                    Constants.AppConfigArguments));
            }

            // ExportMetadataFolderPath
            if (!parameters.Keys.Contains(Constants.Parameters.ExportMetadataFolderPath))
            {
                throw new ValidationException(string.Format(StringResources.MissingRequiredParameter,
                    Constants.Parameters.ExportMetadataFolderPath, Constants.AppConfigArguments));
            }
            // SourceSubscriptionID
            if (!parameters.Keys.Contains(Constants.Parameters.SourceSubscriptionID))
            {
                throw new ValidationException(string.Format(StringResources.MissingRequiredParameter,
                    Constants.Parameters.SourceSubscriptionID, Constants.AppConfigArguments));
            }

            // SourceDCName
            if (!parameters.Keys.Contains(Constants.Parameters.SourceDCName))
            {
                throw new ValidationException(string.Format(StringResources.MissingRequiredParameter,
                    Constants.Parameters.SourceDCName, Constants.AppConfigArguments));
            }

            #endregion

            #region Check for null or empty values

            // PublishSettingsFilePath
            if ((parameters.ContainsKey(Constants.Parameters.SourcePublishSettingsFilePath) && string.IsNullOrEmpty(parameters[Constants.Parameters.SourcePublishSettingsFilePath]))
                   || (parameters.ContainsKey(Constants.Parameters.SourceCertificateThumbprint) && string.IsNullOrEmpty(parameters[Constants.Parameters.SourceCertificateThumbprint])))
            {
                throw new ValidationException(string.Format(StringResources.MissingCredentialsFile, StringResources.Source, Constants.AppConfigArguments));
            }

            // ExportMetadataFolderPath
            if (string.IsNullOrEmpty(parameters[Constants.Parameters.ExportMetadataFolderPath]))
            {
                throw new ValidationException(string.Format(StringResources.EmptyOrNullParameter, Constants.Parameters.ExportMetadataFolderPath));
            }
            // SourceSubscriptionID
            if (string.IsNullOrEmpty(parameters[Constants.Parameters.SourceSubscriptionID]))
            {
                throw new ValidationException(string.Format(StringResources.EmptyOrNullParameter, Constants.Parameters.SourceSubscriptionID));
            }

            // SourceDCName
            if (string.IsNullOrEmpty(parameters[Constants.Parameters.SourceDCName]))
            {
                throw new ValidationException(string.Format(StringResources.EmptyOrNullParameter, Constants.Parameters.SourceDCName));
            }

            #endregion

            #region Validate parameter's value

            // Validate PublishSettingsFilePath 
            if (parameters.ContainsKey(Constants.Parameters.SourcePublishSettingsFilePath) && !File.Exists(parameters[Constants.Parameters.SourcePublishSettingsFilePath]))
            {
                throw new ValidationException(string.Format(StringResources.InvalidParameterValue, Constants.Parameters.SourcePublishSettingsFilePath),
                    new FileNotFoundException(StringResources.PublishSettingsFilePathParamException));
            }

            // Validate ExportMetadataFolderPath
            string folderPath = parameters[Constants.Parameters.ExportMetadataFolderPath];

            if (!Directory.Exists(parameters[Constants.Parameters.ExportMetadataFolderPath]))
            {
                throw new ValidationException(string.Format(StringResources.InvalidParameterValue, Constants.Parameters.ExportMetadataFolderPath),
                    new DirectoryNotFoundException(StringResources.ExportMetadataFolderPathParamException));
            }

            int retryCount;
            Double minBackOff;
            Double maxBackOff;
            Double deltaBackOff;
            bool generateMapperXmlValue;

            if (!parameters.Keys.Contains(Constants.Parameters.RetryCount))
            {
                retryCount = Int32.Parse(Constants.RetryCountDefault);
            }
            else
            {
                Int32.TryParse(parameters[Constants.Parameters.RetryCount], out retryCount);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.MinBackoff))
            {
                minBackOff = Double.Parse(Constants.MinBackoffDefault);
            }
            else
            {
                Double.TryParse(parameters[Constants.Parameters.MinBackoff], out minBackOff);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.MaxBackoff))
            {
                maxBackOff = Double.Parse(Constants.MaxBackoffDefault);
            }
            else
            {
                Double.TryParse(parameters[Constants.Parameters.MaxBackoff], out maxBackOff);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.DeltaBackoff))
            {
                deltaBackOff = Double.Parse(Constants.DeltaBackoffDefault);
            }
            else
            {
                Double.TryParse(parameters[Constants.Parameters.DeltaBackoff], out deltaBackOff);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.GenerateMapperXml))
            {
                generateMapperXmlValue = false;
            }
            else
            {
                bool.TryParse(parameters[Constants.Parameters.GenerateMapperXml], out generateMapperXmlValue);
            }

            // Validate SourceSubscriptionID
            PublishSettings publishSettingsFile = null;
            PublishSetting subscription = null;
            if (parameters.ContainsKey(Constants.Parameters.SourcePublishSettingsFilePath))
            {
                try
                {
                    var fileContents = File.ReadAllText(parameters[Constants.Parameters.SourcePublishSettingsFilePath]);
                    publishSettingsFile = new PublishSettings(fileContents);
                }
                catch (Exception xmlEx)
                {
                    throw new ValidationException(string.Format(StringResources.XMLParsingException + " =>" + xmlEx.Message,
                        parameters[Constants.Parameters.SourcePublishSettingsFilePath]));
                }

                // Check whether subscriptionId exists in publish settings file
                subscription = publishSettingsFile.Subscriptions.FirstOrDefault(a =>
                    string.Compare(a.Id, parameters[Constants.Parameters.SourceSubscriptionID], StringComparison.InvariantCultureIgnoreCase) == 0);
                if (subscription == null)
                {
                    throw new ValidationException(string.Format(StringResources.SubscriptionIdParamException,
                        parameters[Constants.Parameters.SourceSubscriptionID], parameters[Constants.Parameters.SourcePublishSettingsFilePath]));
                }
            }
            else
            {
                string thumbprint = parameters[Constants.Parameters.SourceCertificateThumbprint];
                X509Certificate2 certificate = GetStoreCertificate(thumbprint);
                if (!certificate.HasPrivateKey)
                {
                    throw new ValidationException(string.Format(
                        StringResources.MissingPrivateKeyInCertificate, StringResources.Source, thumbprint));
                }
                var sourceCredentials = new CertificateCloudCredentials(parameters[Constants.Parameters.SourceSubscriptionID],
                      certificate);
                string sourceSubscriptionName = null;
                using (var client = new ManagementClient(sourceCredentials))
                {
                    BaseParameters baseParams = new BaseParameters()
                    {
                        RetryCount = retryCount,
                        MinBackOff = TimeSpan.FromSeconds(minBackOff),
                        MaxBackOff = TimeSpan.FromSeconds(maxBackOff),
                        DeltaBackOff = TimeSpan.FromSeconds(deltaBackOff)
                    };
                    SubscriptionGetResponse subscriptionResponse = Retry.RetryOperation(() => client.Subscriptions.Get(),
                       baseParams, ResourceType.None);
                    sourceSubscriptionName = subscriptionResponse.SubscriptionName;
                }
                subscription = new PublishSetting()
                {
                    Id = parameters[Constants.Parameters.SourceSubscriptionID],
                    Name = sourceSubscriptionName,
                    ServiceUrl = new Uri(Constants.ServiceManagementUrlValue),
                    Credentials = sourceCredentials
                };
            }


            List<string> locations = ExportDataCenterLocations(subscription.Credentials, retryCount, minBackOff, maxBackOff, deltaBackOff);
            // Check whether SourceDc name exists in subscription
            bool sourceLocationNamePresent = locations.Any((l => string.Compare(l, parameters[Constants.Parameters.SourceDCName],
                StringComparison.CurrentCultureIgnoreCase) == 0));

            if (!sourceLocationNamePresent)
            {
                throw new ValidationException(string.Format(StringResources.DCParamException, parameters[Constants.Parameters.SourceDCName]));
            }

            Logger.Info(methodName, ProgressResources.ParametersValidationCompleted);

            #endregion

            // Stores validated parameters in class.
            ExportParameters exportParams = new ExportParameters()
            {
                SourceDCName = parameters[Constants.Parameters.SourceDCName],
                ExportMetadataFolderPath = Path.Combine(parameters[Constants.Parameters.ExportMetadataFolderPath],
                    string.Format(Constants.MetadataFileName, parameters[Constants.Parameters.SourceDCName],
                    DateTime.Now.ToString(Constants.ExportMetadataFileNameFormat))),
                SourceSubscriptionSettings = subscription,
                RetryCount = retryCount,
                MinBackOff = TimeSpan.FromSeconds(minBackOff),
                MaxBackOff = TimeSpan.FromSeconds(maxBackOff),
                DeltaBackOff = TimeSpan.FromSeconds(deltaBackOff),
                GenerateMapperXml = generateMapperXmlValue,
                DestinationPrefixName = parameters.Keys.Contains(Constants.Parameters.DestinationPrefixName) ?
                                        parameters[Constants.Parameters.DestinationPrefixName].ToLower() : Constants.DestinationPrefixValue,
            };
            return exportParams;
        }

        /// <summary>
        /// Validates and converts input paramters into <see cref="ImportParameters"/> class.
        /// </summary>
        /// <param name="parameters">Collection of input parameters stored in key value pairs 
        /// <example> Operation "Import" SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" 
        /// DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" DestinationDCName "West US" 
        /// SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" 
        /// DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" 
        /// ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" DestinationPrefixName "dc" QuietMode "True" 
        /// RollBackOnFailure "True" ResumeImport "True" </example> </param>
        /// <param name="validateForImport">True if the function will be called for imort functionality. 
        /// False if it is called for Migrate functionality</param>
        /// <returns>Parameters required for import functionality</returns>
        internal static ImportParameters ValidateAndConvertImportParameters(IDictionary<string, string> parameters,
            bool validateForImport)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            Logger.Info(methodName, ProgressResources.ParameterValidationStarted);

            #region Check for missing required parameters
            // SourcePublishSettingsFilePath/ CertificateThumbprint            
            if (!parameters.Keys.Contains(Constants.Parameters.SourcePublishSettingsFilePath) && !parameters.Keys.Contains(Constants.Parameters.SourceCertificateThumbprint))
            {
                throw new ValidationException(string.Format(StringResources.MissingCredentialsFile, StringResources.Source, Constants.AppConfigArguments));
            }

            // DestinationPublishSettingsFilePath
            if (!parameters.Keys.Contains(Constants.Parameters.DestinationPublishSettingsFilePath) && !parameters.Keys.Contains(Constants.Parameters.DestinationCertificateThumbprint))
            {
                throw new ValidationException(string.Format(StringResources.MissingCredentialsFile, StringResources.Destination, Constants.AppConfigArguments));
            }

            // SourceSubscriptionID
            if (!parameters.Keys.Contains(Constants.Parameters.SourceSubscriptionID))
            {
                throw new ValidationException(string.Format(StringResources.MissingRequiredParameter,
                    Constants.Parameters.SourceSubscriptionID, Constants.AppConfigArguments));
            }

            // DestinationSubscriptionID
            if (!parameters.Keys.Contains(Constants.Parameters.DestinationSubscriptionID))
            {
                throw new ValidationException(string.Format(StringResources.MissingRequiredParameter,
                    Constants.Parameters.DestinationSubscriptionID, Constants.AppConfigArguments));
            }

            // DestinationDCName
            if (!parameters.Keys.Contains(Constants.Parameters.DestinationDCName))
            {
                throw new ValidationException(string.Format(StringResources.MissingRequiredParameter,
                    Constants.Parameters.DestinationDCName, Constants.AppConfigArguments));
            }

            //DestinationPrefixName & MapperXmlFilePath.
            if (!validateForImport)
            {
                if (!parameters.Keys.Contains(Constants.Parameters.DestinationPrefixName))
                {
                    throw new ValidationException(string.Format(StringResources.MissingRequiredParameter,
                        Constants.Parameters.DestinationPrefixName, Constants.AppConfigArguments));
                }
            }
            else
            {
                if (!parameters.Keys.Contains(Constants.Parameters.DestinationPrefixName) &&
                    !parameters.Keys.Contains(Constants.Parameters.MapperXmlFilePath))
                {
                    throw new ValidationException(string.Format(StringResources.MissingMapperAndPrefix,
                       StringResources.Destination, Constants.AppConfigArguments));
                }
            }

            #endregion

            #region Check for null or empty values
            //SourcePublishSettingsFilePath            
            if ((parameters.ContainsKey(Constants.Parameters.SourcePublishSettingsFilePath) && string.IsNullOrEmpty(parameters[Constants.Parameters.SourcePublishSettingsFilePath]))
                  || (parameters.ContainsKey(Constants.Parameters.SourceCertificateThumbprint) && string.IsNullOrEmpty(parameters[Constants.Parameters.SourceCertificateThumbprint])))
            {
                throw new ValidationException(string.Format(StringResources.MissingCredentialsFile, StringResources.Source, Constants.AppConfigArguments));
            }

            // DestinationPublishSettingsFilePath
            if ((parameters.ContainsKey(Constants.Parameters.DestinationPublishSettingsFilePath) && string.IsNullOrEmpty(parameters[Constants.Parameters.DestinationPublishSettingsFilePath]))
                  || (parameters.ContainsKey(Constants.Parameters.DestinationCertificateThumbprint) && string.IsNullOrEmpty(parameters[Constants.Parameters.DestinationCertificateThumbprint])))
            {
                throw new ValidationException(string.Format(StringResources.MissingCredentialsFile, StringResources.Destination, Constants.AppConfigArguments));
            }

            // ImportMetadataFilePath
            if (validateForImport && (string.IsNullOrEmpty(parameters[Constants.Parameters.ImportMetadataFilePath])))
            {
                throw new ValidationException(string.Format(StringResources.EmptyOrNullParameter,
                    Constants.Parameters.ImportMetadataFilePath));
            }
            // DestinationSubscriptionID
            if (string.IsNullOrEmpty(parameters[Constants.Parameters.DestinationSubscriptionID]))
            {
                throw new ValidationException(string.Format(StringResources.EmptyOrNullParameter,
                    Constants.Parameters.DestinationSubscriptionID));
            }

            // DestinationDCName
            if (string.IsNullOrEmpty(parameters[Constants.Parameters.DestinationDCName]))
            {
                throw new ValidationException(string.Format(StringResources.EmptyOrNullParameter, Constants.Parameters.DestinationDCName));
            }

            //DestinationPrefixName & MapperXmlFilePath.
            if (!validateForImport)
            {
                if (string.IsNullOrEmpty(parameters[Constants.Parameters.DestinationPrefixName]))
                {
                    throw new ValidationException(string.Format(StringResources.EmptyOrNullParameter, Constants.Parameters.DestinationPrefixName));
                }
            }
            else
            {
                if ((parameters.ContainsKey(Constants.Parameters.DestinationPrefixName) &&
                    string.IsNullOrEmpty(parameters[Constants.Parameters.DestinationPrefixName])) ||
                    (parameters.ContainsKey(Constants.Parameters.MapperXmlFilePath)
                    && string.IsNullOrEmpty(parameters[Constants.Parameters.MapperXmlFilePath])))
                {
                    throw new ValidationException(string.Format(StringResources.MissingMapperAndPrefix,
                       StringResources.Destination, Constants.AppConfigArguments));
                }
            }

            #endregion

            #region Validate parameter's value.

            // Validate SourcePublishSettingsFilePath 
            if ((parameters.ContainsKey(Constants.Parameters.SourcePublishSettingsFilePath) && !File.Exists(parameters[Constants.Parameters.SourcePublishSettingsFilePath])))
            {
                throw new ValidationException(string.Format(StringResources.InvalidParameterValue,
                    Constants.Parameters.SourcePublishSettingsFilePath),
                    new FileNotFoundException(StringResources.PublishSettingsFilePathParamException));
            }

            // Validate DestinationPublishSettingsFilePath 
            if ((parameters.ContainsKey(Constants.Parameters.DestinationPublishSettingsFilePath) && !File.Exists(parameters[Constants.Parameters.DestinationPublishSettingsFilePath])))
            {
                throw new ValidationException(string.Format(StringResources.InvalidParameterValue,
                    Constants.Parameters.DestinationPublishSettingsFilePath),
                    new FileNotFoundException(StringResources.PublishSettingsFilePathParamException));
            }

            string importMapperXmlFilePath = string.Empty;
            string destinationPrefix = string.Empty;
            // Validate MapperXmlFilePath if provided
            if (validateForImport && parameters.ContainsKey(Constants.Parameters.MapperXmlFilePath))
            {
                importMapperXmlFilePath = parameters[Constants.Parameters.MapperXmlFilePath];

                if (!File.Exists(importMapperXmlFilePath))
                {
                    throw new ValidationException(string.Format(StringResources.InvalidParameterValue,
                        Constants.Parameters.MapperXmlFilePath),
                        new FileNotFoundException(StringResources.MapperFilePathParamException));
                }
                if (!Path.GetExtension(importMapperXmlFilePath).Equals(Constants.MapperFileExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ValidationException(string.Format(StringResources.InvalidExtensionMapperFile,
                        Constants.Parameters.MapperXmlFilePath),
                        new FileNotFoundException(StringResources.MapperFilePathParamException));
                }
            }
            destinationPrefix = parameters.Keys.Contains(Constants.Parameters.DestinationPrefixName) ?
                                    parameters[Constants.Parameters.DestinationPrefixName].ToLower() :
                                    Constants.DestinationPrefixValue;

            // Validate ImportMetadataFilePath
            if (validateForImport)
            {
                if (!string.IsNullOrEmpty(parameters[Constants.Parameters.ImportMetadataFilePath]))
                {
                    string filePath = parameters[Constants.Parameters.ImportMetadataFilePath];

                    if (!File.Exists(filePath))
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidParameterValue,
                            Constants.Parameters.ImportMetadataFilePath),
                            new FileNotFoundException(StringResources.ImportMetadataFilePathParamException));
                    }
                    if (!Path.GetExtension(filePath).Equals(Constants.MetadataFileExtension, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidExtensionMetadataFile,
                            Constants.Parameters.ImportMetadataFilePath),
                            new FileNotFoundException(StringResources.ImportMetadataFilePathParamException));
                    }
                }
            }

            bool rollBackBoolValue;
            bool resumeImportBoolValue;
            int retryCount;
            Double minBackOff;
            Double maxBackOff;
            Double deltaBackOff;

            if (!parameters.Keys.Contains(Constants.Parameters.RollBackOnFailure))
            {
                rollBackBoolValue = false;
            }
            else
            {
                bool.TryParse(parameters[Constants.Parameters.RollBackOnFailure], out rollBackBoolValue);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.ResumeImport))
            {
                resumeImportBoolValue = false;
            }
            else
            {
                bool.TryParse(parameters[Constants.Parameters.ResumeImport], out resumeImportBoolValue);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.RetryCount))
            {
                retryCount = Int32.Parse(Constants.RetryCountDefault);
            }
            else
            {
                Int32.TryParse(parameters[Constants.Parameters.RetryCount], out retryCount);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.MinBackoff))
            {
                minBackOff = Double.Parse(Constants.MinBackoffDefault);
            }
            else
            {
                Double.TryParse(parameters[Constants.Parameters.MinBackoff], out minBackOff);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.MaxBackoff))
            {
                maxBackOff = Double.Parse(Constants.MaxBackoffDefault);
            }
            else
            {
                Double.TryParse(parameters[Constants.Parameters.MaxBackoff], out maxBackOff);
            }

            if (!parameters.Keys.Contains(Constants.Parameters.DeltaBackoff))
            {
                deltaBackOff = Double.Parse(Constants.DeltaBackoffDefault);
            }
            else
            {
                Double.TryParse(parameters[Constants.Parameters.DeltaBackoff], out deltaBackOff);
            }
            // Validate SourcePublishSettings File
            PublishSettings sourcePublishSettingsFile = null;
            PublishSetting sourceSubscription = null;

            if (parameters.ContainsKey(Constants.Parameters.SourcePublishSettingsFilePath))
            {
                try
                {
                    var fileContents = File.ReadAllText(parameters[Constants.Parameters.SourcePublishSettingsFilePath]);
                    sourcePublishSettingsFile = new PublishSettings(fileContents);
                }
                catch (Exception xmlEx)
                {
                    throw new ValidationException(string.Format(StringResources.XMLParsingException + " =>" + xmlEx.Message,
                        parameters[Constants.Parameters.SourcePublishSettingsFilePath]));
                }

                // Check whether sourceSubscriptionId exists in publish settings file
                sourceSubscription = sourcePublishSettingsFile.Subscriptions.FirstOrDefault(a =>
                    string.Compare(a.Id, parameters[Constants.Parameters.SourceSubscriptionID], StringComparison.InvariantCultureIgnoreCase) == 0);
                if (sourceSubscription == null)
                {
                    throw new ValidationException(string.Format(StringResources.SubscriptionIdParamException, parameters[Constants.Parameters.SourceSubscriptionID], parameters[Constants.Parameters.SourcePublishSettingsFilePath]));
                }
            }
            else
            {
                string thumbprint = parameters[Constants.Parameters.SourceCertificateThumbprint];
                X509Certificate2 certificate = GetStoreCertificate(thumbprint);
                if (!certificate.HasPrivateKey)
                {
                    throw new ValidationException(string.Format(
                        StringResources.MissingPrivateKeyInCertificate, StringResources.Source, thumbprint));
                }
                var sourceCredentials = new CertificateCloudCredentials(parameters[Constants.Parameters.SourceSubscriptionID],
                      certificate);
                string sourceSubscriptionName = null;
                using (var client = new ManagementClient(sourceCredentials))
                {
                    BaseParameters baseParams = new BaseParameters()
                    {
                        RetryCount = retryCount,
                        MinBackOff = TimeSpan.FromSeconds(minBackOff),
                        MaxBackOff = TimeSpan.FromSeconds(maxBackOff),
                        DeltaBackOff = TimeSpan.FromSeconds(deltaBackOff)
                    };
                    SubscriptionGetResponse subscriptionResponse = Retry.RetryOperation(() => client.Subscriptions.Get(), baseParams, ResourceType.None);
                    sourceSubscriptionName = subscriptionResponse.SubscriptionName;
                }
                sourceSubscription = new PublishSetting()
                {
                    Id = parameters[Constants.Parameters.SourceSubscriptionID],
                    Name = sourceSubscriptionName,
                    ServiceUrl = new Uri(Constants.ServiceManagementUrlValue),
                    Credentials = sourceCredentials
                };
            }

            // Validate DestinationPublishSettings File            
            PublishSettings destPublishSettingsFile = null;
            PublishSetting destSubscription = null;

            if (parameters.ContainsKey(Constants.Parameters.DestinationPublishSettingsFilePath))
            {
                try
                {
                    var fileContents = File.ReadAllText(parameters[Constants.Parameters.DestinationPublishSettingsFilePath]);
                    destPublishSettingsFile = new PublishSettings(fileContents);
                }
                catch (Exception xmlEx)
                {
                    throw new ValidationException(string.Format(StringResources.XMLParsingException + " =>" + xmlEx.Message,
                        parameters[Constants.Parameters.DestinationPublishSettingsFilePath]));
                }
                // Check whether destSubscriptionId exists in publish settings file
                destSubscription = destPublishSettingsFile.Subscriptions.FirstOrDefault(a =>
                    string.Compare(a.Id, parameters[Constants.Parameters.DestinationSubscriptionID], StringComparison.InvariantCultureIgnoreCase) == 0);
                if (destSubscription == null)
                {
                    throw new ValidationException(string.Format(StringResources.SubscriptionIdParamException, parameters[Constants.Parameters.DestinationSubscriptionID], parameters[Constants.Parameters.DestinationPublishSettingsFilePath]));
                }
            }
            else
            {
                string thumbprint = parameters[Constants.Parameters.DestinationCertificateThumbprint];
                X509Certificate2 certificate = GetStoreCertificate(thumbprint);
                if (!certificate.HasPrivateKey)
                {
                    throw new ValidationException(string.Format(
                        StringResources.MissingPrivateKeyInCertificate, StringResources.Destination, thumbprint));
                }
                var destCredentials = new CertificateCloudCredentials(parameters[Constants.Parameters.DestinationSubscriptionID],
                       certificate);
                string destSubscriptionName = null;
                using (var client = new ManagementClient(destCredentials))
                {
                    BaseParameters baseParams = new BaseParameters()
                    {
                        RetryCount = retryCount,
                        MinBackOff = TimeSpan.FromSeconds(minBackOff),
                        MaxBackOff = TimeSpan.FromSeconds(maxBackOff),
                        DeltaBackOff = TimeSpan.FromSeconds(deltaBackOff)
                    };
                    SubscriptionGetResponse subscriptionResponse = Retry.RetryOperation(() => client.Subscriptions.Get(),
                      baseParams, ResourceType.None);
                    destSubscriptionName = subscriptionResponse.SubscriptionName;
                }
                destSubscription = new PublishSetting()
                {
                    Id = parameters[Constants.Parameters.DestinationSubscriptionID],
                    Name = destSubscriptionName,
                    ServiceUrl = new Uri(Constants.ServiceManagementUrlValue),
                    Credentials = destCredentials
                };
            }

            // Check whether DestinationDCName exists in subscription
            List<string> locations = ExportDataCenterLocations(destSubscription.Credentials, retryCount, minBackOff, maxBackOff, deltaBackOff);

            bool destinationLocationNamePresent = locations.Any((l => string.Compare(l,
                 parameters[Constants.Parameters.DestinationDCName],
                 StringComparison.CurrentCultureIgnoreCase) == 0));

            if (!destinationLocationNamePresent)
            {
                throw new ValidationException(string.Format(StringResources.DCParamException,
                    parameters[Constants.Parameters.DestinationDCName]));
            }

            // Valiadte DestinationPrefixName
            if (parameters.ContainsKey(Constants.Parameters.DestinationPrefixName) &&
                (parameters[Constants.Parameters.DestinationPrefixName].Length < 1 || parameters[Constants.Parameters.DestinationPrefixName].Length > 5))
            {
                throw new ValidationException(string.Format(StringResources.InvalidDestinationPrefixName,
                    parameters[Constants.Parameters.DestinationPrefixName]));
            }
            #endregion

            // Stores validated parameters in class.
            ImportParameters importParams = new ImportParameters()
            {
                DestinationDCName = parameters[Constants.Parameters.DestinationDCName],
                ImportMetadataFilePath = (!validateForImport) ? null : parameters[Constants.Parameters.ImportMetadataFilePath],
                MapperXmlFilePath = importMapperXmlFilePath,
                DestinationPrefixName = destinationPrefix,
                DestinationSubscriptionSettings = destSubscription,
                RollBackOnFailure = rollBackBoolValue,
                ResumeImport = resumeImportBoolValue,
                RetryCount = retryCount,
                MinBackOff = TimeSpan.FromSeconds(minBackOff),
                MaxBackOff = TimeSpan.FromSeconds(maxBackOff),
                DeltaBackOff = TimeSpan.FromSeconds(deltaBackOff),
                SourceSubscriptionSettings = sourceSubscription
            };

            Logger.Info(methodName, ProgressResources.ParametersValidationCompleted);
            return importParams;
        }

        /// <summary>
        /// Exports list of SubscriptionId specific dataCenters.
        /// </summary>
        ///<param name="credentials"> Subscription credentials</param>                
        /// <param name="retryCount"> No. of times to retry in case of exception</param>
        /// <param name="minBackOff">Minimum backoff in seconds</param>
        /// <param name="maxBackOff">Maximum backoff in seconds</param>
        /// <param name="deltaBackOff">Delta Backoff in seconds</param>
        /// <returns>List of SubscriptionId specific dataCenters.</returns>
        private static List<string> ExportDataCenterLocations(SubscriptionCloudCredentials credentials, int retryCount,
            double minBackOff, double maxBackOff, double deltaBackOff)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            try
            {
                using (var client = new ManagementClient(credentials))
                {
                    BaseParameters baseParams = new BaseParameters()
                    {
                        RetryCount = retryCount,
                        MinBackOff = TimeSpan.FromSeconds(minBackOff),
                        MaxBackOff = TimeSpan.FromSeconds(maxBackOff),
                        DeltaBackOff = TimeSpan.FromSeconds(deltaBackOff)
                    };

                    // Call management API to get list of locations.
                    var locations = Retry.RetryOperation(() => client.Locations.List(), baseParams, ResourceType.None);
                    Logger.Info(methodName, ProgressResources.ExecutionCompleted);
                    return locations.Select(l => l.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex, ResourceType.None.ToString());
                throw;
            }
        }

        /// <summary>
        /// Get store certificate from thumbprint.
        /// </summary>
        /// <param name="thumbprint">Thumbprint value</param>
        /// <returns>Certificate from specified thumbprint value</returns>
        private static X509Certificate2 GetStoreCertificate(string thumbprint)
        {
            List<StoreLocation> locations = new List<StoreLocation>
              { 
                StoreLocation.CurrentUser, 
                StoreLocation.LocalMachine
              };

            foreach (var location in locations)
            {
                X509Store store = new X509Store(StoreName.My, location);
                try
                {
                    thumbprint = thumbprint.ToUpper();
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    X509Certificate2Collection certificates = store.Certificates.Find(
                      X509FindType.FindByThumbprint, thumbprint, false);
                    if (certificates.Count == 1)
                    {
                        return certificates[0];
                    }
                }
                finally
                {
                    store.Close();
                }
            }
            throw new ValidationException(string.Format(
                StringResources.MissingCertificate, thumbprint));
        }
    }
}

