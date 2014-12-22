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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Provides methods to Export (Exports source subscription resources), 
    /// Import (Imports exported resources into destination subscription) and 
    /// Migrate (Export + Import) Microsoft Azure resources.
    /// </summary>
    public class DCMigrationManager
    {
        private bool quietMode;
        /// <summary>
        /// Exports information about source subscription and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json format 
        /// on specified ExportMetadataFolderPath.
        /// </summary>
        /// <param name="parameters"> Collection of key value paired input parameters <example> Operation "Export" 
        /// SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" SourceDCName "East Asia" ExportMetadataFolderPath "D:\\DataCenterMigration" 
        /// SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" 
        /// QuietMode "True" GenerateMapperXml "True" RetryCount "5" MinBackoff "3" MaxBackoff "3" DeltaBackoff "90" </example> </param>
        public void ExportSubscriptionMetadata(IDictionary<string, string> parameters)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            bool boolValue;
            if (!parameters.Keys.Contains(Constants.Parameters.QuietMode))
            {
                quietMode = false;
            }
            else
            {
                quietMode = bool.TryParse(parameters[Constants.Parameters.QuietMode], out boolValue) ? boolValue : false;
            }

            ReportProgress(ProgressResources.ExportMetadataStarted);
            Logger.Info(methodName, ProgressResources.ExportMetadataStarted);

            // Validate the input paramters and export them into parameters class.
            ExportParameters exportParameters = ConfigurationReader.ValidateAndConvertExportParameters(parameters);

            // Export metadata.
            ResourceExporter exporter = new ResourceExporter(exportParameters, this);
            Subscription subscription = exporter.ExportSubscriptionMetadata();

            // Export metadata into json file.
            File.WriteAllText(exportParameters.ExportMetadataFolderPath,
               JsonConvert.SerializeObject(subscription, Formatting.Indented));

            if (exportParameters.GenerateMapperXml)
            {
                ResourceNameMapperHelper resourceHelper = new ResourceNameMapperHelper();
                File.WriteAllText(
                    Path.ChangeExtension(exportParameters.ExportMetadataFolderPath, Constants.MapperFileExtension),
                    resourceHelper.GenerateMapperXml(subscription, exportParameters.DestinationPrefixName));
            }
            ReportProgress(string.Format(ProgressResources.ExportMetadataCompleted, exportParameters.ExportMetadataFolderPath));
            Logger.Info(methodName, string.Format(ProgressResources.ExportMetadataCompleted, exportParameters.ExportMetadataFolderPath));
            Logger.Info(methodName, ProgressResources.ExecutionCompleted);
        }

        /// <summary>
        /// Reads exported metadata json file and deploys all the source resources into destination subscription.
        /// </summary>
        /// <param name="parameters"> Collection of key value paired input parameters <example> 
        /// Operation "Import" SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" 
        /// DestinationDCName "West US" SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" 
        /// DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" DestinationCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" 
        /// ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" MapperXmlFilePath "D:\\DataCenterMigration\mydata.xml" DestinationPrefixName "dc" 
        /// RetryCount "5" MinBackoff "3" MaxBackoff "3" DeltaBackoff "90" QuietMode "True" RollBackOnFailure "True" ResumeImport "True" </example></param>
        public void ImportSubscriptionMetadata(IDictionary<string, string> parameters)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            bool boolValue;
            if (!parameters.Keys.Contains(Constants.Parameters.QuietMode))
            {
                quietMode = false;
            }
            else
            {
                quietMode = bool.TryParse(parameters[Constants.Parameters.QuietMode], out boolValue) ? boolValue : false;
            }

            Logger.Info(methodName, string.Format(ProgressResources.ImportMetadataStarted, parameters[Constants.Parameters.ImportMetadataFilePath]));
            ReportProgress(string.Format(ProgressResources.ImportMetadataStarted, parameters[Constants.Parameters.ImportMetadataFilePath]));

            // Validate the input paramters and export them into parameters class.
            ImportParameters importParameters = ConfigurationReader.ValidateAndConvertImportParameters(parameters, true);

            // Import metadata.
            ResourceImporter importer = new ResourceImporter(importParameters, this);
            importer.ImportSubscriptionMetadata();

            ReportProgress(ProgressResources.ImportMetadataCompleted);

            Logger.Info(methodName, ProgressResources.ImportMetadataCompleted);
            Logger.Info(methodName, ProgressResources.ExecutionCompleted);
        }

        /// <summary>
        /// Combination of Export and Import functionality. 
        /// Exports information about source subscription and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json format 
        /// on specified ExportMetadataFolderPath.
        /// Reads exported metadata json file and deploys all the source resources into destination subscription.
        /// </summary>
        /// <param name="parameters"> Collection of key value paired input parameters <example> Operation "Migrate" 
        /// SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" SourceDCName "East Asia" 
        /// DestinationDCName "West US" SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" 
        /// DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" DestinationCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" 
        /// ExportMetadataFolderPath "D:\\DataCenterMigration" DestinationPrefixName "dc" RetryCount "5" MinBackoff "3" MaxBackoff "3" DeltaBackoff "90"
        /// QuietMode "True" RollBackOnFailure "True" </example></param>
        public void MigrateSubscription(IDictionary<string, string> parameters)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            bool boolValue;
            if (!parameters.Keys.Contains(Constants.Parameters.QuietMode))
            {
                quietMode = false;
            }
            else
            {
                quietMode = bool.TryParse(parameters[Constants.Parameters.QuietMode], out boolValue) ? boolValue : false;
            }
            ReportProgress(ProgressResources.ExportMetadataStarted);
            Logger.Info(methodName, ProgressResources.ExportMetadataStarted);

            // Validate the input paramters and export them into parameters class.
            ExportParameters exportParameters = ConfigurationReader.ValidateAndConvertExportParameters(parameters);

            // Validate the input paramters and export them into parameters class.
            ImportParameters importParameters = ConfigurationReader.ValidateAndConvertImportParameters(parameters, false);

            // Export metadata.
            ResourceExporter exporter = new ResourceExporter(exportParameters, this);
            Subscription subscription = exporter.ExportSubscriptionMetadata();

            File.WriteAllText(exportParameters.ExportMetadataFolderPath,
               JsonConvert.SerializeObject(subscription, Formatting.Indented));
            ReportProgress(string.Format(ProgressResources.ExportMetadataCompleted, exportParameters.ExportMetadataFolderPath));
            Logger.Info(methodName, string.Format(ProgressResources.ExportMetadataCompleted, exportParameters.ExportMetadataFolderPath));
            Logger.Info(methodName, ProgressResources.ExecutionCompleted);

            importParameters.ImportMetadataFilePath = exportParameters.ExportMetadataFolderPath;
            // Import metadata.     
            Logger.Info(methodName, string.Format(ProgressResources.ImportMetadataStarted, importParameters.ImportMetadataFilePath));
            ReportProgress(string.Format(ProgressResources.ImportMetadataStarted, importParameters.ImportMetadataFilePath));

            ResourceImporter importer = new ResourceImporter(importParameters, this);
            importer.ImportSubscriptionMetadata();

            ReportProgress(ProgressResources.ImportMetadataCompleted);
            Logger.Info(methodName, ProgressResources.ImportMetadataCompleted);

            Logger.Info(methodName, ProgressResources.ExecutionCompleted);
        }

        /// <summary>
        /// Reports the progress of the operation.
        /// </summary>
        /// <param name="message">Progress Message</param>        
        internal void ReportProgress(string message)
        {
            if (!quietMode && Progress != null)
            {
                Progress(null, new ProgressEventArgs(message));
            }
        }

        /// <summary>
        /// Handles an events occurred in Data Center Migration library.
        /// </summary>
        public event EventHandler<ProgressEventArgs> Progress;
    }

}
