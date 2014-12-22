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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Management.Network;
using Microsoft.WindowsAzure.Management.Network.Models;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Polenter.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Class for exporting resources.
    /// </summary>
    internal class ResourceExporter
    {
        #region Private Members

        private ExportParameters exportParameters;
        private DCMigrationManager dcMigration;


        #endregion

        public ResourceExporter(ExportParameters parameters, DCMigrationManager dcMigration)
        {
            // Set export parameters.
            exportParameters = parameters;
            this.dcMigration = dcMigration;
        }

        #region Build a Data Structurefor for DC Migration by exporting resources from recieved MS azure responses.

        /// <summary>
        /// Exports Subscription metadata.
        /// </summary>
        /// <returns>Subscription details/></returns>
        internal Subscription ExportSubscriptionMetadata()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            Subscription subscription = new Subscription()
            {
                Name = exportParameters.SourceSubscriptionSettings.Name                
            };

            Logger.Info(methodName, string.Format(ProgressResources.ExportDataCenterStarted, exportParameters.SourceDCName), ResourceType.DataCenter.ToString(), exportParameters.SourceDCName);
            dcMigration.ReportProgress(string.Format(ProgressResources.ExportDataCenterStarted, exportParameters.SourceDCName));

            AffinityGroupListResponse affinityGroupResponse = GetAffinityGroupListResponseFromMSAzure(exportParameters.SourceSubscriptionSettings.Credentials);
            HostedServiceListResponse cloudserviceResponse = GetCloudServiceListResponseFromMSAzure(exportParameters.SourceSubscriptionSettings.Credentials, exportParameters.SourceSubscriptionSettings.ServiceUrl);
            NetworkGetConfigurationResponse networkResponse = GetNetworkConfigurationFromMSAzure(exportParameters.SourceSubscriptionSettings.Credentials, exportParameters.SourceSubscriptionSettings.ServiceUrl);
            StorageAccountListResponse storageAccountResponse = GetStorageAccountListResponseFromMSAzure(exportParameters.SourceSubscriptionSettings.Credentials);

            // Create an instance of data center.
            var dataCenter = new DataCenter
                {
                    LocationName = exportParameters.SourceDCName,
                };

            // Get all affinity groups.            
            Logger.Info(methodName, ProgressResources.ExportAffinityGroupStarted, ResourceType.AffinityGroup.ToString());
            dcMigration.ReportProgress(ProgressResources.ExportAffinityGroupStarted);

            var affinityGroups = ExportAffinityGroups(affinityGroupResponse);
            dataCenter.AffinityGroups.AddRange(affinityGroups.ToList());
            int stageCount = 1;
            Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, stageCount, Constants.ExportTotalStages));
            dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, stageCount, Constants.ExportTotalStages));

            List<string> affinityGroupNamesInDC = affinityGroups.Select(ag => ag.AffinityGroupDetails.Name).ToList();

            Logger.Info(methodName, ProgressResources.ExportVNetConfigurationStarted, ResourceType.VirtualNetwork.ToString());
            dcMigration.ReportProgress(ProgressResources.ExportVNetConfigurationStarted);

            // Filter and Export network configuration file.
            dataCenter.NetworkConfiguration = ExportVNetConfiguration(networkResponse, affinityGroupNamesInDC);
            Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ExportTotalStages));
            dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, stageCount, Constants.ExportTotalStages));

            Logger.Info(methodName, ProgressResources.ExportCloudServicesStarted, ResourceType.CloudService.ToString());
            dcMigration.ReportProgress(ProgressResources.ExportCloudServicesStarted);

            // Get cloud services for affinityGroupNamesInDC or for SourceDCName
            dataCenter.CloudServices.AddRange(ExportCloudServices(affinityGroupNamesInDC, cloudserviceResponse));
            Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ExportTotalStages));
            dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, stageCount, Constants.ExportTotalStages));

            Logger.Info(methodName, ProgressResources.ExportStorageAccountStarted, ResourceType.StorageAccount.ToString());
            dcMigration.ReportProgress(ProgressResources.ExportStorageAccountStarted);

            // Get list of storage accounts 
            dataCenter.StorageAccounts.AddRange(ExportStorageAccounts(affinityGroupNamesInDC, storageAccountResponse));
            Logger.Info(methodName, string.Format(ProgressResources.CompletedStages, ++stageCount, Constants.ExportTotalStages));
            dcMigration.ReportProgress(string.Format(ProgressResources.CompletedStages, stageCount, Constants.ExportTotalStages));

            // Add the data center into subscription.
            subscription.DataCenters.Add(dataCenter);
            Logger.Info(methodName, string.Format(ProgressResources.ExportDataCenterCompleted, dataCenter.LocationName), ResourceType.DataCenter.ToString());
            Logger.Info(methodName, ProgressResources.ExecutionCompleted);
            return subscription;
        }

        /// <summary>
        /// Exports list of affinity groups.
        /// </summary>        
        /// <param name="affinityGroupResponse">List of affinity groups</param>
        /// <returns>List of affinity groups</returns>
        private List<AffinityGroup> ExportAffinityGroups(AffinityGroupListResponse affinityGroupResponse)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.AffinityGroup.ToString());
            try
            {
                Logger.Info(methodName, ProgressResources.ExportAffinityGroupStarted, ResourceType.AffinityGroup.ToString());
                List<AffinityGroup> affinityGroups = (from affinityGroup in affinityGroupResponse
                                                      where (string.Compare(affinityGroup.Location, exportParameters.SourceDCName, StringComparison.CurrentCultureIgnoreCase) == 0)
                                                      select new AffinityGroup
                                                      {
                                                          AffinityGroupDetails = affinityGroup,
                                                          IsImported = false
                                                      }).ToList();
                Logger.Info(methodName, ProgressResources.ExportAffinityGroupCompleted, ResourceType.AffinityGroup.ToString());
                return affinityGroups;
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex);
                throw;
            }
        }

        /// <summary>
        /// Exports list of production cloud services.
        /// </summary>
        /// <param name="dcAffinityGroupNames">List of affinity group names for which cloud service to be filtered out</param>
        /// <param name="services">Hosted service response to filter services by SourceDC name</param>
        /// <returns>List of production cloud services</returns>        
        private List<CloudService> ExportCloudServices(List<string> dcAffinityGroupNames, HostedServiceListResponse services)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.CloudService.ToString());
            List<CloudService> cloudServices = new List<CloudService>();

            using (var client = new ComputeManagementClient(exportParameters.SourceSubscriptionSettings.Credentials, exportParameters.SourceSubscriptionSettings.ServiceUrl))
            {
                DeploymentGetResponse deployment = null;
                CloudService cloudService = null;

                foreach (var service in services)
                {
                    if ((string.Compare(service.Properties.Location, exportParameters.SourceDCName, StringComparison.CurrentCultureIgnoreCase) == 0) ||
                             (service.Properties.Location == null && (dcAffinityGroupNames.Contains(service.Properties.AffinityGroup))))
                    {
                        Logger.Info(methodName, string.Format(ProgressResources.ExportCloudServiceStarted, service.ServiceName), ResourceType.CloudService.ToString(), service.ServiceName);
                        try
                        {
                            // Get deployment of production slot for specific service.
                            deployment = client.Deployments.GetBySlot(service.ServiceName, DeploymentSlot.Production);
                        }
                        catch (CloudException ex)
                        {
                            deployment = null;
                            if (string.Compare(ex.ErrorCode, Constants.ResourceNotFound) != 0)
                            {
                                Logger.Error(methodName, ex, ResourceType.CloudService.ToString(), service.ServiceName);
                                throw;
                            }
                        }
                        finally
                        {
                            // Create CloudService object if cloud service exist in SourceDCName location.
                            // Or create when service exist in affinity group and affinity group location matches with SourceDName.

                            var deploymentDetails = deployment != null ? ExportDeployment(service.ServiceName, deployment) : null;

                            cloudService = new CloudService
                            {
                                IsImported = false,
                                CloudServiceDetails = service,
                                DeploymentDetails = deploymentDetails,
                            };
                            cloudServices.Add(cloudService);
                            Logger.Info(methodName, string.Format(ProgressResources.ExportCloudServiceCompleted, service.ServiceName), ResourceType.CloudService.ToString(), service.ServiceName);
                        }
                    }
                }
            }
            Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.CloudService.ToString());
            return cloudServices;
        }

        /// <summary>
        /// Exports list of production cloud services.
        /// </summary>
        /// <param name="dcAffinityGroupNames">Affinity group names for which storage accounts to be filtered out</param>
        /// <param name="storageAccounts">StorageAccountListResponse to filter accounts by SourceDC account names</param>
        /// <returns>List of production cloud services</returns>        
        private List<Azure.DataCenterMigration.Models.StorageAccount> ExportStorageAccounts(List<string> dcAffinityGroupNames, StorageAccountListResponse storageAccounts)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExportStorageAccountStarted, ResourceType.StorageAccount.ToString());
            try
            {
                List<Azure.DataCenterMigration.Models.StorageAccount> storageAccountsInDC =
                    (from account in storageAccounts
                     where
                     ((string.Compare(account.Properties.Location, exportParameters.SourceDCName,
                     StringComparison.CurrentCultureIgnoreCase) == 0) ||
                            (account.Properties.Location == null && (dcAffinityGroupNames.Contains(account.Properties.AffinityGroup))))
                     select new Azure.DataCenterMigration.Models.StorageAccount
                     {
                         IsImported = false,
                         StorageAccountDetails = account
                     }
                    ).ToList();
                Logger.Info(methodName, ProgressResources.ExportStorageAccountCompleted, ResourceType.StorageAccount.ToString());
                return storageAccountsInDC;
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex);
                throw;
            }
        }

        /// <summary>
        /// Exports service specific deployment details.
        /// </summary>
        /// <param name="serviceName">CloudService name</param>
        /// <param name="deploymentResponse">Deployment response recieved from Microsoft Azure API</param>
        /// <returns>Cloud service specific deployment details</returns>
        private Deployment ExportDeployment(string serviceName, DeploymentGetResponse deploymentResponse)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, string.Format(ProgressResources.ExportDeploymentStarted, deploymentResponse.Name, serviceName), ResourceType.Deployment.ToString(), deploymentResponse.Name);
            try
            {
                Deployment deployment = new Deployment
                {
                    DnsSettings = deploymentResponse.DnsSettings,
                    ExtendedProperties = deploymentResponse.ExtendedProperties,
                    Label = deploymentResponse.Label,
                    LoadBalancers = deploymentResponse.LoadBalancers,
                    Name = deploymentResponse.Name,
                    ReservedIPName = deploymentResponse.ReservedIPName,
                    VirtualIPAddresses = deploymentResponse.VirtualIPAddresses,
                    VirtualNetworkName = deploymentResponse.VirtualNetworkName,
                    // Export Virtual Machines
                    VirtualMachines = ExportVirtualMachines(serviceName, deploymentResponse),
                    IsImported = false
                };
                Logger.Info(methodName, string.Format(ProgressResources.ExportDeploymentCompleted, deploymentResponse.Name, serviceName), ResourceType.Deployment.ToString(), deploymentResponse.Name);
                return deployment;
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex, ResourceType.Deployment.ToString(), deploymentResponse.Name);
                throw;
            }
        }

        /// <summary>
        /// Exports list of cloudservice specific virtual machines.
        /// </summary>
        /// <param name="serviceName">CloudService name</param>
        /// <param name="deployment">Deployment details </param>
        /// <returns>List of cloudservice specific virtual machines</returns>
        private List<VirtualMachine> ExportVirtualMachines(string serviceName, DeploymentGetResponse deployment)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, string.Format(ProgressResources.ExportVirtualMachineStarted, serviceName), ResourceType.VirtualMachine.ToString());
            try
            {
                List<VirtualMachine> virtualMachines = new List<VirtualMachine>();
                // Retrieve roles information for PersistentVMRole type.
                virtualMachines = (from role in deployment.Roles
                                   where role.RoleType == Constants.PersistentVMRole
                                   select new VirtualMachine
                                   {
                                       VirtualMachineDetails = role,
                                       IsImported = false,
                                   }).ToList();

                Logger.Info(methodName, string.Format(ProgressResources.ExportVirtualMachineCompleted, serviceName), ResourceType.VirtualMachine.ToString());
                return virtualMachines;
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex, ResourceType.VirtualMachine.ToString());
                throw;
            }
        }

        /// <summary>
        /// Exports Vnetconfig data from Source Subscription. Serialises the exported configurations into NetworkConfiguration class
        /// </summary>
        /// <param name="configuration">The network configuration for subscription</param>
        /// <param name="affinityGroupNames"> </param>
        /// <returns>Network configurations </returns>
        private NetworkConfiguration ExportVNetConfiguration(NetworkGetConfigurationResponse configuration, List<string> affinityGroupNames)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExportVNetConfigurationStarted, ResourceType.VirtualNetwork.ToString());
            try
            {
                if (configuration == null)
                {
                    return null;
                }
                var reader = new StringReader(configuration.Configuration);
                var serializer = new XmlSerializer(typeof(NetworkConfiguration));
                NetworkConfiguration netConfiguration = (NetworkConfiguration)serializer.Deserialize(reader);

                if (netConfiguration.VirtualNetworkConfiguration != null && netConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                {
                    // Filter the virtaul networks - Find the networks for which we have considered affinity groups
                    var requiredVirtualNetworkSites = (netConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.
                        Where(vn => affinityGroupNames.Contains(vn.AffinityGroup) || string.Compare(vn.Location, exportParameters.SourceDCName, StringComparison.CurrentCultureIgnoreCase) == 0)).ToArray();

                    List<string> dnsNames = new List<string>();
                    List<string> localNetNames = new List<string>();
                    foreach (var vns in requiredVirtualNetworkSites)
                    {
                        if (vns.DnsServersRef != null)
                        {
                            dnsNames.AddRange(vns.DnsServersRef.Select(dns => dns.name).ToList());
                        }
                        if (vns.Gateway != null && vns.Gateway.ConnectionsToLocalNetwork != null && vns.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef != null)
                        {
                            localNetNames.Add(vns.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef.name);
                        }
                    }

                    if (netConfiguration.VirtualNetworkConfiguration.Dns != null &&
                        netConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null)
                    {
                        //Remove DnsServers which are not related to required virtual networks
                        netConfiguration.VirtualNetworkConfiguration.Dns.DnsServers =
                            netConfiguration.VirtualNetworkConfiguration.Dns.DnsServers.Where(dns => dnsNames.Distinct().Contains(dns.name)).ToArray();
                    }

                    if (netConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null)
                    {
                        //Remove LocalNetworks which are not related to required virtual networks
                        netConfiguration.VirtualNetworkConfiguration.LocalNetworkSites =
                            netConfiguration.VirtualNetworkConfiguration.LocalNetworkSites.Where(lns => localNetNames.Distinct().Contains(lns.name)).ToArray();
                    }
                    //Set VirtualNetworkSites
                    netConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites = requiredVirtualNetworkSites;

                    Logger.Info(methodName, ProgressResources.ExportVNetConfigurationCompleted, ResourceType.VirtualNetwork.ToString());
                }
                return netConfiguration;
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex, ResourceType.VirtualNetwork.ToString());
                throw;
            }
        }



        #endregion

        #region Management API alls

        /// <summary>        
        /// Gets list of hosted service operation response from MS azure using API call.        
        /// </summary>
        /// <returns>List of hosted service operation response for subscription </returns>
        private HostedServiceListResponse GetCloudServiceListResponseFromMSAzure(SubscriptionCloudCredentials credentials, Uri serviceUrl)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetServicesFromMSAzureStarted, ResourceType.CloudService.ToString());
            dcMigration.ReportProgress(ProgressResources.GetServicesFromMSAzureStarted);
            try
            {
                using (var client = new ComputeManagementClient(credentials, serviceUrl))
                {
                    // Call management API to get list of CloudServices.
                    //HostedServiceListResponse serviceResponse = Retry.RetryOperation(() => client.HostedServices.List(), exportParameters.RetryCount, exportParameters.MinBackOff, exportParameters.MaxBackOff, exportParameters.DeltaBackOff, ResourceType.CloudService);
                    HostedServiceListResponse serviceResponse = Retry.RetryOperation(() => client.HostedServices.List(),
                       (BaseParameters)exportParameters,
                        ResourceType.CloudService);
                    Logger.Info(methodName, ProgressResources.GetServicesFromMSAzureCompleted, ResourceType.CloudService.ToString());
                    return serviceResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex, ResourceType.CloudService.ToString());
                throw;
            }
        }

        /// <summary>
        /// Gets list of affinity group operation response from MS azure using API call.
        /// </summary>
        /// <returns>List of affinity group operation response for subscription </returns>
        private AffinityGroupListResponse GetAffinityGroupListResponseFromMSAzure(SubscriptionCloudCredentials credentials)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetAffinityGroupsFromMSAzureStarted, ResourceType.AffinityGroup.ToString());
            dcMigration.ReportProgress(ProgressResources.GetAffinityGroupsFromMSAzureStarted);
            try
            {
                using (var client = new ManagementClient(credentials))
                {
                    // Call management API to get list of affinity groups.

                    AffinityGroupListResponse agResponse = Retry.RetryOperation(() => client.AffinityGroups.List(),
                       (BaseParameters)exportParameters, ResourceType.AffinityGroup);
                    Logger.Info(methodName, ProgressResources.GetAffinityGroupsFromMSAzureCompleted, ResourceType.AffinityGroup.ToString());
                    return agResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex, ResourceType.AffinityGroup.ToString());
                throw;
            }
        }

        /// <summary>
        /// Gets network configuration from MS azure using management API call.
        /// </summary>
        /// <param name="credentials">Source subscription credentials</param>
        /// <param name="serviceUrl">Subscription service Url</param>
        /// <returns>Network configuration for subscription</returns>
        private NetworkGetConfigurationResponse GetNetworkConfigurationFromMSAzure(SubscriptionCloudCredentials credentials, Uri serviceUrl)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetVirtualNetworkConfigFromMSAzureStarted, ResourceType.VirtualNetwork.ToString());
            dcMigration.ReportProgress(ProgressResources.GetVirtualNetworkConfigFromMSAzureStarted);
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
                    if (string.Compare(cex.ErrorCode, Constants.ResourceNotFound, StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        return null;
                    }
                    else
                    {
                        Logger.Error(methodName, cex, ResourceType.VirtualNetwork.ToString());
                        throw;
                    }
                }

            }
        }

        /// <summary>
        /// Gets list of storage account operation response from MS azure using API call. 
        /// </summary>
        /// <param name="credentials">Source Subscription Credentials</param>
        /// <returns>list of storage account</returns>
        internal StorageAccountListResponse GetStorageAccountListResponseFromMSAzure(SubscriptionCloudCredentials credentials)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            dcMigration.ReportProgress(ProgressResources.GetStorageAccountsFromMSAzureStarted);
            try
            {
                Logger.Info(methodName, ProgressResources.GetStorageAccountsFromMSAzureStarted, ResourceType.StorageAccount.ToString());
                using (var client = new StorageManagementClient(credentials))
                {
                    // Call management API to get list of storage accounts.
                    StorageAccountListResponse storageResponse = Retry.RetryOperation(() => client.StorageAccounts.List(),
                        (BaseParameters)exportParameters, ResourceType.StorageAccount);
                    Logger.Info(methodName, ProgressResources.GetStorageAccountsFromMSAzureCompleted, ResourceType.StorageAccount.ToString());
                    return storageResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex, ResourceType.StorageAccount.ToString());
                throw;
            }
        }
        #endregion
    }

}
