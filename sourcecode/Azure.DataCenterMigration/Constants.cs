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
using System;
using System.Collections.Generic;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// This class contains the variables to store the constant values for Azure.DataCenterMigration.
    /// </summary>
    public static class Constants
    {
        private static Dictionary<ResourceType, int> resourceNameMaxLength ;

        static Constants()
        {
            resourceNameMaxLength = new Dictionary<ResourceType, int>();
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.AffinityGroup, 63);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.StorageAccount, 23);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.CloudService, 63);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.NetworkConfiguration, 63);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.LocalNetworkSite, 63);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.VirtualNetworkSite, 63);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.Deployment, 63);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.VirtualMachine, 63);
            resourceNameMaxLength.Add(DataCenterMigration.ResourceType.VirtualNetwork, 63);        
        }

        /// <summary>
        /// Constant to store 'https://management.core.windows.net/{0}/locations' string.
        /// </summary>
        internal const string ManagementURL = "https://management.core.windows.net/{0}/locations";

        /// <summary>
        /// Constant to store 'https://management.core.windows.net/{0}/rolesizes' string.
        /// </summary>
        internal const string ManagementURLRoleSizes = "https://management.core.windows.net/{0}/rolesizes";

        /// <summary>
        /// Constant to store 'x-ms-version:2010-10-28' string.
        /// </summary>
        internal const string RequestHeaderVersion = "x-ms-version:2010-10-28";

        /// <summary>
        /// Constant to store 'x-ms-version:2013-08-01' string.
        /// </summary>
        internal const string RequestHeaderVersion20130801 = "x-ms-version:2013-08-01";

        /// <summary>
        /// Constant to store 'http://schemas.microsoft.com/windowsazure' string.
        /// </summary>
        internal const string XNameSpace = "http://schemas.microsoft.com/windowsazure";

        /// <summary>
        /// Constant to store 'Location' string.
        /// </summary>
        internal const string StringLocation = "Location";

        /// <summary>
        /// Constant to store 'Name' string.
        /// </summary>
        internal const string StringName = "Name";

        /// <summary>
        /// Constant to store 'RoleSize' string.
        /// </summary>
        internal const string StringRoleSize = "RoleSize";

        /// <summary>
        /// Constant to store 'Cores' string.
        /// </summary>
        internal const string StringCores = "Cores";

        /// <summary>
        /// Constant to store 'app.config file' string.
        /// </summary>
        internal const string AppConfigArguments = "app.config file";

        /// <summary>
        /// Constant to store '{0}-{1}.json' string.
        /// </summary>
        internal const string MetadataFileName = "{0}-{1}.json";

        /// <summary>
        /// Constant to store 'MM-dd-yyyy-HH-mm' string.
        /// </summary>
        internal const string ExportMetadataFileNameFormat = "MM-dd-yyyy-HH-mm";

        /// <summary>
        /// Constant to store 'PersistentVMRole' string.
        /// </summary>
        internal const string PersistentVMRole = "PersistentVMRole";

        /// <summary>
        /// Constant to store 'ResourceNotFound' string.
        /// </summary>
        internal const string ResourceNotFound = "ResourceNotFound";

        /// <summary>
        /// Constant to store 'PublishProfile' string.
        /// </summary>
        internal const string PublishProfile = "PublishProfile";

        /// <summary>
        /// Constant to store 'Url' string.
        /// </summary>
        internal const string StringUrl = "Url";

        /// <summary>
        /// Constant to store 'ManagementCertificate' string.
        /// </summary>
        internal const string ManagementCertificate = "ManagementCertificate";

        /// <summary>
        /// Constant to store 'Subscription' string.
        /// </summary>
        internal const string Subscription = "Subscription";

        /// <summary>
        /// Constant to store 'ServiceManagementUrl' string.
        /// </summary>
        internal const string ServiceManagementUrl = "ServiceManagementUrl";

        /// <summary>
        /// Constant to store 'Id' string.
        /// </summary>
        internal const string Id = "Id";

        /// <summary>
        /// Constant to store '.json' string.
        /// </summary>
        internal const string MetadataFileExtension = ".json";
        
        /// <summary>
        /// Constant to store '.xml' string.
        /// </summary>
        internal const string MapperFileExtension = ".xml";

        /// <summary>
        /// Constant to store '{0}_ImportStatus.json' string.
        /// </summary>
        internal const string MetadataFileNewName = "{0}_ImportStatus.json";


        /// <summary>
        /// Constant to store '^[a-zA-Z]+[\-]?[a-zA-Z0-9]+$' pattern.
        /// </summary>
        internal const string VirtualMachineRegex = @"^[a-zA-Z]+[\-]?[a-zA-Z0-9]+$";

        /// <summary>
        /// Constant to store '^[a-zA-Z0-9]+[\-]?[a-zA-Z0-9]+$' pattern.
        /// </summary>
        internal const string AffinityGroupNameRegex = @"^[a-zA-Z0-9]+[\-]?[a-zA-Z0-9]+$";

        /// <summary>
        /// Constant to store '^[a-zA-Z0-9]+[\-]?[a-zA-Z0-9]+$' pattern.
        /// </summary>
        internal const string ServiceNameRegex = @"^[a-zA-Z0-9]+[\-]?[a-zA-Z0-9]+$";

        /// <summary>
        /// Constant to store '^[a-zA-Z]+[a-zA-Z0-9]*[\-]?[a-zA-Z0-9]+$' pattern.
        /// </summary>
        internal const string NetworkRegex = @"^[a-zA-Z]+[a-zA-Z0-9]*[\-]?[a-zA-Z0-9]+$";

        /// <summary>
        /// Constant to store 'dc' string.
        /// </summary>
        internal const string DestinationPrefixValue = "dc";

        /// <summary>
        /// Constant to store 'https://{0}.blob.core.windows.net/{1}/{2}' string.
        /// </summary>
        internal const string StorageAccountMediaLink = "https://{0}.blob.core.windows.net/{1}/{2}";

        /// <summary>
        /// Constant to store '^[a-z0-9]+$' string.
        /// </summary>
        internal const string StorageAccountNameRegex = @"^[a-z0-9]+$";

        /// <summary>
        /// Constant to store 'NetworkConfiguration' string.
        /// </summary>
        internal const string ConfigurationSetType = "NetworkConfiguration";

        /// <summary>
        /// Constant to store 'StoppedVM' string.
        /// </summary>
        internal const string VMStatusStopped = "StoppedVM";

        /// <summary>
        /// Constant to store 'StoppedDeallocated' string.
        /// </summary>
        internal const string VMStatusStoppedDeallocated = "StoppedDeallocated";

        /// <summary>
        /// Constant to store total stages for export operation.
        /// </summary>
        internal const string ExportTotalStages = "4";

        /// <summary>
        /// Constant to store total stages for import operation.
        /// </summary>
        internal const string ImportTotalStages = "6";

        /// <summary>
        /// Constant to store total stages for rollback operation.
        /// </summary>
        internal const string RollBackTotalStages = "4";

        /// <summary>
        /// Constant to store '..\Logs\' string.
        /// </summary>
        internal const string LogFilePath = @"..\Logs\";

        /// <summary>
        /// Constant to store 'ResourceType' string.
        /// </summary>
        internal const string ResourceType = "ResourceType";

        /// <summary>
        /// Constant to store 'ResourceName' string.
        /// </summary>
        internal const string ResourceName = "ResourceName";

        /// <summary>
        /// Constant to store 'Azure.DataCenterMigration.StringResources' string.
        /// </summary>
        internal const string StringResources = "Azure.DataCenterMigration.StringResources";

        /// <summary>
        /// Constant to store '-' string.
        /// </summary>
        internal const string StringHyphen = "-";

        /// <summary>
        /// Constant to store '5' string.
        /// </summary>
        internal const string RetryCountDefault = "5";

        /// <summary>
        /// Constant to store '3' string.
        /// </summary>
        internal const string MinBackoffDefault = "3";

        /// <summary>
        /// Constant to store '90' string.
        /// </summary>
        internal const string MaxBackoffDefault = "90";

        /// <summary>
        /// Constant to store '90' string.
        /// </summary>
        internal const string DeltaBackoffDefault = "90";

        /// <summary>
        /// Constant to store 'https://management.core.windows.net' string.
        /// </summary>
        internal const string ServiceManagementUrlValue = "https://management.core.windows.net";

        /// <summary>
        /// Constant to store '256' value.
        /// </summary>
        internal const int MaximumLimitAffinityGroups = 256;

        /// <summary>
        /// Constant to store '1 * 30 * 1000 (milliseconds)' value.
        /// </summary>
        internal const int DelayTimeInMilliseconds = 1 * 30 * 1000;

        /// <summary>
        /// Constant to store '1 * 60 * 1000 (milliseconds)' value.
        /// </summary>
        internal const int DelayTimeInMilliseconds_Rollback = 1 * 60 * 1000;
        /// <summary>
        /// Class stores constants specific to Operations.
        /// </summary>
        public static class Operations
        {
            /// <summary>
            /// Constant to store 'Export' string.
            /// </summary>
            public const string Export = "Export";

            /// <summary>
            /// Constant to store 'Import' string.
            /// </summary>
            public const string Import = "Import";

            /// <summary>
            /// Constant to store 'Migrate' string.
            /// </summary>
            public const string Migrate = "Migrate";
        }

        /// <summary>
        /// Class stores constants specific to ResourceNameMapper class.
        /// </summary>
        public static class ResourceNameMapper
        {
            /// <summary>
            /// Constant to store 'SourceName' string.
            /// </summary>
            public const string SourceName = "SourceName";

            /// <summary>
            /// Constant to store 'DestinationName' string.
            /// </summary>
            public const string DestinationName = "DestinationName";

            /// <summary>
            /// Constant to store 'Import' string.
            /// </summary>
            public const string Import = "Import";
        }

        /// <summary>
        /// Class stores constants specific to parameters.
        /// </summary>
        public static class Parameters
        {
            #region For Export
            /// <summary>
            /// Constant to store 'SourceSubscriptionID' string.
            /// </summary>
            public const string SourceSubscriptionID = "SourceSubscriptionID";

            /// <summary>
            /// Constant to store 'SourceDCName' string.
            /// </summary>
            public const string SourceDCName = "SourceDCName";

            /// <summary>
            /// Constant to store 'ExportMetadataFolderPath' string.
            /// </summary>
            public const string ExportMetadataFolderPath = "ExportMetadataFolderPath";

            /// <summary>
            /// Constant to store 'SourcePublishSettingsFilePath' string.
            /// </summary>
            public const string SourcePublishSettingsFilePath = "SourcePublishSettingsFilePath";

            /// <summary>
            /// Constant to store 'GenerateMapperXml' string.
            /// </summary>
            public const string GenerateMapperXml = "GenerateMapperXml";
            #endregion

            #region For Import / Migrate

            /// <summary>
            /// Constant to store 'CertificateThumbprint' string.
            /// </summary>
            public const string SourceCertificateThumbprint = "SourceCertificateThumbprint";

            /// <summary>
            /// Constant to store 'DestinationCertificateThumbprint' string.
            /// </summary>
            public const string DestinationCertificateThumbprint = "DestinationCertificateThumbprint";

            /// <summary>
            /// Constant to store 'DestinationSubscriptionID' string.
            /// </summary>
            public const string DestinationSubscriptionID = "DestinationSubscriptionID";

            /// <summary>
            /// Constant to store 'DestinationDCName' string.
            /// </summary>
            public const string DestinationDCName = "DestinationDCName";

            /// <summary>
            /// Constant to store 'ImportMetadataFilePath' string.
            /// </summary>
            public const string ImportMetadataFilePath = "ImportMetadataFilePath";

            /// <summary>
            /// Constant to store 'MapperXmlFilePath' string.
            /// </summary>
            public const string MapperXmlFilePath = "MapperXmlFilePath";

            /// <summary>
            /// Constant to store 'DestinationPublishSettingsFilePath' string.
            /// </summary>
            public const string DestinationPublishSettingsFilePath = "DestinationPublishSettingsFilePath";

            /// <summary>
            /// Constant to store 'DestinationPrefixName' string.
            /// </summary>
            public const string DestinationPrefixName = "DestinationPrefixName";

            /// <summary>
            /// Constant to store 'RollBackOnFailure' string.
            /// </summary>
            public const string RollBackOnFailure = "RollBackOnFailure";

            /// <summary>
            /// Constant to store 'QuietMode' string.
            /// </summary>
            public const string QuietMode = "QuietMode";

            /// <summary>
            /// Constant to store 'ResumeImport' string.
            /// </summary>
            public const string ResumeImport = "ResumeImport";

            /// <summary>
            /// Constant to store 'RetryCount' string.
            /// </summary>
            public const string RetryCount = "RetryCount";

            /// <summary>
            /// Constant to store 'Operation' string.
            /// </summary>
            public const string Operation = "Operation";

            /// <summary>
            /// Constant to store 'MinBackoff' string.
            /// </summary>
            public const string MinBackoff = "MinBackoff";

            /// <summary>
            /// Constant to store 'MaxBackoff' string.
            /// </summary>
            public const string MaxBackoff = "MaxBackoff";

            /// <summary>
            /// Constant to store 'DeltaBackoff' string.
            /// </summary>
            public const string DeltaBackoff = "DeltaBackoff";

            #endregion
        }

        /// <summary>
        ///  Get maximum length allowed for resource type.
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns>max lenth allowed for resource type. returns -1 if resource type doesnt match in resourceNameMaxLength dictionary</returns>
        internal static int GetMaxLengthForResourceType( ResourceType resourceType)
        {            
             int maxLength;
             if (resourceNameMaxLength.TryGetValue(resourceType, out maxLength))
             {
                 return maxLength;
             }
             else
                 return -1; 
        }
    }

    #region Enumerations
    /// <summary>
    /// Enumeration for resource type to update the resource status in metadata file
    /// </summary>
     enum ResourceType
    {
        DataCenter,
        AffinityGroup,
        StorageAccount,
        CloudService,
        NetworkConfiguration,
        Deployment,
        VirtualMachine,
        VirtualNetwork,
        DnsServer,
        LocalNetworkSite,
        VirtualNetworkSite,
        Blob,
        OSDisk,
        HardDisk,
        None
    };    
    #endregion
}