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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Management.Network;
using Microsoft.WindowsAzure.Management.Network.Models;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Class for importing resources.
    /// </summary>
    internal class ResourceImporter
    {
        #region Private Members
        private ImportParameters importParameters;
        private Subscription destSubscriptionMetadata;
        private Subscription sourceSubscriptionMetadata;
        private DCMigrationManager dcMigration;
        private Object thisLockContainer = new Object();
        private Object thisLockFile = new Object();
        private Dictionary<ResourceType, List<ResourceNameMapper>> resourceNameCollection = new Dictionary<ResourceType, List<ResourceNameMapper>>();
        private ResourceNameMapperHelper helper;
        #endregion

        /// <summary>
        /// Constructor class
        /// </summary>
        /// <param name="parameters">Input parameters for import functionality.</param>
        /// <param name="dcMigration">DCMigration class object to report progress.</param>
        public ResourceImporter(ImportParameters parameters, DCMigrationManager dcMigration)
        {
            // Set export parameters.
            importParameters = parameters;
            this.dcMigration = dcMigration;
            helper = new ResourceNameMapperHelper();
        }

        /// <summary>
        /// Import Subscription metadata.
        /// </summary>
        internal void ImportSubscriptionMetadata()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string importFileContents = File.ReadAllText(importParameters.ImportMetadataFilePath);

            try
            {
                Logger.Info(methodName, string.Format(ProgressResources.ImportDataCenterStarted, importParameters.DestinationDCName),
                    ResourceType.DataCenter.ToString(), importParameters.DestinationDCName);
                dcMigration.ReportProgress(string.Format(ProgressResources.ImportDataCenterStarted, importParameters.DestinationDCName));

                // Deserialize the import metadata file contents
                destSubscriptionMetadata = JsonConvert.DeserializeObject<Subscription>(importFileContents);

                // Copy of deserialize data to update metadata file with import status
                sourceSubscriptionMetadata = JsonConvert.DeserializeObject<Subscription>(importFileContents);

                #region Mapper Xml
                if (String.IsNullOrEmpty(importParameters.MapperXmlFilePath))
                {
                    ResourceNameMapperHelper resourceHelper = new ResourceNameMapperHelper();
                    importParameters.MapperXmlFilePath = 
                        Path.ChangeExtension(importParameters.ImportMetadataFilePath, Constants.MapperFileExtension);
                    File.WriteAllText(importParameters.MapperXmlFilePath,
                        resourceHelper.GenerateMapperXml(destSubscriptionMetadata, importParameters.DestinationPrefixName));
                }
                string destinationPrefixValue;

                resourceNameCollection = helper.GetDestinationResourceNames(
                    importParameters.MapperXmlFilePath, out destinationPrefixValue);

                importParameters.DestinationPrefixName = destinationPrefixValue;
                #endregion

                // If Resume Import false, make a copy of metadata file for import status update
                if (!importParameters.ResumeImport)
                {
                    // Generate new metadata file name
                    importParameters.ImportMetadataFilePath = Path.Combine(
                        Path.GetDirectoryName(importParameters.ImportMetadataFilePath),
                        string.Format(Constants.MetadataFileNewName,
                            Path.GetFileNameWithoutExtension(importParameters.ImportMetadataFilePath)));

                    // Make a copy of metadata file
                    File.WriteAllText(importParameters.ImportMetadataFilePath,
                              JsonConvert.SerializeObject(sourceSubscriptionMetadata, Newtonsoft.Json.Formatting.Indented));
                }

                // Validate Metadata file resources.
                dcMigration.ReportProgress(ProgressResources.ValidateMetadataFileResources);
                int stageCount = 1;             
                ChangeAndValidateMetadataFileResources();

                dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, stageCount, Constants.ImportTotalStages));
                Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, stageCount, Constants.ImportTotalStages));
                //Create Destination Resources
                foreach (var datacenter in destSubscriptionMetadata.DataCenters)
                {
                    // Check if all reources are already imported
                    if (!datacenter.IsImported)
                    {
                        AffinityGroupListResponse affinityGroupResponse = GetAffinityGroupListResponseFromMSAzure(
                            importParameters.DestinationSubscriptionSettings.Credentials);
                        // Create affinity groups
                        dcMigration.ReportProgress(ProgressResources.CreateAffinityGroups);
                        CreateAffinityGroups(datacenter.AffinityGroups, affinityGroupResponse);
                        dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ImportTotalStages));
                        Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, stageCount, Constants.ImportTotalStages));

                        // Create storage accounts
                        dcMigration.ReportProgress(ProgressResources.CreateStorageAccounts);
                        CreateStorageAccounts(datacenter.StorageAccounts);
                        dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ImportTotalStages));
                        Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, stageCount, Constants.ImportTotalStages));

                        // Copy all blobs to destination
                        dcMigration.ReportProgress(ProgressResources.CopyAllBlobsToDestination);
                        ShutDownVMsAndCopyBlobToDestination();
                        dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ImportTotalStages));
                        Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, stageCount, Constants.ImportTotalStages));

                        // Create virtual networks with local networks and DNS servers
                        dcMigration.ReportProgress(ProgressResources.CreateVirtualNetworks);
                        CreateVirtualNetworks(datacenter.NetworkConfiguration);
                        dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ImportTotalStages));
                        Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, stageCount, Constants.ImportTotalStages));

                        // Create cloud services and there deployments
                        dcMigration.ReportProgress(ProgressResources.CreateCloudServices);
                        CreateCloudServices(datacenter.CloudServices);
                        dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ImportTotalStages));
                        Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, stageCount, Constants.ImportTotalStages));
                        //  throw new Exception();
                        // Update datacenter status as imported after successful import and update metadata file
                        datacenter.IsImported = true;
                        UpdateMedatadaFile(ResourceType.DataCenter);
                        Logger.Info(methodName, string.Format(ProgressResources.ImportDataCenterCompleted, importParameters.DestinationDCName));
                    }
                }
            }
            catch (ValidationException vex)
            {
                throw vex;
            }
            catch
            {
                // Update Metadata File.
                UpdateMedatadaFile(ResourceType.None);

                if (importParameters.RollBackOnFailure)
                {
                    RollBack rollback = new RollBack(importParameters, sourceSubscriptionMetadata, dcMigration, this);
                    rollback.RollBackResources();
                }
                // If Resume Import false, Inform user about updated import status file
                else if (!importParameters.ResumeImport)
                {
                    dcMigration.ReportProgress(string.Format(ProgressResources.ResumeImport, importParameters.ImportMetadataFilePath));
                }
                throw;
            }
        }

        #region Validations
        /// <summary>
        /// Changes resource name by appending Prefix text and validates the same.
        /// </summary>
        private void ChangeAndValidateMetadataFileResources()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);

            // Validate subscription capacity.
            ValidateSubscriptionCapacity();

            // Get list of affinity groups.
            List<AffinityGroup> affinityGroups = (from datacenter in destSubscriptionMetadata.DataCenters
                                                  select datacenter.AffinityGroups).FirstOrDefault();

            //Get list of cloud services.
            List<CloudService> cloudServices = (from datacenter in destSubscriptionMetadata.DataCenters
                                                select datacenter.CloudServices).FirstOrDefault();

            //Get list of storage accounts.
            List<Azure.DataCenterMigration.Models.StorageAccount> storageAccounts =
                (from datacenter in destSubscriptionMetadata.DataCenters
                 select datacenter.StorageAccounts).FirstOrDefault();

            //Get network configuration.
            NetworkConfiguration networkConfiguration = (from datacenter in destSubscriptionMetadata.DataCenters
                                                         select datacenter.NetworkConfiguration).FirstOrDefault();

            // Call management API to get destination subscription resources and check if resources already exist.

            AffinityGroupListResponse affinityGroupResponse = GetAffinityGroupListResponseFromMSAzure(
                importParameters.DestinationSubscriptionSettings.Credentials);

            HostedServiceListResponse cloudserviceResponse = GetCloudServiceListResponseFromMSAzure(
                importParameters.DestinationSubscriptionSettings.Credentials, importParameters.DestinationSubscriptionSettings.ServiceUrl);

            // Validate affinity groups.
            RenameAndValidateDestAffinityGroupNames(affinityGroups, affinityGroupResponse);

            // Validate network configuration.
            RenameAndValidateDestNetworkConfigurationNames(networkConfiguration);

            // Validate storage accounts.
            RenameAndValidateStorageAccountNames(storageAccounts);

            // Validate cloud services.
            RenameAndValidateDestCloudServiceNames(cloudServices, cloudserviceResponse);
            Logger.Info(methodName, ProgressResources.ExecutionCompleted);
        }

        /// <summary>
        /// Rename the destination affinity group name and validate it.
        /// </summary>
        /// <param name="affinityGroups">List of Affinity Groups from the metadata</param>
        /// <param name="affinityGroupResponse">List of Affinity Groups Response for destination subscription</param>        
        private void RenameAndValidateDestAffinityGroupNames(List<AffinityGroup> affinityGroups, AffinityGroupListResponse affinityGroupResponse)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.AffinityGroup.ToString());

            // Checks capacity for services.
            if ((Constants.MaximumLimitAffinityGroups - affinityGroupResponse.AffinityGroups.Count())
                < destSubscriptionMetadata.DataCenters.FirstOrDefault().AffinityGroups.Where(ag => ag.IsImported != true).Count())
            {
                throw (new ValidationException(StringResources.InsufficientAffinityGroups));
            }

            foreach (var affinityGroup in affinityGroups)
            {
                affinityGroup.AffinityGroupDetails.Name = GetDestinationResourceName(ResourceType.AffinityGroup, affinityGroup.AffinityGroupDetails.Name);
                affinityGroup.AffinityGroupDetails.Label = null;// affinityGroup.AffinityGroupDetails.Name;

                if (!affinityGroup.IsImported)
                {
                    // Check for duplicate name in destination subscription.
                    var affinityGroupInDestinationSubscription = (from ag in affinityGroupResponse.AffinityGroups
                                                                  where (string.Compare(ag.Name, affinityGroup.AffinityGroupDetails.Name,
                                                                  StringComparison.CurrentCultureIgnoreCase) == 0)
                                                                  select ag);

                    if (affinityGroupInDestinationSubscription.ToList().Count() > 0)
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidAffinityGroupNameExist,
                            affinityGroup.AffinityGroupDetails.Name));
                    }
                }
            }
            //Check for unique name in affinity groups. 
            // If count doesn't match then affinity group name is duplicate
            if (affinityGroups.Select(af => af.AffinityGroupDetails.Name).Distinct().Count() != affinityGroups.Count())
            {
                throw new ValidationException(StringResources.DuplicateAffinityGroupName);
            }
            Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.AffinityGroup.ToString());
        }

        /// <summary>
        /// Rename and validate destination network configuration names. 
        /// </summary>
        /// <param name="networkConfiguration">network configuration from the metadata</param>
        private void RenameAndValidateDestNetworkConfigurationNames(NetworkConfiguration networkConfiguration)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.VirtualNetwork.ToString());

            if (networkConfiguration != null && networkConfiguration.VirtualNetworkConfiguration != null && !networkConfiguration.IsImported)
            {
                // Get destination subscription network configuration
                NetworkGetConfigurationResponse destinationNetworkResponse = GetNetworkConfigurationFromMSAzure(
                    importParameters.DestinationSubscriptionSettings.Credentials, importParameters.DestinationSubscriptionSettings.ServiceUrl);

                NetworkConfiguration destinationNetConfiguration = null;
                XmlSerializer serializer = new XmlSerializer(typeof(NetworkConfiguration));
                if (destinationNetworkResponse != null && !(string.IsNullOrEmpty(destinationNetworkResponse.Configuration)))
                {
                    var destinationReader = new StringReader(destinationNetworkResponse.Configuration);
                    destinationNetConfiguration = (NetworkConfiguration)serializer.Deserialize(destinationReader);
                }

                if (networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                {
                    foreach (var virtualNetworkSite in networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites)
                    {
                        virtualNetworkSite.name = GetDestinationResourceName(ResourceType.VirtualNetworkSite, virtualNetworkSite.name);

                        if (virtualNetworkSite.Location != null)
                        {
                            virtualNetworkSite.Location = importParameters.DestinationDCName;
                        }
                        virtualNetworkSite.AffinityGroup = GetDestinationResourceName(ResourceType.AffinityGroup, virtualNetworkSite.AffinityGroup);
                        if (virtualNetworkSite.DnsServersRef != null)
                        {
                            var dnsList =
                              networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers.
                              Where(x => virtualNetworkSite.DnsServersRef.
                                  Any(x1 => x1.name == x.name)).ToArray();

                            virtualNetworkSite.DnsServersRef = (virtualNetworkSite.DnsServersRef.Select(s =>
                            {
                                s.name = GetDestinationResourceName(
                                    ResourceType.DnsServer, s.name,
                                    ResourceType.VirtualNetworkSite, virtualNetworkSite.name); return s;
                            })).ToArray();

                            dnsList = (dnsList.Select(s =>
                            {
                                s.name = GetDestinationResourceName(
                                    ResourceType.DnsServer, s.name,
                                    ResourceType.VirtualNetworkSite, virtualNetworkSite.name); return s;
                            })).ToArray();
                        }
                        if (virtualNetworkSite.Gateway != null && virtualNetworkSite.Gateway.ConnectionsToLocalNetwork != null &&
                            virtualNetworkSite.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef != null)
                        {
                            var localNetworkList =
                             networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites.
                             Where(x => virtualNetworkSite.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef.name == x.name).ToArray();

                            virtualNetworkSite.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef.name =
                                    GetDestinationResourceName(ResourceType.LocalNetworkSite,
                                virtualNetworkSite.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef.name,
                                ResourceType.VirtualNetworkSite, virtualNetworkSite.name);

                            localNetworkList = (localNetworkList.Select(s =>
                            {
                                s.name = GetDestinationResourceName(
                                    ResourceType.LocalNetworkSite, s.name,
                                    ResourceType.VirtualNetworkSite, virtualNetworkSite.name); return s;
                            })).ToArray();
                        }
                    }
                }
                if (destinationNetConfiguration != null &&
                        networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null
                    && destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                {
                    var networkNames =
                        (from dest in destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites
                         from vnet in networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites
                         where (string.Compare(vnet.name, dest.name, StringComparison.CurrentCultureIgnoreCase) == 0)
                         select dest.name).ToArray();

                    if (networkNames != null && networkNames.Count() > 0)
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidVirtualNetworkNameExist,
                      String.Join(", ", networkNames)));
                    }
                }
                // Check for unique name in virtual network names. 
                // If count doesn't match then virtual network name is duplicate
                if (networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                {
                    if (networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.Select(
                        cs => cs.name).Distinct().Count() !=
                        networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.Count())
                    {
                        throw new ValidationException(StringResources.DuplicateVirtualNetworkName);
                    }
                }

                if (networkConfiguration.VirtualNetworkConfiguration.Dns != null &&
                   networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null)
                {
                    if (destinationNetConfiguration != null &&
                        destinationNetConfiguration.VirtualNetworkConfiguration.Dns != null && destinationNetConfiguration
                        .VirtualNetworkConfiguration.Dns.DnsServers != null)
                    {
                        var dnsServerNames = (from dest in destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers
                                              from dns in networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers
                                              where (string.Compare(dns.name, dest.name, StringComparison.CurrentCultureIgnoreCase) == 0)
                                              select dest.name).ToArray();

                        if (dnsServerNames != null && dnsServerNames.Count() > 0)
                        {
                            throw new ValidationException(string.Format(StringResources.InvalidDNSServerNameExist,
                          String.Join(", ", dnsServerNames)));
                        }
                    }
                    // Check for unique name in dns name. 
                    // If count doesn't match then dns name is duplicate
                    if (networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers.Select(
                        cs => cs.name).Distinct().Count() != networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers.Count())
                    {
                        throw new ValidationException(StringResources.DuplicateDnsName);
                    }
                }
                if (networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null)
                {
                    if (destinationNetConfiguration != null &&
                       destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null)
                    {
                        var localNetworkNames = (from dest in destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites
                                                 from lnet in networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites
                                                 where (string.Compare(lnet.name, dest.name, StringComparison.CurrentCultureIgnoreCase) == 0)
                                                 select dest.name).ToArray();

                        if (localNetworkNames != null && localNetworkNames.Count() > 0)
                        {
                            throw new ValidationException(string.Format(StringResources.InvalidLocalNetworkNameExist,
                          String.Join(", ", localNetworkNames)));
                        }
                    }
                    // Check for unique name in local network names. 
                    // If count doesn't match then local network name is duplicate
                    if (networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites.Select(
                        cs => cs.name).Distinct().Count() !=
                        networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites.Count())
                    {
                        throw new ValidationException(StringResources.DuplicateLocalNetworkName);
                    }
                }
            }

            Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.VirtualNetwork.ToString());
        }

        /// <summary>
        /// Rename the destination cloud service name and validate it.
        /// </summary>
        /// <param name="cloudServices">List of Cloud Services from the metadata</param>
        /// <param name="cloudserviceResponse">List of Cloud Service Response for destination subscription</param>
        private void RenameAndValidateDestCloudServiceNames(List<CloudService> cloudServices, HostedServiceListResponse cloudserviceResponse)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.CloudService.ToString());

            if (cloudServices.Select(cs => cs.DeploymentDetails).Where(d => d != null).Where(IP => IP.ReservedIPName != null).Select(resip => resip.ReservedIPName).Distinct().Count() !=
               cloudServices.Select(cs => cs.DeploymentDetails).Where(d => d != null).Where(IP => IP.ReservedIPName != null).Select(resip => resip.ReservedIPName).Count())
            {
                throw new ValidationException(ProgressResources.DuplicateReservedIPName);
            }

            foreach (var service in cloudServices)
            {
                service.CloudServiceDetails.ServiceName = GetDestinationResourceName(ResourceType.CloudService,
                    service.CloudServiceDetails.ServiceName);
                if (!service.IsImported)
                {
                    // Check for service name availability.
                    if (!(CheckServiceNameAvailability(service.CloudServiceDetails.ServiceName,
                        importParameters.DestinationSubscriptionSettings.Credentials, importParameters.DestinationSubscriptionSettings.ServiceUrl)))
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidServiceNameExist, service.CloudServiceDetails.ServiceName));
                    }
                }
                service.CloudServiceDetails.Properties.AffinityGroup = GetDestinationResourceName(ResourceType.AffinityGroup,
                    service.CloudServiceDetails.Properties.AffinityGroup);

                // Validate Virtual machines parameters.
                if (service.DeploymentDetails != null)
                {
                    // Validate for ReservedIPName
                    if (service.DeploymentDetails.ReservedIPName != null)
                    {
                        CheckReservedIPNameAvailability(importParameters.DestinationSubscriptionSettings.Credentials, importParameters.DestinationSubscriptionSettings.ServiceUrl,
                            service.DeploymentDetails.ReservedIPName, service.CloudServiceDetails.ServiceName);
                    }
                    service.DeploymentDetails.VirtualNetworkName = GetDestinationResourceName(ResourceType.VirtualNetworkSite,
                        service.DeploymentDetails.VirtualNetworkName);
                    service.DeploymentDetails.Name = GetDestinationResourceName(ResourceType.Deployment, service.DeploymentDetails.Name, ResourceType.CloudService,
                        service.CloudServiceDetails.ServiceName);
                    RenameAndValidateDestVirtualMachineNames(service.DeploymentDetails.VirtualMachines, service.CloudServiceDetails.ServiceName);
                }
            }
            //Check for unique name in cloudServices. 
            // If count doesn't match then cloud service name is duplicate
            if (cloudServices.Select(cs => cs.CloudServiceDetails.ServiceName).Distinct().Count() != cloudServices.Count())
            {
                throw new ValidationException(StringResources.DuplicateServiceName);
            }
            Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.CloudService.ToString());
        }

        /// <summary>
        /// Rename and validate destination virtual Machines names.
        /// </summary>
        /// <param name="virtualMachines">List of Virtual Machines from the metadata</param>
        /// <param name="serviceName">Service name associated with virtual machines</param>        
        private void RenameAndValidateDestVirtualMachineNames(List<VirtualMachine> virtualMachines, string serviceName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.VirtualMachine.ToString());


            foreach (var virtualMachine in virtualMachines)
            {
                virtualMachine.VirtualMachineDetails.RoleName = GetDestinationResourceName(ResourceType.VirtualMachine,
                    virtualMachine.VirtualMachineDetails.RoleName, ResourceType.CloudService, serviceName);

                if (!virtualMachine.IsImported)
                {
                    string blobName;
                    string containerName;

                    // Check for valid length.
                    if (!(CheckForLength(virtualMachine.VirtualMachineDetails.RoleName, 3, 15)))
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidVirtualMachineLength,
                            virtualMachine.VirtualMachineDetails.RoleName));
                    }

                    //Check for blob exists
                    string storageAccountName = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Host.Substring(0,
                        virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Host.IndexOf('.'));
                    string storageAccountKey = GetStorageAccountKeysFromMSAzure(importParameters.SourceSubscriptionSettings.Credentials,
                        storageAccountName).PrimaryKey;

                    blobName = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Segments.Last();
                    containerName = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Segments[1].Substring(0,
                        virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Segments[1].IndexOf('/'));
                    if (!BlobExists(blobName, containerName, storageAccountKey, storageAccountName, true))
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidSourceOSBlob,
                                    virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.AbsoluteUri));
                    }

                    foreach (DataVirtualHardDisk disk in virtualMachine.VirtualMachineDetails.DataVirtualHardDisks)
                    {
                        blobName = disk.MediaLink.Segments.Last();
                        containerName = disk.MediaLink.Segments[1].Substring(0, disk.MediaLink.Segments[1].IndexOf('/'));
                        if (!BlobExists(blobName, containerName, storageAccountKey, storageAccountName, true))
                        {
                            throw new ValidationException(string.Format(StringResources.InvalidSourceDataDiskBlob,
                                        disk.MediaLink.AbsoluteUri));
                        }
                    }
                }
            }
            // Check for unique name in virtualMachines. 
            // If count doesn't match then virtual machine name is duplicate
            if (virtualMachines.Select(vm => vm.VirtualMachineDetails.RoleName).Distinct().Count() != virtualMachines.Count())
            {
                throw new ValidationException(StringResources.DuplicateVirtualMachineName);
            }
            Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.VirtualMachine.ToString());
        }

        /// <summary>
        /// Checks if destination subscription has capacity to deploy resources.
        /// </summary>
        private void ValidateSubscriptionCapacity()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);

            var dataCenter = destSubscriptionMetadata.DataCenters.FirstOrDefault();
            using (var client = new ManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
            {
                SubscriptionGetResponse subscriptionResponse = Retry.RetryOperation(() => client.Subscriptions.Get(),
                    (BaseParameters)importParameters,
                    ResourceType.None);

                // Checks capacity for services.
                if ((subscriptionResponse.MaximumHostedServices - subscriptionResponse.CurrentHostedServices)
                    < dataCenter.CloudServices.Where(service => service.IsImported != true).Count())
                {
                    throw (new ValidationException(StringResources.InsufficientCloudServices));
                }

                // Checks capacity for storage accounts.
                if ((subscriptionResponse.MaximumStorageAccounts - subscriptionResponse.CurrentStorageAccounts)
                    < dataCenter.StorageAccounts.Where(storage => storage.IsImported != true).Count())
                {
                    throw (new ValidationException(StringResources.InsufficientStorage));
                }

                if (dataCenter.NetworkConfiguration != null && dataCenter.NetworkConfiguration.IsImported == false &&
                    dataCenter.NetworkConfiguration.VirtualNetworkConfiguration != null)
                {
                    // Checks capacity for VirtualNetworkSites.
                    if (dataCenter.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null &&
                        (subscriptionResponse.MaximumVirtualNetworkSites - subscriptionResponse.CurrentVirtualNetworkSites)
                        < dataCenter.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.Count())
                    {
                        throw (new ValidationException(StringResources.InsufficientVirtualNetwork));
                    }

                    // Checks capacity for Dns.
                    if (dataCenter.NetworkConfiguration.VirtualNetworkConfiguration.Dns != null &&
                        dataCenter.NetworkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null &&
                        (subscriptionResponse.MaximumDnsServers - subscriptionResponse.CurrentDnsServers) <
                        dataCenter.NetworkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers.Count())
                    {
                        throw (new ValidationException(StringResources.InsufficientDnsServers));
                    }

                    // Checks capacity for LocalNetworkSites.
                    if (dataCenter.NetworkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null &&
                        (subscriptionResponse.MaximumLocalNetworkSites - subscriptionResponse.CurrentLocalNetworkSites)
                        < dataCenter.NetworkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites.Count())
                    {
                        throw (new ValidationException(StringResources.InsufficientLocalNetwork));
                    }
                }
                List<string> roleSizes = (from service in dataCenter.CloudServices
                                          where service.DeploymentDetails != null
                                          from virtualMachine in service.DeploymentDetails.VirtualMachines
                                          where virtualMachine.IsImported != true
                                          select virtualMachine.VirtualMachineDetails.RoleSize).ToList();

                Dictionary<string, string> roleSizeMaster = GetRoleSizes(importParameters.DestinationSubscriptionSettings.Id,
                         importParameters.DestinationSubscriptionSettings.Credentials.ManagementCertificate);

                int totalCoreCount = 0;
                foreach (string rolesize in roleSizes)
                {
                    string cores;
                    if (!roleSizeMaster.TryGetValue(rolesize, out cores))
                    {
                        cores = "0";
                    }
                    totalCoreCount += Convert.ToInt32(cores);
                }
                // Checks capacity for Vm cores.
                if ((subscriptionResponse.MaximumCoreCount - subscriptionResponse.CurrentCoreCount) < totalCoreCount)
                {
                    throw (new ValidationException(StringResources.InsufficientCores));
                }
            }
            Logger.Info(methodName, ProgressResources.ExecutionCompleted);
        }

        /// <summary>
        /// Rename and validate destination storage account names.
        /// </summary>
        /// <param name="storageAccounts">List of Storage Accounts from the metadata</param>
        private void RenameAndValidateStorageAccountNames(List<Azure.DataCenterMigration.Models.StorageAccount> storageAccounts)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.StorageAccount.ToString());

            foreach (Azure.DataCenterMigration.Models.StorageAccount storageAccount in storageAccounts)
            {
                string originalStorageName = storageAccount.StorageAccountDetails.Name;

                storageAccount.StorageAccountDetails.Name = GetDestinationResourceName(ResourceType.StorageAccount,
                    storageAccount.StorageAccountDetails.Name);
                storageAccount.StorageAccountDetails.Properties.AffinityGroup = GetDestinationResourceName(ResourceType.AffinityGroup,
                    storageAccount.StorageAccountDetails.Properties.AffinityGroup);

                if (!storageAccount.IsImported)
                {
                    // Check for length between 3 to 24 Char 
                    if (!CheckForLength(storageAccount.StorageAccountDetails.Name, 3, 24))
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidStorageAccountLength,
                            storageAccount.StorageAccountDetails.Name));
                    }

                    if (!(CheckStorageNameAvailability(storageAccount.StorageAccountDetails.Name,
                        importParameters.DestinationSubscriptionSettings.Credentials)))
                    {
                        throw new ValidationException(string.Format(StringResources.InvalidStorageAccountNameExist,
                            storageAccount.StorageAccountDetails.Name));
                    }
                }
            }

            // Check for unique name in storage account. 
            // If count doesn't match then storage name is duplicate
            if (storageAccounts.Select(cs => cs.StorageAccountDetails.Name).Distinct().Count() != storageAccounts.Count())
            {
                throw new ValidationException(StringResources.DuplicateStorageAccount);
            }
            Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.StorageAccount.ToString());
        }

        /// <summary>
        /// Validates for length.
        /// </summary>
        /// <param name="resourceName">Name whose length to be validated</param>
        /// <param name="minLength">Min length</param>
        /// <param name="maxLength">Max length</param>
        /// <returns>True if resourceName length exist in between min and max length</returns>
        private bool CheckForLength(string resourceName, int minLength, int maxLength)
        {
            if (resourceName.Length > maxLength || resourceName.Length < minLength)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Generates new name for destination resources by attaching Prefix to source  resources.
        /// </summary>
        /// <param name="originalResourceName">Source subscription resource name</param>
        /// <param name="resourceType">Resource type</param>
        /// <param name="parentResourceType">Type of parent resource</param>
        /// <param name="parentResourceDestinationName">Parent resource name of destination resource</param>
        /// <returns>Prefix attached value</returns>
        internal string GetDestinationResourceName(ResourceType resourceType, string originalResourceName, ResourceType parentResourceType = ResourceType.None, string parentResourceDestinationName = null)
        {
            if (originalResourceName == null)
            {
                return null;
            }
            else
            {
                string destinationResourceName = originalResourceName;
                if (resourceType != ResourceType.None)
                {
                    if (resourceNameCollection.ContainsKey(resourceType))
                    {
                        ResourceNameMapper resource = resourceNameCollection[resourceType].Where(s =>
                            s.SourceName.Equals(originalResourceName)).FirstOrDefault();
                        if (resource != null)
                        {
                            destinationResourceName = resource.DestinationName;
                        }
                    }
                    else
                    {
                        if (resourceNameCollection.ContainsKey(parentResourceType))
                        {
                            ResourceNameMapper resourceParent = resourceNameCollection[parentResourceType].Where(s =>
                                s.DestinationName.Equals(parentResourceDestinationName)).FirstOrDefault();

                            if (resourceParent != null)
                            {
                                destinationResourceName = helper.GetChildResourceName(resourceParent, resourceType, originalResourceName);
                            }
                        }
                    }
                }
                return destinationResourceName;
            }
        }

        /// <summary>
        /// Gets original resource name from newly genrated resource name
        /// </summary>        
        /// <param name="resourceType">Reource type/></param>
        /// /// <param name="resourceName">Resource name</param>
        /// <param name="parentResourceType">Type of parent resource</param>
        /// <param name="parentResourceName">Parent resource name</param>
        /// <returns>Prefix removed value</returns>
        internal string GetSourceResourceName(ResourceType resourceType, string resourceName, ResourceType parentResourceType = ResourceType.None, string parentResourceName = null)
        {
            if (resourceName == null)
            {
                return null;
            }
            else
            {
                string sourceResourceName = resourceName;

                if (resourceType != ResourceType.None)
                {
                    if (resourceNameCollection.ContainsKey(resourceType) && resourceNameCollection[resourceType].Count() > 0)
                    {
                        ResourceNameMapper resource = resourceNameCollection[resourceType].Where(s =>
                            s.DestinationName.Equals(resourceName)).FirstOrDefault();
                        if (resource != null)
                        {
                            sourceResourceName = resource.SourceName;
                        }
                    }
                    if (resourceNameCollection.ContainsKey(parentResourceType))
                    {
                        if (resourceNameCollection.ContainsKey(parentResourceType))
                        {
                            ResourceNameMapper resourceParent = resourceNameCollection[parentResourceType].Where(s =>
                                s.DestinationName.Equals(parentResourceName)).FirstOrDefault();

                            if (resourceParent != null)
                            {
                                sourceResourceName = helper.GetChildResourceName(resourceParent, resourceType, resourceName, false);
                            }
                        }
                    }
                }
                return sourceResourceName;
            }
        }

        #endregion

        #region Creates Resources in Destination
        /// <summary>
        /// Creates affinity groups.        
        /// </summary>        
        /// <param name="affinityGroups">List of affinity groups.</param>
        /// <param name="response">Source affinity groups with details from which the required affinity groups will be filtered and created</param>
        private void CreateAffinityGroups(List<AffinityGroup> affinityGroups, AffinityGroupListResponse response)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.AffinityGroup.ToString());
            Stopwatch swTotalAG = new Stopwatch();
            swTotalAG.Start();
            using (var client = new ManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
            {
                Parallel.ForEach(affinityGroups, affinityGroup =>
                {
                    try
                    {
                        // Check if affinity group is already imported, if not create new affinity group
                        if (!affinityGroup.IsImported)
                        {
                            Logger.Info(methodName, string.Format(ProgressResources.ImportAffinityGroupStarted,
                                affinityGroup.AffinityGroupDetails.Name), ResourceType.AffinityGroup.ToString(), affinityGroup.AffinityGroupDetails.Name);
                            Stopwatch swSingleAG = new Stopwatch();
                            swSingleAG.Start();
                            OperationResponse createAffinityGroupResult = Retry.RetryOperation(() => client.AffinityGroups.Create(
                                new AffinityGroupCreateParameters
                                {
                                    Label = affinityGroup.AffinityGroupDetails.Label,
                                    Description = affinityGroup.AffinityGroupDetails.Description,
                                    Location = importParameters.DestinationDCName,
                                    Name = affinityGroup.AffinityGroupDetails.Name
                                }), (BaseParameters)importParameters, ResourceType.AffinityGroup, affinityGroup.AffinityGroupDetails.Name,
                                () => DeleteAffinityGroupIfTaskCancelled(ResourceType.AffinityGroup, affinityGroup.AffinityGroupDetails.Name)
                                );
                            UpdateMedatadaFile(ResourceType.AffinityGroup, affinityGroup.AffinityGroupDetails.Name);
                            swSingleAG.Stop();
                            Logger.Info(methodName, string.Format(ProgressResources.ImportAffinityGroupCompleted,
                                affinityGroup.AffinityGroupDetails.Name, swSingleAG.Elapsed.Days, swSingleAG.Elapsed.Hours, swSingleAG.Elapsed.Minutes,
                               swSingleAG.Elapsed.Seconds), ResourceType.AffinityGroup.ToString(), affinityGroup.AffinityGroupDetails.Name);
                        }
                    }
                    catch (AggregateException exAgg)
                    {
                        foreach (var ex in exAgg.InnerExceptions)
                        {
                            Logger.Error(methodName, ex, ResourceType.AffinityGroup.ToString(), affinityGroup.AffinityGroupDetails.Name);
                        }
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(methodName, ex, ResourceType.AffinityGroup.ToString(), affinityGroup.AffinityGroupDetails.Name);
                        throw;
                    }
                });
            }
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime, swTotalAG.Elapsed.Days, swTotalAG.Elapsed.Hours, swTotalAG.Elapsed.Minutes,
                swTotalAG.Elapsed.Seconds), ResourceType.AffinityGroup.ToString());
        }

        /// <summary>
        /// Creates virtual networks.
        /// </summary>
        /// <param name="networkConfiguration">Network configuration</param>
        private void CreateVirtualNetworks(NetworkConfiguration networkConfiguration)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Stopwatch swVirtualNet = new Stopwatch();
            swVirtualNet.Start();
            if (networkConfiguration != null && !networkConfiguration.IsImported)
            {
                Logger.Info(methodName, ProgressResources.ImportVirtualNetworkStarted, ResourceType.VirtualNetwork.ToString());

                try
                {
                    // Get destination subscription network configuration
                    NetworkGetConfigurationResponse destinationNetworkResponse = GetNetworkConfigurationFromMSAzure(
                        importParameters.DestinationSubscriptionSettings.Credentials, importParameters.DestinationSubscriptionSettings.ServiceUrl);

                    NetworkConfiguration destinationNetConfiguration = null;
                    XmlSerializer serializer = new XmlSerializer(typeof(NetworkConfiguration));
                    if (destinationNetworkResponse != null && !(string.IsNullOrEmpty(destinationNetworkResponse.Configuration)))
                    {
                        var destinationReader = new StringReader(destinationNetworkResponse.Configuration);
                        destinationNetConfiguration = (NetworkConfiguration)serializer.Deserialize(destinationReader);
                    }

                    // Merge network configuration if destination subscription is already having networks.
                    if (destinationNetConfiguration != null)
                    {
                        if (destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null &&
                            networkConfiguration.VirtualNetworkConfiguration != null &&
                            networkConfiguration.VirtualNetworkConfiguration.Dns != null &&
                            networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null)
                        {
                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers =
                                destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers.Union(
                                  networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers
                                  ).ToArray();
                        }
                        else if (destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            (destinationNetConfiguration.VirtualNetworkConfiguration.Dns == null ||
                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers == null)
                            &&
                            networkConfiguration.VirtualNetworkConfiguration != null &&
                            networkConfiguration.VirtualNetworkConfiguration.Dns != null &&
                            networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null)
                        {
                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers =
                                networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers;
                        }

                        if (destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null &&
                            networkConfiguration.VirtualNetworkConfiguration != null &&
                            networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null)
                        {
                            destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites =
                              destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites.Union(
                                networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites).ToArray();
                        }
                        else if (destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites == null &&
                            networkConfiguration.VirtualNetworkConfiguration != null &&
                            networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null)
                        {
                            destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites = networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites;
                        }
                        if (destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null &&
                            networkConfiguration.VirtualNetworkConfiguration != null &&
                            networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                        {
                            destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites =
                               destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.Union(
                                 networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites).ToArray();
                        }
                        else if (destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites == null &&
                            networkConfiguration.VirtualNetworkConfiguration != null &&
                            networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                        {
                            destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites =
                                 networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites;
                        }
                    }
                    // else destination network configuration will have new network configuration only.    
                    else
                    {
                        destinationNetConfiguration = networkConfiguration;
                    }

                    MemoryStream memoryStream = new MemoryStream();
                    XmlWriter writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { Encoding = Encoding.UTF8 });
                    serializer.Serialize(writer, destinationNetConfiguration);

                    using (var vnetClient = new NetworkManagementClient(importParameters.DestinationSubscriptionSettings.Credentials,
                        importParameters.DestinationSubscriptionSettings.ServiceUrl))
                    {
                        OperationStatusResponse response = Retry.RetryOperation(() =>
                            vnetClient.Networks.SetConfiguration(new NetworkSetConfigurationParameters
                        {
                            Configuration = Encoding.UTF8.GetString(memoryStream.ToArray())
                        }
                        ), (BaseParameters)importParameters, ResourceType.NetworkConfiguration);
                    }
                    UpdateMedatadaFile(ResourceType.NetworkConfiguration);
                    writer.Close();
                    memoryStream.Close();
                    swVirtualNet.Stop();
                    Logger.Info(methodName, string.Format(ProgressResources.ImportVirtualNetworkCompleted, swVirtualNet.Elapsed.Days,
                        swVirtualNet.Elapsed.Hours, swVirtualNet.Elapsed.Minutes, swVirtualNet.Elapsed.Seconds),
                        ResourceType.VirtualNetwork.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Error(methodName, ex, ResourceType.VirtualNetwork.ToString());
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates cloud services services in destination subscription.
        /// </summary>
        /// <param name="cloudServices">List of cloud services.</param>
        private void CreateCloudServices(List<CloudService> cloudServices)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.CloudService.ToString());

            Stopwatch swTotalServices = new Stopwatch();
            swTotalServices.Start();
            using (var computeManagementClient = new ComputeManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
            {
                // To update imported cloud service count
                int importedServiceCount = 0;
                Parallel.ForEach(cloudServices, service =>
                {
                    try
                    {
                        Stopwatch swSingleService = new Stopwatch();
                        swSingleService.Start();
                        // Check if cloud service is already imported, if not create new cloud service
                        if (!service.IsImported)
                        {
                            Logger.Info(methodName, string.Format(ProgressResources.ImportServiceStarted,
                                service.CloudServiceDetails.ServiceName), ResourceType.CloudService.ToString(),
                                service.CloudServiceDetails.ServiceName);
                            OperationResponse createHostedServiceResult = (service.CloudServiceDetails.Properties.AffinityGroup == null) ?
                            Retry.RetryOperation(() => computeManagementClient.HostedServices.Create(
                            new HostedServiceCreateParameters
                            {
                                Label = null,
                                ServiceName = service.CloudServiceDetails.ServiceName,
                                Description = service.CloudServiceDetails.Properties.Description,
                                Location = importParameters.DestinationDCName,
                                ExtendedProperties = service.CloudServiceDetails.Properties.ExtendedProperties
                            }), (BaseParameters)importParameters, ResourceType.CloudService, service.CloudServiceDetails.ServiceName,
                                () => DeleteCloudServiceIfTaskCancelled(ResourceType.CloudService, service.CloudServiceDetails.ServiceName)) :

                            Retry.RetryOperation(() => computeManagementClient.HostedServices.Create(
                            new HostedServiceCreateParameters
                            {
                                Label = null,
                                ServiceName = service.CloudServiceDetails.ServiceName,
                                Description = service.CloudServiceDetails.Properties.Description,
                                AffinityGroup = service.CloudServiceDetails.Properties.AffinityGroup,
                                ExtendedProperties = service.CloudServiceDetails.Properties.ExtendedProperties
                            }), (BaseParameters)importParameters, ResourceType.CloudService, service.CloudServiceDetails.ServiceName,
                                () => DeleteCloudServiceIfTaskCancelled(ResourceType.CloudService, service.CloudServiceDetails.ServiceName));

                            UpdateMedatadaFile(ResourceType.CloudService, service.CloudServiceDetails.ServiceName);
                            swSingleService.Stop();
                            Logger.Info(methodName, string.Format(ProgressResources.ImportServiceCompleted,
                                service.CloudServiceDetails.ServiceName, swSingleService.Elapsed.Days,
                                swSingleService.Elapsed.Hours, swSingleService.Elapsed.Minutes, swSingleService.Elapsed.Seconds),
                                ResourceType.CloudService.ToString(), service.CloudServiceDetails.ServiceName);
                            // Import deployment in the cloud service
                            if (service.DeploymentDetails != null && service.DeploymentDetails.VirtualMachines != null
                                && service.DeploymentDetails.VirtualMachines.Count > 0)
                            {
                                CreateDeployment(service.DeploymentDetails, service.CloudServiceDetails.ServiceName);
                                if (!service.DeploymentDetails.IsImported)
                                {
                                    dcMigration.ReportProgress(string.Format(ProgressResources.CompletedServices, importedServiceCount + 1,
                                        cloudServices.Count()));
                                    Logger.Info(methodName, string.Format(ProgressResources.CompletedServices, importedServiceCount + 1,
                                        cloudServices.Count()),
                                    ResourceType.CloudService.ToString(), service.CloudServiceDetails.ServiceName);
                                }
                            }
                        }
                        else
                        {
                            // Import deployment in the cloud service
                            if (service.DeploymentDetails != null && service.DeploymentDetails.VirtualMachines != null
                                    && service.DeploymentDetails.VirtualMachines.Count > 0)
                            {
                                CreateDeployment(service.DeploymentDetails, service.CloudServiceDetails.ServiceName);
                                if (!service.DeploymentDetails.IsImported)
                                {
                                    dcMigration.ReportProgress(string.Format(ProgressResources.CompletedServices, importedServiceCount + 1,
                                        cloudServices.Count()));
                                    Logger.Info(methodName, string.Format(ProgressResources.CompletedServices, importedServiceCount + 1,
                                        cloudServices.Count()),
                                        ResourceType.CloudService.ToString(), service.CloudServiceDetails.ServiceName);
                                }
                            }
                        }
                        importedServiceCount++;
                    }
                    catch (AggregateException exAgg)
                    {
                        foreach (var ex in exAgg.InnerExceptions)
                        {
                            Logger.Error(methodName, ex, ResourceType.CloudService.ToString(), service.CloudServiceDetails.ServiceName);
                        }
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(methodName, ex, ResourceType.CloudService.ToString(), service.CloudServiceDetails.ServiceName);
                        throw;
                    }
                });
            }
            swTotalServices.Stop();
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime, swTotalServices.Elapsed.Days, swTotalServices.Elapsed.Hours,
                swTotalServices.Elapsed.Minutes, swTotalServices.Elapsed.Seconds), ResourceType.CloudService.ToString());
        }

        /// <summary>
        /// Creates storage account in destination subscription.
        /// </summary>
        /// <param name="storageAccounts">List of storage accounts</param>
        private void CreateStorageAccounts(List<Azure.DataCenterMigration.Models.StorageAccount> storageAccounts)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.StorageAccount.ToString());
            Stopwatch swTotalStorages = new Stopwatch();
            swTotalStorages.Start();
            Parallel.ForEach(storageAccounts, storageAccount =>
            {
                try
                {
                    // Check if storage account is already imported, if not create new storage account
                    if (!storageAccount.IsImported)
                    {
                        Stopwatch swStorage = new Stopwatch();
                        swStorage.Start();
                        // Create storage account 
                        using (var computeManagementClient = new StorageManagementClient(
                            importParameters.DestinationSubscriptionSettings.Credentials))
                        {
                            Logger.Info(methodName, string.Format(ProgressResources.ImportStorageAccountStarted,
                                storageAccount.StorageAccountDetails.Name), ResourceType.StorageAccount.ToString(), storageAccount.StorageAccountDetails.Name);

                            OperationStatusResponse createStorageAccountResult = (storageAccount.StorageAccountDetails.Properties.AffinityGroup == null)
                                ? Retry.RetryOperation(() => computeManagementClient.StorageAccounts.Create(
                                    new StorageAccountCreateParameters
                                    {
                                        Name = storageAccount.StorageAccountDetails.Name,
                                        Label = null,
                                        Description = storageAccount.StorageAccountDetails.Properties.Description,
                                        GeoReplicationEnabled = storageAccount.StorageAccountDetails.Properties.GeoReplicationEnabled,
                                        Location = importParameters.DestinationDCName,
                                        ExtendedProperties = storageAccount.StorageAccountDetails.ExtendedProperties
                                    }), (BaseParameters)importParameters,
                                    ResourceType.StorageAccount, storageAccount.StorageAccountDetails.Name)
                                    : Retry.RetryOperation(() => computeManagementClient.StorageAccounts.Create(
                                    new StorageAccountCreateParameters
                                    {
                                        Name = storageAccount.StorageAccountDetails.Name,
                                        Label = null,
                                        Description = storageAccount.StorageAccountDetails.Properties.Description,
                                        GeoReplicationEnabled = storageAccount.StorageAccountDetails.Properties.GeoReplicationEnabled,
                                        AffinityGroup = storageAccount.StorageAccountDetails.Properties.AffinityGroup,
                                        ExtendedProperties = storageAccount.StorageAccountDetails.ExtendedProperties
                                    }), (BaseParameters)importParameters,
                                    ResourceType.StorageAccount, storageAccount.StorageAccountDetails.Name);
                            UpdateMedatadaFile(ResourceType.StorageAccount, storageAccount.StorageAccountDetails.Name);
                            swStorage.Stop();
                            Logger.Info(methodName, string.Format(ProgressResources.ImportStorageAccountCompleted,
                                storageAccount.StorageAccountDetails.Name, swStorage.Elapsed.Days, swStorage.Elapsed.Hours, swStorage.Elapsed.Minutes, swStorage.Elapsed.Seconds),
                                ResourceType.StorageAccount.ToString(), storageAccount.StorageAccountDetails.Name);
                        }
                    }
                }
                catch (AggregateException exAgg)
                {
                    foreach (var ex in exAgg.InnerExceptions)
                    {
                        Logger.Error(methodName, exAgg, ResourceType.StorageAccount.ToString(), storageAccount.StorageAccountDetails.Name);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error(methodName, ex, ResourceType.StorageAccount.ToString(), storageAccount.StorageAccountDetails.Name);
                    throw;
                }
            });
            swTotalStorages.Stop();
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime, swTotalStorages.Elapsed.Days, swTotalStorages.Elapsed.Hours, swTotalStorages.Elapsed.Minutes,
                swTotalStorages.Elapsed.Seconds), ResourceType.StorageAccount.ToString());
        }

        /// <summary>
        /// Creates deployment and virtual machines in destination subscription.
        /// </summary>
        /// <param name="deploymentDetails">Deployment details</param>
        /// <param name="serviceName">Cloud service name</param>        
        private void CreateDeployment(Deployment deploymentDetails, string serviceName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.Deployment.ToString(), deploymentDetails.Name);
            Stopwatch swTotalDeploymentWithVMs = new Stopwatch();
            swTotalDeploymentWithVMs.Start();
            if (!deploymentDetails.IsImported)
            {
                //List<Uri> disks = null;                
                string containerName;
                using (var client = new ComputeManagementClient(importParameters.SourceSubscriptionSettings.Credentials,
                    importParameters.SourceSubscriptionSettings.ServiceUrl))
                {
                    try
                    {
                        using (var computeClient = new ComputeManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
                        {
                            for (int virtualMachineNumber = 0; virtualMachineNumber < deploymentDetails.VirtualMachines.Count(); virtualMachineNumber++)
                            {
                                VirtualMachine virtualMachine = deploymentDetails.VirtualMachines[virtualMachineNumber];
                                // Check if virtual machine is already imported, if not create new virtual machine
                                if (!virtualMachine.IsImported)
                                {
                                    string sourceStorageAccountName = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Host.Substring(
                                        0, virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Host.IndexOf('.'));
                                    string accountName = GetDestinationResourceName(ResourceType.StorageAccount, sourceStorageAccountName);
                                    Stopwatch swDeployment = new Stopwatch();
                                    swDeployment.Start();
                                    containerName = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Segments[1].Substring(0,
                                        virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Segments[1].IndexOf('/')); ;

                                    // Set up the Virtual Hard Disk with the OS Disk
                                    var vhd = new OSVirtualHardDisk
                                    {
                                        HostCaching = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.HostCaching,
                                        MediaLink = new Uri(string.Format(CultureInfo.InvariantCulture,
                                            Constants.StorageAccountMediaLink,
                                            accountName, containerName,
                                            virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.MediaLink.Segments.Last()), UriKind.Absolute),
                                        OperatingSystem = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.OperatingSystem,
                                        Label = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.Label,
                                        Name = string.Format("{0}{1}", importParameters.DestinationPrefixName,
                                        GetDestinationResourceName(ResourceType.OSDisk, virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.Name,
                                        ResourceType.CloudService, serviceName))
                                    };

                                    // Set up the Data Disk
                                    List<DataVirtualHardDisk> dataDisks = new List<DataVirtualHardDisk>();
                                    foreach (var disk in virtualMachine.VirtualMachineDetails.DataVirtualHardDisks)
                                    {
                                        dataDisks.Add(new DataVirtualHardDisk
                                        {
                                            SourceMediaLink = new Uri(string.Format(CultureInfo.InvariantCulture,
                                                Constants.StorageAccountMediaLink,
                                                accountName, containerName,
                                                   disk.MediaLink.Segments.Last()), UriKind.Absolute),
                                            LogicalUnitNumber = disk.LogicalUnitNumber,
                                            LogicalDiskSizeInGB = disk.LogicalDiskSizeInGB,
                                            Label = disk.Label,
                                            HostCaching = disk.HostCaching,
                                            Name = string.Format("{0}{1}", importParameters.DestinationPrefixName,
                                            GetDestinationResourceName(ResourceType.HardDisk, disk.Name, ResourceType.CloudService, serviceName))
                                        });
                                    };

                                    // Deploy the Virtual Machine
                                    Logger.Info(methodName, string.Format(ProgressResources.ImportVirtualMachineStarted,
                                                      virtualMachine.VirtualMachineDetails.RoleName, deploymentDetails.Name),
                                                      ResourceType.VirtualMachine.ToString(), virtualMachine.VirtualMachineDetails.RoleName);

                                    // For first virtual machine create new deployment
                                    if (virtualMachineNumber == 0)
                                    {
                                        List<Role> roles = new List<Role>();
                                        roles.Add(new Role
                                        {
                                            RoleName = virtualMachine.VirtualMachineDetails.RoleName,
                                            RoleSize = virtualMachine.VirtualMachineDetails.RoleSize,
                                            RoleType = virtualMachine.VirtualMachineDetails.RoleType,
                                            OSVirtualHardDisk = vhd,
                                            DataVirtualHardDisks = dataDisks,
                                            ConfigurationSets = virtualMachine.VirtualMachineDetails.ConfigurationSets,
                                            AvailabilitySetName = virtualMachine.VirtualMachineDetails.AvailabilitySetName,
                                            DefaultWinRmCertificateThumbprint = virtualMachine.VirtualMachineDetails.DefaultWinRmCertificateThumbprint,
                                            ProvisionGuestAgent = true,
                                            ResourceExtensionReferences = virtualMachine.VirtualMachineDetails.ResourceExtensionReferences
                                        });

                                        // Create the deployment parameters
                                        var createDeploymentParameters = new VirtualMachineCreateDeploymentParameters
                                        {
                                            DeploymentSlot = DeploymentSlot.Production,
                                            DnsSettings = deploymentDetails.DnsSettings,
                                            Name = deploymentDetails.Name,
                                            Label = deploymentDetails.Label,
                                            VirtualNetworkName = deploymentDetails.VirtualNetworkName,

                                            ReservedIPName = deploymentDetails.ReservedIPName,
                                            LoadBalancers = deploymentDetails.LoadBalancers,
                                            Roles = roles,

                                        };
                                        var deploymentResult = Retry.RetryOperation(() => computeClient.VirtualMachines.CreateDeployment(
                                            serviceName, createDeploymentParameters), (BaseParameters)importParameters, ResourceType.VirtualMachine,
                                            virtualMachine.VirtualMachineDetails.RoleName);

                                        UpdateMedatadaFile(ResourceType.VirtualMachine, virtualMachine.VirtualMachineDetails.RoleName,
                                            parentResourceName: serviceName);

                                    }
                                    // Add virtual machine in existing deployment
                                    else
                                    {
                                        VirtualMachineCreateParameters parameters = new VirtualMachineCreateParameters
                                        {
                                            RoleName = virtualMachine.VirtualMachineDetails.RoleName,
                                            RoleSize = virtualMachine.VirtualMachineDetails.RoleSize,
                                            ProvisionGuestAgent = true,
                                            OSVirtualHardDisk = vhd,
                                            ConfigurationSets = virtualMachine.VirtualMachineDetails.ConfigurationSets,
                                            AvailabilitySetName = virtualMachine.VirtualMachineDetails.AvailabilitySetName,
                                            DataVirtualHardDisks = dataDisks,
                                            ResourceExtensionReferences = virtualMachine.VirtualMachineDetails.ResourceExtensionReferences
                                        };

                                        Retry.RetryOperation(() => computeClient.VirtualMachines.Create(serviceName, deploymentDetails.Name, parameters),
                                           (BaseParameters)importParameters, ResourceType.VirtualMachine, virtualMachine.VirtualMachineDetails.RoleName,
                                            () => DeleteVirtualMachineIfTaskCancelled(ResourceType.VirtualMachine, serviceName, deploymentDetails.Name, virtualMachine.VirtualMachineDetails.RoleName));

                                        UpdateMedatadaFile(ResourceType.VirtualMachine, virtualMachine.VirtualMachineDetails.RoleName, parentResourceName: serviceName);
                                    }
                                    swDeployment.Stop();
                                    Logger.Info(methodName, string.Format(ProgressResources.ImportVirtualMachineCompleted,
                                                    virtualMachine.VirtualMachineDetails.RoleName, deploymentDetails.Name, swDeployment.Elapsed.Days, swDeployment.Elapsed.Hours,
                                                    swDeployment.Elapsed.Minutes, swDeployment.Elapsed.Seconds),
                                                    ResourceType.VirtualMachine.ToString(), virtualMachine.VirtualMachineDetails.RoleName);

                                    // ShutDown created virtual machine.
                                    computeClient.VirtualMachines.Shutdown(serviceName, deploymentDetails.Name,
                                        virtualMachine.VirtualMachineDetails.RoleName,
                                        new VirtualMachineShutdownParameters { PostShutdownAction = PostShutdownAction.StoppedDeallocated });
                                }
                            }
                            UpdateMedatadaFile(ResourceType.Deployment, deploymentDetails.Name, parentResourceName: serviceName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(methodName, ex, ResourceType.Deployment.ToString(), deploymentDetails.Name);
                        throw;
                    }
                }
            }
            swTotalDeploymentWithVMs.Stop();
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime, swTotalDeploymentWithVMs.Elapsed.Days, swTotalDeploymentWithVMs.Elapsed.Hours,
                swTotalDeploymentWithVMs.Elapsed.Minutes, swTotalDeploymentWithVMs.Elapsed.Seconds), ResourceType.Deployment.ToString(), deploymentDetails.Name);
        }

        #region Copy storage blob

        ///<summary> 
        /// Shutdown the source virtual machine and copy a blob to a particular blob destination endpoint - this is a blocking call        
        /// </summary>
        public void ShutDownVMsAndCopyBlobToDestination()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;

            // Get list of disk uri
            List<Uri> diskUri = new List<Uri>();
            diskUri.AddRange(destSubscriptionMetadata.DataCenters.FirstOrDefault().CloudServices.Where(
                d => d.DeploymentDetails != null).Select(dd => dd.DeploymentDetails).SelectMany(vm => vm.VirtualMachines).Where(dds => dds.VirtualMachineDetails.DataVirtualHardDisks != null).SelectMany(medialinks => medialinks.VirtualMachineDetails.DataVirtualHardDisks).Select(dds => dds.MediaLink));
            diskUri.AddRange(destSubscriptionMetadata.DataCenters.FirstOrDefault().CloudServices.Where(
                d => d.DeploymentDetails != null).Select(dd => dd.DeploymentDetails).SelectMany(vm => vm.VirtualMachines).Select(
                medialinks => medialinks.VirtualMachineDetails.OSVirtualHardDisk.MediaLink));

            using (var client = new ComputeManagementClient(importParameters.SourceSubscriptionSettings.Credentials,
                importParameters.SourceSubscriptionSettings.ServiceUrl))
            {
                dcMigration.ReportProgress(ProgressResources.ShutDownAllVms);
                Parallel.ForEach(destSubscriptionMetadata.DataCenters.FirstOrDefault().CloudServices, service =>
                {
                    string serviceName = service.CloudServiceDetails.ServiceName;
                    if (service.DeploymentDetails != null)
                    {
                        // Get deployment of production slot for specific service.
                        var sourceDeployment = client.Deployments.GetBySlot(GetSourceResourceName(ResourceType.CloudService,
                            serviceName), DeploymentSlot.Production);
                        Parallel.ForEach(service.DeploymentDetails.VirtualMachines, virtualMachine =>
                        {
                            string vmSourceName = GetSourceResourceName(ResourceType.VirtualMachine, virtualMachine.VirtualMachineDetails.RoleName,
                                                            ResourceType.CloudService, serviceName);
                            RoleInstance roleInstance = (from instance in sourceDeployment.RoleInstances
                                                         where (instance.RoleName ==
                                                             vmSourceName)
                                                         // virtualMachine.VirtualMachineDetails.RoleName)
                                                         select instance).FirstOrDefault();

                            Logger.Info(methodName, string.Format(ProgressResources.ShutDownVm, vmSourceName),
                                ResourceType.VirtualMachine.ToString(), vmSourceName);
                            // Shutdown the source virtual machine if it is running
                            if (roleInstance.InstanceStatus != Constants.VMStatusStopped && roleInstance.InstanceStatus != Constants.VMStatusStoppedDeallocated)
                            {
                                Retry.RetryOperation((() => client.VirtualMachines.Shutdown(GetSourceResourceName(ResourceType.CloudService, serviceName),
                                 sourceDeployment.Name, vmSourceName,
                                 new VirtualMachineShutdownParameters { PostShutdownAction = PostShutdownAction.Stopped })),
                                 (BaseParameters)importParameters, ResourceType.VirtualMachine, vmSourceName);
                            }
                        });
                    }
                });
            }
            // Start stopwatch to calculate total time required for all blobs
            Stopwatch swCopyAllBlobsOfVM = new Stopwatch();
            swCopyAllBlobsOfVM.Start();

            Parallel.ForEach(diskUri, blobUri =>
            {
                string sourceStorageAccountName = blobUri.Host.Substring(0, blobUri.Host.IndexOf('.'));
                string destStorageAccountName = GetDestinationResourceName(ResourceType.StorageAccount, sourceStorageAccountName);

                // get storage account key.
                string sourceStorageAccountKey = GetStorageAccountKeysFromMSAzure(importParameters.SourceSubscriptionSettings.Credentials,
                    sourceStorageAccountName).PrimaryKey;
                string destStorageAccountKey = GetStorageAccountKeysFromMSAzure(importParameters.DestinationSubscriptionSettings.Credentials,
                    destStorageAccountName).PrimaryKey;
                string blobName = blobUri.Segments.Last();
                string containerName = blobUri.Segments[1].Substring(0, blobUri.Segments[1].IndexOf('/'));

                try
                {
                    // Start stopwatch to calculate total time required for single blob
                    Stopwatch swBlob = new Stopwatch();
                    swBlob.Start();

                    bool deletedPendingBlob = false;
                    // get all details of destination blob.
                    CloudPageBlob destBlob = GetCloudBlob(blobName, containerName, destStorageAccountKey, destStorageAccountName, false);
                    // Check the status of blob if it is already present. Delete the blob if the status is pending.
                    BlobRequestOptions requestOptions = Retry.GetBlobRequestOptions(importParameters.DeltaBackOff, importParameters.RetryCount);
                    if (destBlob.Exists())
                    {
                        CloudPageBlob destBlobInfo = (CloudPageBlob)destBlob.Container.GetBlobReferenceFromServer(blobName);
                        if (destBlobInfo.CopyState.Status == CopyStatus.Pending)
                        {
                            Logger.Info(methodName, string.Format(ProgressResources.DeleteNonSuccessBlob, destBlobInfo.CopyState.Status),
                                ResourceType.Blob.ToString(), blobName);
                            destBlobInfo.AbortCopy(destBlobInfo.CopyState.CopyId, null, requestOptions);
                            destBlobInfo.Delete(DeleteSnapshotsOption.IncludeSnapshots, null, requestOptions, null);
                            deletedPendingBlob = true;
                        }
                    }

                    // if blob is not exists or deleted the pending blob then copy it on destination.
                    if (!destBlob.Exists() || (deletedPendingBlob))
                    {
                        Logger.Info(methodName, String.Format(ProgressResources.CopyBlobToDestinationStarted, containerName, blobName, destStorageAccountName),
                            ResourceType.Blob.ToString(), blobName);

                        // get all details of source blob.
                        Microsoft.WindowsAzure.Storage.Blob.CloudPageBlob sourceBlob = GetCloudBlob(blobName, containerName,
                            sourceStorageAccountKey, sourceStorageAccountName, true);
                        destBlob = GetCloudBlob(blobName, containerName, destStorageAccountKey, destStorageAccountName, false);

                        // get Shared Access Signature for private containers.
                        var sas = sourceBlob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                        {
                            SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                            SharedAccessExpiryTime = DateTime.UtcNow.AddDays(7),
                            Permissions = SharedAccessBlobPermissions.Read,
                        });

                        var srcBlobSasUri = string.Format("{0}{1}", sourceBlob.Uri, sas);
                        string destUri = string.Format(Constants.StorageAccountMediaLink, destStorageAccountName, containerName, blobName);

                        // copy blob from source to destination.
                        string copyId = destBlob.StartCopyFromBlob(new Uri(srcBlobSasUri), null, null, requestOptions, null);

                        dcMigration.ReportProgress(string.Format(ProgressResources.BlobCopyStarted, blobUri, destUri));
                        WaitForBlobCopy(destBlob.Container, blobName);

                        swBlob.Stop();
                        dcMigration.ReportProgress(string.Format(ProgressResources.BlobCopyCompleted, blobUri, destUri));
                        Logger.Info(methodName, String.Format(ProgressResources.CopyBlobToDestinationCompleted, containerName, blobName,
                            destStorageAccountName, swBlob.Elapsed.Days, swBlob.Elapsed.Hours, swBlob.Elapsed.Minutes, swBlob.Elapsed.Seconds), ResourceType.Blob.ToString(), blobName);
                    }
                    else
                    {
                        Logger.Info(methodName, String.Format(ProgressResources.BlobExistsInDestination, containerName, blobName,
                            destStorageAccountName), ResourceType.Blob.ToString(), blobName);
                    }
                }
                catch (AggregateException exAgg)
                {
                    foreach (var ex in exAgg.InnerExceptions)
                    {
                        Logger.Error(methodName, exAgg, ResourceType.StorageAccount.ToString(), blobName);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error(methodName, ex, ResourceType.StorageAccount.ToString(), blobName);
                    throw;
                }
            });
            swCopyAllBlobsOfVM.Stop();
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime, swCopyAllBlobsOfVM.Elapsed.Days, swCopyAllBlobsOfVM.Elapsed.Hours,
                swCopyAllBlobsOfVM.Elapsed.Minutes, swCopyAllBlobsOfVM.Elapsed.Seconds), ResourceType.Blob.ToString());
        }

        /// <summary>
        /// Print blob copy progress
        /// </summary>
        /// <param name="blobContainer">destination container name</param>
        /// <param name="blobName">blob name</param>
        private void WaitForBlobCopy(CloudBlobContainer blobContainer, string blobName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.Blob.ToString(), blobName);
            double bytesCopied = 0;
            // Initial status
            CloudPageBlob blob = (CloudPageBlob)blobContainer.GetBlobReferenceFromServer(blobName);
            CopyStatus copyStatus = blob.CopyState.Status;
            // Loop until status becomes success
            while (copyStatus == CopyStatus.Pending)
            {
                try
                {
                    Task.Delay(Constants.DelayTimeInMilliseconds).Wait();
                    blob = (CloudPageBlob)blobContainer.GetBlobReferenceFromServer(blobName);
                    copyStatus = blob.CopyState.Status;
                    if (blob.CopyState.BytesCopied.HasValue)
                        bytesCopied = blob.CopyState.BytesCopied.Value;

                    var totalBytes = blob.CopyState.TotalBytes;

                    if (totalBytes.HasValue)
                    {
                        // Print status
                        dcMigration.ReportProgress(string.Format(ProgressResources.CopyBlobProgressInPercentage, blob.Name,
                                          (bytesCopied / totalBytes.Value) * 100));
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(methodName, ex, ResourceType.Blob.ToString(), blobName);
                }
            }
            if (copyStatus == CopyStatus.Aborted || copyStatus == CopyStatus.Failed || copyStatus == CopyStatus.Invalid)
            {
                blob = (CloudPageBlob)blobContainer.GetBlobReferenceFromServer(blobName);
                BlobRequestOptions requestOptions = Retry.GetBlobRequestOptions(importParameters.DeltaBackOff,
                    importParameters.RetryCount);
                blob.Delete(DeleteSnapshotsOption.IncludeSnapshots, null, requestOptions, null);
                Logger.Info(methodName, string.Format(ProgressResources.DeleteNonSuccessBlob, copyStatus),
                    ResourceType.Blob.ToString(), blobName);
                throw new Exception(string.Format(ProgressResources.DeleteNonSuccessBlob, copyStatus));
            }

            Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.Blob.ToString(), blobName);
        }

        /// <summary>
        /// Used to determine whether the blob exists or not
        /// </summary>
        /// <param name="blobName">blob name</param>
        /// <param name="containerName">container name</param>
        /// <param name="storageKey">storage key</param>
        /// <param name="storageAccountName">storage account name</param>
        /// <param name="sourceSubscription">true if it is verifying for source subscription</param>
        /// <returns>true if blob exist</returns>
        public bool BlobExists(string blobName, string containerName, string storageKey, string storageAccountName,
            bool sourceSubscription)
        {
            // get the cloud blob
            var cloudBlob = GetCloudBlob(blobName, containerName, storageKey, storageAccountName, sourceSubscription);
            return (cloudBlob.Exists());
        }

        /// <summary>
        /// Used to pull back the cloud blob that should be copied from or to
        /// </summary>
        /// <param name="blobName">blob name</param>
        /// <param name="containerName">container name</param>
        /// <param name="storageKey">storage key</param>
        /// <param name="storageAccountName">storage account name</param>
        /// <param name="sourceSubscription">true if it is getting blob value for source subscription</param>
        /// <returns>the cloud blob that should be copied from or to</returns>
        private CloudPageBlob GetCloudBlob(string blobName, string containerName, string storageKey,
            string storageAccountName, bool sourceSubscription)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, string.Format(ProgressResources.GetCloudBlobStarted, blobName, storageAccountName),
                ResourceType.Blob.ToString(), blobName);

            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount =
                new Microsoft.WindowsAzure.Storage.CloudStorageAccount(
                    new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageAccountName, storageKey), true);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = cloudBlobClient.GetContainerReference(containerName);
            if (!container.Exists() && sourceSubscription == false)
            {
                lock (thisLockContainer)
                {
                    if (!container.Exists())
                    {
                        container.Create();
                    }
                }
            }
            Logger.Info(methodName, string.Format(ProgressResources.CloudBlobInfoRecieved, blobName, storageAccountName),
                ResourceType.Blob.ToString(), blobName);
            return (container.GetPageBlobReference(blobName));
        }

        #endregion
        #endregion

        #region Management API Calls

        /// <summary>
        /// Checks for service name availability
        /// </summary>
        /// <param name="serviceName"> Service Name</param>
        /// <param name="credentials"> Destination subscription credentials</param>
        /// <param name="serviceUrl"> Destination subscription service Url</param>
        /// <returns>false if service name is reserved</returns>
        private bool CheckServiceNameAvailability(string serviceName, SubscriptionCloudCredentials credentials, Uri serviceUrl)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, string.Format(ProgressResources.CheckServiceNameAvailabilityFromMSAzureStarted, serviceName), ResourceType.CloudService.ToString(), serviceName);
            using (var client = new ComputeManagementClient(credentials, serviceUrl))
            {
                HostedServiceCheckNameAvailabilityResponse availabilityResponse = Retry.RetryOperation(() =>
                    client.HostedServices.CheckNameAvailability(serviceName),
                   (BaseParameters)importParameters, ResourceType.CloudService, serviceName);
                Logger.Info(methodName, String.Format(ProgressResources.CheckServiceNameAvailabilityFromMSAzureCompleted, serviceName),
                    ResourceType.CloudService.ToString(), serviceName);
                return availabilityResponse.IsAvailable;
            }
        }

        /// <summary>
        /// Checks for storage account name availability
        /// </summary>
        /// <param name="storageAccountName">Destination storage account name</param>
        /// <param name="credentials">Destination subsctription credentials</param>
        /// <returns>false if storage account name is reserved</returns>
        private bool CheckStorageNameAvailability(string storageAccountName, SubscriptionCloudCredentials credentials)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, String.Format(ProgressResources.CheckStorageNameAvailabilityFromMSAzureStarted, storageAccountName),
                ResourceType.StorageAccount.ToString(), storageAccountName);
            using (var client = new StorageManagementClient(credentials))
            {
                CheckNameAvailabilityResponse storageResponse = Retry.RetryOperation(() => client.StorageAccounts.CheckNameAvailability(
                    storageAccountName), (BaseParameters)importParameters, ResourceType.StorageAccount, storageAccountName);
                Logger.Info(methodName, String.Format(ProgressResources.CheckStorageNameAvailabilityFromMSAzureCompleted,
                    storageAccountName), ResourceType.StorageAccount.ToString(), storageAccountName);
                return storageResponse.IsAvailable;
            }
        }

        /// <summary>
        /// Gets list of storage account operation response from MS azure using API call. 
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>
        /// <returns>list of storage account</returns>
        private StorageAccountListResponse GetStorageAccountListResponseFromMSAzure(SubscriptionCloudCredentials credentials)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetStorageAccountsFromMSAzureStarted, ResourceType.StorageAccount.ToString());
            using (var client = new StorageManagementClient(credentials))
            {
                // Call management API to get list of storage accounts.
                StorageAccountListResponse storageResponse = Retry.RetryOperation(() => client.StorageAccounts.List(),
                    (BaseParameters)importParameters, ResourceType.StorageAccount);
                Logger.Info(methodName, ProgressResources.GetStorageAccountsFromMSAzureCompleted, ResourceType.StorageAccount.ToString());
                return storageResponse;
            }
        }


        /// <summary>
        /// Gets list of storage account key response from MS azure using API call. 
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>
        /// <param name="storageAccountName">Storage account name for which storage account key to get received</param>
        /// <returns>key response for storage account</returns>
        private StorageAccountGetKeysResponse GetStorageAccountKeysFromMSAzure(SubscriptionCloudCredentials credentials, string storageAccountName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            using (var client = new StorageManagementClient(credentials))
            {
                Logger.Info(methodName, string.Format(ProgressResources.GetStorageAccountKeysStarted, storageAccountName),
                    ResourceType.StorageAccount.ToString(), storageAccountName);
                // Call management API to get keys of storage account.
                StorageAccountGetKeysResponse storageKeyResponse = Retry.RetryOperation(() => client.StorageAccounts.GetKeys(storageAccountName),
                    (BaseParameters)importParameters,
                    ResourceType.StorageAccount, storageAccountName);
                Logger.Info(methodName, string.Format(ProgressResources.GetStorageAccountKeysCompleted, storageAccountName),
                    ResourceType.StorageAccount.ToString(), storageAccountName);
                return storageKeyResponse;
            }
        }

        /// <summary>        
        /// Gets list of hosted service operation response from MS azure using API call.        
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>
        /// <param name="serviceUrl">service url of subscription</param>
        /// <returns>List of hosted service operation response for subscription </returns>
        private HostedServiceListResponse GetCloudServiceListResponseFromMSAzure(SubscriptionCloudCredentials credentials, Uri serviceUrl)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetServicesFromMSAzureStarted, ResourceType.CloudService.ToString());
            using (var client = new ComputeManagementClient(credentials, serviceUrl))
            {
                // Call management API to get list of CloudServices.
                HostedServiceListResponse serviceResponse = Retry.RetryOperation(() => client.HostedServices.List(),
                    (BaseParameters)importParameters,
                    ResourceType.CloudService);
                Logger.Info(methodName, ProgressResources.GetServicesFromMSAzureCompleted, ResourceType.CloudService.ToString());
                return serviceResponse;
            }
        }

        /// <summary>        
        /// Gets list of affinity group operation response from MS azure using API call.        
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>        
        /// <returns>List of affinity group operation response for subscription </returns>
        private AffinityGroupListResponse GetAffinityGroupListResponseFromMSAzure(SubscriptionCloudCredentials credentials)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetAffinityGroupsFromMSAzureStarted, ResourceType.AffinityGroup.ToString());
            using (var client = new ManagementClient(credentials))
            {
                // Call management API to get list of affinity groups.
                AffinityGroupListResponse agResponse = Retry.RetryOperation(() => client.AffinityGroups.List(),
                   (BaseParameters)importParameters,
                    ResourceType.AffinityGroup);
                Logger.Info(methodName, ProgressResources.GetAffinityGroupsFromMSAzureCompleted, ResourceType.AffinityGroup.ToString());
                return agResponse;
            }
        }

        /// <summary>
        /// Gets network configuration from MS azure using management API call.
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>
        /// <param name="serviceUrl">service url of subscription</param>
        /// <returns>Network configuration for subscription</returns>
        private NetworkGetConfigurationResponse GetNetworkConfigurationFromMSAzure(SubscriptionCloudCredentials credentials, Uri serviceUrl)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetVirtualNetworkConfigFromMSAzureStarted, ResourceType.VirtualNetwork.ToString());
            using (var vnetClient = new NetworkManagementClient(credentials, serviceUrl))
            {
                try
                {
                    NetworkGetConfigurationResponse ventConfig = vnetClient.Networks.GetConfiguration();
                    Logger.Info(methodName, ProgressResources.GetVirtualNetworkConfigFromMSAzureCompleted, ResourceType.VirtualNetwork.ToString());
                    return ventConfig;
                }
                catch (CloudException cex)
                {
                    if (cex.ErrorCode == Constants.ResourceNotFound)
                    {
                        return null;
                    }
                    else
                    {
                        Logger.Error(methodName, cex, ResourceType.VirtualNetwork.ToString());
                        throw cex;
                    }
                }
            }
        }

        /// <summary>        
        /// Gets virtual machines response from MS azure using API call.        
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>
        /// <param name="serviceUrl">ServiceUrl of subscription</param>
        /// <param name="serviceName">Name of azure service</param>
        /// <param name="deploymentName">Name of azure service deployment</param>
        /// <param name="virtualMachineName">Name of azure virtual machine</param>
        /// <returns>Virtual machine response for subscription </returns>
        private VirtualMachineGetResponse GetVirtualMachinesResponseFromMSAzure(SubscriptionCloudCredentials credentials, Uri serviceUrl, 
            string serviceName, string deploymentName, string virtualMachineName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetVirtualMachinesFromMSAzureStarted, ResourceType.VirtualMachine.ToString());
            using (var client = new ComputeManagementClient(credentials, serviceUrl))
            {
                // Call management API to get list of CloudServices.
                VirtualMachineGetResponse virtualMachineResponse = Retry.RetryOperation(() => client.VirtualMachines.Get(serviceName, deploymentName, virtualMachineName),
                   (BaseParameters)importParameters,
                    ResourceType.VirtualMachine, virtualMachineName);
                Logger.Info(methodName, ProgressResources.GetVirtualMachinesFromMSAzureCompleted, ResourceType.VirtualMachine.ToString());
                return virtualMachineResponse;
            }
        }

        /// <summary>        
        /// Gets deployement response from MS azure using API call.        
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>
        /// <param name="serviceUrl">ServiceUrl of subscription</param>
        /// <param name="serviceName">Name of azure service</param>
        /// <param name="deploymentName">Name of azure service deployment</param>
        /// <returns>Deployment response for subscription </returns>
        private DeploymentGetResponse GetDeploymentResponseFromMSAzure(SubscriptionCloudCredentials credentials, Uri serviceUrl,
            string serviceName, string deploymentName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetDeploymentFromMSAzureStarted, ResourceType.CloudService.ToString());
            using (var client = new ComputeManagementClient(credentials, serviceUrl))
            {
                // Call management API to get list of CloudServices.
                DeploymentGetResponse deploymentResponse = Retry.RetryOperation(() => client.Deployments.GetByName(serviceName, deploymentName),
                   (BaseParameters)importParameters,
                    ResourceType.Deployment, deploymentName);
                Logger.Info(methodName, ProgressResources.GetDeploymentFromMSAzureCompleted, ResourceType.CloudService.ToString());
                return deploymentResponse;
            }
        }

        /// <summary>
        /// Checks that the provided reserved ip name is present in destination and also it is not allocated to any service.
        /// </summary>
        /// <param name="credentials">Subscription credentials</param>
        /// <param name="serviceUrl">Subscription service url</param>
        /// <param name="reservedIPName">Reserved ip name</param>
        /// <param name="serviceName">Service name</param>
        /// <returns>Returns true if ReservedIPName is present in destination and it is not allocated to any deployment /
        /// returns <see cref="ValidationException"/> if the ReservedIPName is not available in destination subscription or it is available but already assigned to any service.
        /// </returns>
        private bool CheckReservedIPNameAvailability(SubscriptionCloudCredentials credentials, Uri serviceUrl, string reservedIPName, string serviceName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetVirtualNetworkConfigFromMSAzureStarted, ResourceType.VirtualNetwork.ToString());
            using (var vnetClient = new NetworkManagementClient(credentials, serviceUrl))
            {
                var reservedIPLists = vnetClient.ReservedIPs.List();
                NetworkReservedIPListResponse.ReservedIP reservedIpPresent = (from ri in reservedIPLists
                                                                              where (ri.Name == reservedIPName)
                                                                              select ri).FirstOrDefault();

                if (reservedIpPresent == null)
                {
                    throw new ValidationException(string.Format(ProgressResources.MissingReservedIPName, reservedIPName));
                }
                else if (reservedIpPresent.ServiceName != null && !(serviceName.Equals(reservedIpPresent.ServiceName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new ValidationException(string.Format(ProgressResources.OccupiedReservedIpName, reservedIPName, reservedIpPresent.ServiceName));
                }
            }
            return true;
        }
        #endregion

        #region Delete Resources if TaskCancelled exception occurred in resource creation process

        ///<summary>
        /// Deletes the affinity group if it is started creating and the corresponding task is got cancelled later.
        /// </summary>
        /// <param name="resourceType">Resource Type</param>
        /// <param name="affinityGroupName">Affinity Group Name</param>
        internal void DeleteAffinityGroupIfTaskCancelled(ResourceType resourceType, string affinityGroupName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            AffinityGroupListResponse affinityGroupResponse = GetAffinityGroupListResponseFromMSAzure(
               importParameters.DestinationSubscriptionSettings.Credentials);

            var affinityGroupInDestinationSubscription = (from ag in affinityGroupResponse.AffinityGroups
                                                          where (string.Compare(ag.Name, affinityGroupName,
                                                          StringComparison.CurrentCultureIgnoreCase) == 0)
                                                          select ag).FirstOrDefault();
            if (affinityGroupInDestinationSubscription != null)
            {
                Logger.Info(methodName, string.Format(ProgressResources.DeleteAGOnTaskCancelled, affinityGroupName), ResourceType.AffinityGroup.ToString(), affinityGroupName);
                using (var client = new ManagementClient(importParameters.DestinationSubscriptionSettings.Credentials,
                    importParameters.DestinationSubscriptionSettings.ServiceUrl))
                {
                    try
                    {
                        client.AffinityGroups.Delete(affinityGroupName);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(methodName, ex, ResourceType.AffinityGroup.ToString(), affinityGroupName);
                        // Ignore the exception if occurs in cleanup process
                    }
                }
            }
        }

        ///<summary>
        /// Deletes the cloud service if it is started creating and the corresponding task is got cancelled later.
        /// </summary>
        /// <param name="resourceType">Resource Type</param>
        /// <param name="serviceName">Service Name</param>       
        internal void DeleteCloudServiceIfTaskCancelled(ResourceType resourceType, string serviceName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            HostedServiceListResponse cloudserviceResponse = GetCloudServiceListResponseFromMSAzure(
                        importParameters.DestinationSubscriptionSettings.Credentials, importParameters.DestinationSubscriptionSettings.ServiceUrl);

            var cloudServiceInDestinationSubscription = (from cs in cloudserviceResponse
                                                         where (string.Compare(cs.ServiceName, serviceName,
                                                          StringComparison.CurrentCultureIgnoreCase) == 0)
                                                         select cs).FirstOrDefault();
            if (cloudServiceInDestinationSubscription != null)
            {
                Logger.Info(methodName, string.Format(ProgressResources.DeleteCloudServiceOnTaskCancelled, serviceName), ResourceType.AffinityGroup.ToString(), serviceName);
                using (var client = new ComputeManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
                {
                    try
                    {
                        client.HostedServices.Delete(serviceName);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(methodName, ex, ResourceType.CloudService.ToString(), serviceName);
                        // Ignore the exception if occurs in cleanup process
                    }
                }
            }
        }

        
        /// <summary>
        /// Deletes the virtual machine if it is started creating and the corresponding task is got cancelled later.        
        /// </summary>
        /// <param name="resourceType">Resource Type</param>
        /// <param name="serviceName">Service Name</param>
        /// <param name="deploymentName">Deployment Name</param>
        /// <param name="virtualMachineName">Virtual Machine Name</param>
        internal void DeleteVirtualMachineIfTaskCancelled(ResourceType resourceType, string serviceName, string deploymentName, string virtualMachineName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (GetVirtualMachinesResponseFromMSAzure(importParameters.DestinationSubscriptionSettings.Credentials, importParameters.DestinationSubscriptionSettings.ServiceUrl, serviceName, deploymentName, virtualMachineName) != null)
            {
                Logger.Info(methodName, string.Format(ProgressResources.DeleteVirtualMachineOnTaskCancelled, virtualMachineName), ResourceType.AffinityGroup.ToString(), serviceName);
                using (var computeClient = new ComputeManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
                {
                    try
                    {
                        computeClient.VirtualMachines.Delete(serviceName, deploymentName, virtualMachineName, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(methodName, ex, ResourceType.VirtualMachine.ToString(), virtualMachineName);
                        // Ignore the exception if occurs in cleanup process
                    }
                }

            }
        }

        /// <summary>
        /// Deletes the storage account if it is started creating and the corresponding task is got cancelled later.
        /// </summary>
        /// <param name="resourceType">Resource Type</param>
        /// <param name="storageAccountName">Storage Account Name</param>
        internal void DeleteStorageIfTaskCancelled(ResourceType resourceType, string storageAccountName)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var storageAccountResponse = GetStorageAccountListResponseFromMSAzure(importParameters.DestinationSubscriptionSettings.Credentials);
            var storageAccountInDestinationSubscription = (from sa in storageAccountResponse
                                                           where (string.Compare(sa.Name, storageAccountName,
                                                            StringComparison.CurrentCultureIgnoreCase) == 0)
                                                           select sa).FirstOrDefault();
            if (storageAccountInDestinationSubscription != null)
            {
                Logger.Info(methodName, string.Format(ProgressResources.DeleteStorageAccountOnTaskCancelled, storageAccountName), ResourceType.StorageAccount.ToString(), storageAccountName);
                using (var client = new StorageManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
                {
                    try
                    {
                        client.StorageAccounts.Delete(storageAccountName);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(methodName, ex, ResourceType.StorageAccount.ToString(), storageAccountName);
                        // Ignore the exception if occurs in cleanup process
                    }
                }
            }
        }

        #endregion

        #region Update Metadata File

        /// <summary>
        /// Update the metadata file with resource status IsImported
        /// </summary>
        /// <param name="resourceType">Type of resource to be updated in metadata file</param>
        /// <param name="resourceName">Name of the resource</param>
        /// <param name="isImported">If reource is imported successfully</param>
        /// <param name="parentResourceName">Name of parent resource</param>
        internal void UpdateMedatadaFile(ResourceType resourceType, string resourceName = null, bool isImported = true, string parentResourceName = null)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, resourceType.ToString(), resourceName);

            foreach (DataCenter dataCenter in sourceSubscriptionMetadata.DataCenters)
            {
                switch (resourceType)
                {
                    case ResourceType.AffinityGroup:
                        {
                            // For Affinity Group
                            dataCenter.AffinityGroups = dataCenter.AffinityGroups.Select(affinityGroup =>
                            {
                                if (affinityGroup.AffinityGroupDetails.Name.Equals(GetSourceResourceName(ResourceType.AffinityGroup, resourceName),
                                    StringComparison.InvariantCultureIgnoreCase))
                                {
                                    affinityGroup.IsImported = isImported;
                                }
                                return affinityGroup;
                            }).ToList();
                        }
                        break;
                    case ResourceType.NetworkConfiguration:
                        {
                            // For Network Configuration
                            if (dataCenter.NetworkConfiguration != null)
                            {
                                dataCenter.NetworkConfiguration.IsImported = isImported;
                            }
                        }
                        break;
                    case ResourceType.StorageAccount:
                        {
                            // For Storage Account
                            dataCenter.StorageAccounts = dataCenter.StorageAccounts.Select(storageAccount =>
                            {
                                if (storageAccount.StorageAccountDetails.Name.Equals(GetSourceResourceName(ResourceType.StorageAccount, resourceName),
                                    StringComparison.InvariantCultureIgnoreCase))
                                {
                                    storageAccount.IsImported = isImported;
                                }
                                return storageAccount;
                            }).ToList();
                        }
                        break;
                    case ResourceType.CloudService:
                        {
                            // For Cloud Service
                            dataCenter.CloudServices = dataCenter.CloudServices.Select(service =>
                            {
                                if (service.CloudServiceDetails.ServiceName.Equals(GetSourceResourceName(ResourceType.CloudService, resourceName),
                                   StringComparison.InvariantCultureIgnoreCase))
                                {
                                    service.IsImported = isImported;
                                }
                                return service;
                            }).ToList();
                        }
                        break;
                    case ResourceType.Deployment:
                        {
                            // For Deployment
                            dataCenter.CloudServices = dataCenter.CloudServices.Select(service =>
                            {
                                if (service.DeploymentDetails != null &&
                                     service.DeploymentDetails.Name.Equals(GetSourceResourceName(ResourceType.Deployment, resourceName,
                                     ResourceType.CloudService, parentResourceName),
                                    StringComparison.InvariantCultureIgnoreCase)
                                    && service.CloudServiceDetails.ServiceName.
                                    Equals(GetSourceResourceName(ResourceType.CloudService, parentResourceName)))
                                {
                                    service.DeploymentDetails.IsImported = isImported;
                                }
                                return service;
                            }).ToList();
                        }
                        break;

                    case ResourceType.VirtualMachine:
                        {
                            // For Virtual Machine
                            dataCenter.CloudServices = dataCenter.CloudServices.Select(service =>
                            {
                                if (service.DeploymentDetails != null && service.CloudServiceDetails.ServiceName.
                                    Equals(GetSourceResourceName(ResourceType.CloudService, parentResourceName)))
                                {
                                    service.DeploymentDetails.VirtualMachines = service.DeploymentDetails.VirtualMachines.Select(vm =>
                                    {
                                        if (vm.VirtualMachineDetails.RoleName.Equals(
                                              GetSourceResourceName(ResourceType.VirtualMachine, resourceName,
                                                             ResourceType.CloudService, parentResourceName),
                                            // resourceName,
                                      StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            vm.IsImported = isImported;
                                        }
                                        return vm;
                                    }).ToList();
                                }
                                return service;
                            }).ToList();
                        }
                        break;
                    case ResourceType.DataCenter:
                        {
                            dataCenter.IsImported = isImported;
                        }
                        break;
                    default:
                        break;
                }
            }
            lock (thisLockFile)
            {
                // Update the metadata file
                File.WriteAllText(importParameters.ImportMetadataFilePath,
                          JsonConvert.SerializeObject(sourceSubscriptionMetadata, Newtonsoft.Json.Formatting.Indented));
            }
            Logger.Info(methodName, ProgressResources.ExecutionCompleted, resourceType.ToString(), resourceName);
        }
        #endregion


        #region REST API Calls


        /// <summary>
        /// Lists the role sizes that are available under the specified subscription.
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <param name="certificate">Subscription specific certificate</param>
        /// <returns>List of role sizes that are available under the specified subscription.</returns>
        private Dictionary<string, string> GetRoleSizes(string subscriptionId, X509Certificate2 certificate)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(string.Format(Constants.ManagementURLRoleSizes, subscriptionId));
                request.Headers.Add(Constants.RequestHeaderVersion20130801);
                request.ClientCertificates.Add(certificate);

                var response = request.GetResponse().GetResponseStream();
                var xmlofResponse = new StreamReader(response).ReadToEnd();
                XElement document = XElement.Parse(xmlofResponse);

                XNamespace ns = Constants.XNameSpace;
                Logger.Info(methodName, ProgressResources.ExecutionCompleted);
                return (from a in document.Descendants(ns + Constants.StringRoleSize)
                        select new
                        {
                            Name = a.Element(ns + Constants.StringName).Value,
                            Cores = a.Element(ns + Constants.StringCores).Value,

                        }).ToDictionary(i => i.Name, i => i.Cores);
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex);
                throw;
            }
        }
        #endregion
    }
}
