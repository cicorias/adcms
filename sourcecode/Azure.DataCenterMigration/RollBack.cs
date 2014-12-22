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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.WindowsAzure.Management.Storage;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.WindowsAzure.Management.Network;
using Microsoft.WindowsAzure.Management.Network.Models;
using log4net;
using System.Diagnostics;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Class to rollback all imported resources.
    /// </summary>
    internal class RollBack
    {
        private Subscription subscription;
        private ImportParameters importParameters;
        private DCMigrationManager dcMigrationManager;
        private ResourceImporter resourceImporter;


        public RollBack(ImportParameters importParameters, Subscription subscription, DCMigrationManager dcMigrationManager,
            ResourceImporter resourceImporter)
        {
            this.subscription = subscription;
            this.importParameters = importParameters;
            this.dcMigrationManager = dcMigrationManager;
            this.resourceImporter = resourceImporter;
        }

        /// <summary>
        /// Rollback all imported resources.
        /// </summary>
        internal void RollBackResources()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted);
            Logger.Info(methodName, ProgressResources.RollbackStarted);
            dcMigrationManager.ReportProgress(ProgressResources.RollbackStarted);
            List<string> listOfImportedResources = new List<string>();
            foreach (var datacenter in subscription.DataCenters)
            {
                // Rollback all services.
                RollBackServices(datacenter.CloudServices.Where(csImported => csImported.IsImported == true).Select(cs =>
                     resourceImporter.GetDestinationResourceName(ResourceType.CloudService, cs.CloudServiceDetails.ServiceName)
                    ).ToList());
                int stageCount = 1;
                Logger.Info(methodName, string.Format(ProgressResources.RollbackCompletedStages, stageCount,
                    Constants.RollBackTotalStages), ResourceType.CloudService.ToString());
                dcMigrationManager.ReportProgress(string.Format(ProgressResources.RollbackCompletedStages, stageCount,
                    Constants.RollBackTotalStages));

                // Rollback all storage accounts.
                RollBackStorageAccounts(datacenter.StorageAccounts.Where(saImported => saImported.IsImported == true).Select(sa =>
                    resourceImporter.GetDestinationResourceName(ResourceType.StorageAccount, sa.StorageAccountDetails.Name)
                    ).ToList());
                Logger.Info(methodName, string.Format(ProgressResources.RollbackCompletedStages, stageCount++,
                    Constants.RollBackTotalStages), ResourceType.StorageAccount.ToString());
                dcMigrationManager.ReportProgress(string.Format(ProgressResources.RollbackCompletedStages, stageCount,
                    Constants.RollBackTotalStages));

                dcMigrationManager.ReportProgress(ProgressResources.RollbackVirtualNetworks);
                Logger.Info(methodName, ProgressResources.RollbackVirtualNetworks, ResourceType.NetworkConfiguration.ToString());
                // Rollback all virtual networks.
                if (datacenter.NetworkConfiguration != null && datacenter.NetworkConfiguration.IsImported)
                {
                    RollBackVirtualNetworks(datacenter.NetworkConfiguration);
                }
                Logger.Info(methodName, string.Format(ProgressResources.RollbackCompletedStages, stageCount++,
                    Constants.RollBackTotalStages), ResourceType.NetworkConfiguration.ToString());
                dcMigrationManager.ReportProgress(string.Format(ProgressResources.RollbackCompletedStages, stageCount,
                    Constants.RollBackTotalStages));

                // Rollback all affinity groups.
                RollBackAffinityGroups(datacenter.AffinityGroups.Where(agImported => agImported.IsImported == true).Select(ag =>
                resourceImporter.GetDestinationResourceName(ResourceType.AffinityGroup, ag.AffinityGroupDetails.Name)
                ).ToList());

                Logger.Info(methodName, string.Format(ProgressResources.RollbackCompletedStages, stageCount++,
                    Constants.RollBackTotalStages), ResourceType.AffinityGroup.ToString());
                dcMigrationManager.ReportProgress(string.Format(ProgressResources.RollbackCompletedStages, stageCount,
                    Constants.RollBackTotalStages));

                Logger.Info(methodName, ProgressResources.RollbackCompleted);
                dcMigrationManager.ReportProgress(ProgressResources.RollbackCompleted);

                Logger.Info(methodName, ProgressResources.ExecutionCompleted);
            }
        }

        /// <summary>
        /// Rollback all imported cloud services.
        /// </summary>
        /// <param name="cloudServices">Cloud services to be deleted.</param>
        private void RollBackServices(List<string> cloudServices)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.CloudService.ToString());
            Logger.Info(methodName, ProgressResources.RollbackCloudServices, ResourceType.CloudService.ToString());
            dcMigrationManager.ReportProgress(ProgressResources.RollbackCloudServices);
            Stopwatch swTotalServices = new Stopwatch();
            swTotalServices.Start();
            //string origServiceName = null;
            if (cloudServices.Count > 0)
            {
                using (var client = new ComputeManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
                {
                    Parallel.ForEach(cloudServices, cloudService =>
                        {
                            try
                            {
                                Stopwatch swService = new Stopwatch();
                                swService.Start();
                                string origServiceName = resourceImporter.GetSourceResourceName(ResourceType.CloudService, cloudService);
                                Retry.RetryOperation(() => client.HostedServices.DeleteAll(cloudService),
                                    (BaseParameters)importParameters, ResourceType.CloudService, cloudService, ignoreResourceNotFoundEx:true);

                                CloudService service = subscription.DataCenters.FirstOrDefault().CloudServices.
                                    Where(ser => (ser.CloudServiceDetails.ServiceName == origServiceName)).FirstOrDefault();
                                if (service.DeploymentDetails != null)
                                {
                                    string deploymentName = resourceImporter.GetDestinationResourceName(
                                        ResourceType.Deployment, service.DeploymentDetails.Name, ResourceType.CloudService,
                                        resourceImporter.GetDestinationResourceName(ResourceType.CloudService, service.CloudServiceDetails.ServiceName)
                                        );
                                    resourceImporter.UpdateMedatadaFile(ResourceType.Deployment, deploymentName, false,
                                          resourceImporter.GetDestinationResourceName(ResourceType.CloudService, service.CloudServiceDetails.ServiceName)
                                        );
                                    Logger.Info(methodName, string.Format(ProgressResources.RollbackDeployment, service.DeploymentDetails.Name,
                                        service.DeploymentDetails.Name), ResourceType.Deployment.ToString(), service.DeploymentDetails.Name);

                                    foreach (VirtualMachine vm in service.DeploymentDetails.VirtualMachines)
                                    {
                                        string virtualmachineName = resourceImporter.GetDestinationResourceName(
                                        ResourceType.VirtualMachine, vm.VirtualMachineDetails.RoleName, ResourceType.CloudService,
                                        resourceImporter.GetDestinationResourceName(ResourceType.CloudService, service.CloudServiceDetails.ServiceName)
                                        );

                                        resourceImporter.UpdateMedatadaFile(ResourceType.VirtualMachine, virtualmachineName, false,
                                              resourceImporter.GetDestinationResourceName(ResourceType.CloudService, service.CloudServiceDetails.ServiceName)
                                            );
                                        Logger.Info(methodName, string.Format(ProgressResources.RollbackVirtualMachine, vm.VirtualMachineDetails.RoleName),
                                            ResourceType.VirtualMachine.ToString(), vm.VirtualMachineDetails.RoleName);
                                    }
                                }
                                resourceImporter.UpdateMedatadaFile(ResourceType.CloudService, cloudService, false);
                                swService.Stop();
                                Logger.Info(methodName, string.Format(ProgressResources.RollbackCloudService, cloudService,swService.Elapsed.Days, swService.Elapsed.Hours,
                                    swService.Elapsed.Minutes, swService.Elapsed.Seconds), ResourceType.CloudService.ToString(), cloudService);
                            }
                            catch (AggregateException exAgg)
                            {
                                foreach (var ex in exAgg.InnerExceptions)
                                {
                                    Logger.Error(methodName, exAgg, ResourceType.CloudService.ToString(), cloudService);
                                }
                                throw;
                            }
                        });
                    Logger.Info(methodName, ProgressResources.RollbackCloudServicesWaiting, ResourceType.CloudService.ToString());
                    dcMigrationManager.ReportProgress(ProgressResources.RollbackCloudServicesWaiting);
                    Task.Delay(Constants.DelayTimeInMilliseconds_Rollback).Wait();
                }
            }
            swTotalServices.Stop();
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime,swTotalServices.Elapsed.Days, swTotalServices.Elapsed.Hours, swTotalServices.Elapsed.Minutes,
                swTotalServices.Elapsed.Seconds), ResourceType.CloudService.ToString());
        }

        /// <summary>
        /// Rollback all imported storage accounts.
        /// </summary>
        /// <param name="storageAccounts">Storage accounts to be deleted.</param>
        private void RollBackStorageAccounts(List<string> storageAccounts)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.StorageAccount.ToString());
            dcMigrationManager.ReportProgress(ProgressResources.RollbackStorageAccounts);
            Logger.Info(methodName, ProgressResources.RollbackStorageAccounts, ResourceType.StorageAccount.ToString());
            Stopwatch swTotalstorages = new Stopwatch();
            swTotalstorages.Start();
            if (storageAccounts.Count > 0)
            {
                using (var client = new StorageManagementClient(importParameters.DestinationSubscriptionSettings.Credentials))
                {
                    Parallel.ForEach(storageAccounts, storageAccount =>
                    {
                        Stopwatch swStorage = new Stopwatch();
                        swStorage.Start();
                        try
                        {
                            Retry.RetryOperation(() => client.StorageAccounts.Delete(storageAccount),
                               (BaseParameters)importParameters,
                                ResourceType.StorageAccount, storageAccount, ignoreResourceNotFoundEx: true);

                            resourceImporter.UpdateMedatadaFile(ResourceType.StorageAccount, storageAccount, false);
                            swStorage.Stop();
                            Logger.Info(methodName, string.Format(ProgressResources.RollbackStorageAccount, storageAccount,swStorage.Elapsed.Days, swStorage.Elapsed.Hours,
                                swStorage.Elapsed.Minutes, swStorage.Elapsed.Seconds), ResourceType.StorageAccount.ToString(), storageAccount);
                        }
                        catch (AggregateException exAgg)
                        {
                            foreach (var ex in exAgg.InnerExceptions)
                            {
                                Logger.Error(methodName, exAgg, ResourceType.StorageAccount.ToString(), storageAccount);
                            }
                                throw;
                            }
                    });

                    Logger.Info(methodName, ProgressResources.RollbackStorageAccountsWaiting, ResourceType.StorageAccount.ToString());
                    dcMigrationManager.ReportProgress(ProgressResources.RollbackStorageAccountsWaiting);
                    Task.Delay(Constants.DelayTimeInMilliseconds_Rollback).Wait();
                }
            }
            swTotalstorages.Stop();
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime,swTotalstorages.Elapsed.Days, swTotalstorages.Elapsed.Hours,
                swTotalstorages.Elapsed.Minutes, swTotalstorages.Elapsed.Seconds), ResourceType.StorageAccount.ToString());
        }

        /// <summary>
        /// Rollback all virtual networks
        /// </summary>
        /// <param name="networkConfiguration">Network configuration</param>
        private void RollBackVirtualNetworks(NetworkConfiguration networkConfiguration)
        {
            Stopwatch swVirtualNetwork = new Stopwatch();
            swVirtualNetwork.Start();
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.NetworkConfiguration.ToString());
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

            try
            {
                if (networkConfiguration != null)
                {
                    if (networkConfiguration.VirtualNetworkConfiguration != null)
                    {
                        if (networkConfiguration.VirtualNetworkConfiguration.Dns != null &&
                            networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null &&
                            destinationNetConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers != null)
                        {

                            foreach (var virtualNetworkSite in networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites)
                            {
                                foreach (var dns in networkConfiguration.VirtualNetworkConfiguration.Dns.DnsServers)
                                {
                                    string dnsDestination = resourceImporter.GetDestinationResourceName(ResourceType.DnsServer, dns.name,
                                           ResourceType.VirtualNetworkSite,
                                           resourceImporter.GetDestinationResourceName(ResourceType.VirtualNetworkSite,
                                           virtualNetworkSite.name));

                                    if (!string.IsNullOrEmpty(dnsDestination))
                                    {
                                        destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers =
                                            destinationNetConfiguration.VirtualNetworkConfiguration.Dns.DnsServers.Where(s => s.name != dnsDestination).ToArray();
                                    }
                                }
                            }
                        }
                        if (networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null &&
                            destinationNetConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites != null)
                        {
                            foreach (var virtualNetworkSite in networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites)
                            {
                                foreach (var localNetwork in networkConfiguration.VirtualNetworkConfiguration.LocalNetworkSites)
                                {
                                    string localNetworkDestination = resourceImporter.GetDestinationResourceName(ResourceType.LocalNetworkSite,
                                        localNetwork.name,
                                           ResourceType.VirtualNetworkSite,
                                           resourceImporter.GetDestinationResourceName(ResourceType.VirtualNetworkSite,
                                           virtualNetworkSite.name));
                                    if (!string.IsNullOrEmpty(localNetworkDestination))
                                    {
                                        destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites =
                                            destinationNetConfiguration.VirtualNetworkConfiguration.LocalNetworkSites.Where(s => s.name != localNetworkDestination).ToArray();
                                    }
                                }
                            }
                        }
                        if (networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null &&
                            destinationNetConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration != null &&
                            destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                        {
                            destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites =
                                destinationNetConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.
                                Where(x => !networkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.
                                    Any(x1 => resourceImporter.GetDestinationResourceName(ResourceType.VirtualNetworkSite, x1.name) == x.name)).ToArray();
                        }
                    }
                }

                MemoryStream memoryStream = new MemoryStream();
                XmlWriter writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { Encoding = Encoding.UTF8 });
                serializer.Serialize(writer, destinationNetConfiguration);

                using (var vnetClient = new NetworkManagementClient(importParameters.DestinationSubscriptionSettings.Credentials,
                    importParameters.DestinationSubscriptionSettings.ServiceUrl))
                {
                    OperationStatusResponse response = Retry.RetryOperation(() => vnetClient.Networks.SetConfiguration(
                        new NetworkSetConfigurationParameters
                        {
                            Configuration = Encoding.UTF8.GetString(memoryStream.ToArray())
                        }), (BaseParameters)importParameters, ResourceType.NetworkConfiguration);
                }
                writer.Close();
                memoryStream.Close();
                resourceImporter.UpdateMedatadaFile(ResourceType.NetworkConfiguration, null, false);
                Logger.Info(methodName, ProgressResources.RollbackVirtualNetworksWaiting, ResourceType.NetworkConfiguration.ToString());
                dcMigrationManager.ReportProgress(ProgressResources.RollbackVirtualNetworksWaiting);
                Task.Delay(Constants.DelayTimeInMilliseconds_Rollback).Wait();
                Logger.Info(methodName, ProgressResources.RollbackVirtualNetwork, ResourceType.NetworkConfiguration.ToString());
                swVirtualNetwork.Stop();
                Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime,swVirtualNetwork.Elapsed.Days, swVirtualNetwork.Elapsed.Hours,
                    swVirtualNetwork.Elapsed.Minutes, swVirtualNetwork.Elapsed.Seconds), ResourceType.NetworkConfiguration.ToString());
            }
            catch (CloudException ex)
            {
                if (string.Compare(ex.ErrorCode, Constants.ResourceNotFound, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    Logger.Error(methodName, ex, ResourceType.VirtualNetwork.ToString());
                }
                else
                {
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Rollback all imported affinity groups.
        /// </summary>
        /// <param name="affinityGroups">Affinity groups to be deleted.</param>
        private void RollBackAffinityGroups(List<string> affinityGroups)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.AffinityGroup.ToString());
            dcMigrationManager.ReportProgress(ProgressResources.RollbackAffinityGroups);
            Logger.Info(methodName, ProgressResources.RollbackAffinityGroups, ResourceType.AffinityGroup.ToString());

            Stopwatch swTotalAffGrp = new Stopwatch();
            swTotalAffGrp.Start();
            if (affinityGroups.Count > 0)
            {
                using (var client = new ManagementClient(importParameters.DestinationSubscriptionSettings.Credentials,
                    importParameters.DestinationSubscriptionSettings.ServiceUrl))
                {
                    Parallel.ForEach(affinityGroups, affinityGroup =>
                        {
                            Stopwatch swAffinityGroup = new Stopwatch();
                            swAffinityGroup.Start();
                            try
                            {
                                Retry.RetryOperation(() => client.AffinityGroups.Delete(affinityGroup),
                                    (BaseParameters)importParameters, ResourceType.AffinityGroup, affinityGroup, ignoreResourceNotFoundEx: true);

                                resourceImporter.UpdateMedatadaFile(ResourceType.AffinityGroup, affinityGroup, false);
                                swAffinityGroup.Stop();

                                Logger.Info(methodName, string.Format(ProgressResources.RollbackAffinityGroup, affinityGroup,swAffinityGroup.Elapsed.Days,
                                    swAffinityGroup.Elapsed.Hours, swAffinityGroup.Elapsed.Minutes, swAffinityGroup.Elapsed.Seconds),
                                    ResourceType.AffinityGroup.ToString(), affinityGroup);
                            }
                            catch (AggregateException exAgg)
                            {
                                foreach (var ex in exAgg.InnerExceptions)
                                {
                                    Logger.Error(methodName, exAgg, ResourceType.AffinityGroup.ToString(), affinityGroup);
                                }
                                throw;
                            }
                        });

                    dcMigrationManager.ReportProgress(ProgressResources.RollbackAffinityGroupsWaiting);
                    Logger.Info(methodName, ProgressResources.RollbackAffinityGroupsWaiting, ResourceType.AffinityGroup.ToString());
                    Task.Delay(Constants.DelayTimeInMilliseconds_Rollback).Wait();
                }

            }
            swTotalAffGrp.Stop();
            Logger.Info(methodName, string.Format(ProgressResources.ExecutionCompletedWithTime, swTotalAffGrp.Elapsed.Days, swTotalAffGrp.Elapsed.Hours,
                swTotalAffGrp.Elapsed.Minutes, swTotalAffGrp.Elapsed.Seconds), ResourceType.AffinityGroup.ToString());
        }


        #region Management API call
        /// <summary>
        /// Gets network configuration from MS azure using management API call.
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>
        /// <param name="serviceUrl">service url of subscription</param>
        /// <returns>Network configuration for subscription</returns>
        private NetworkGetConfigurationResponse GetNetworkConfigurationFromMSAzure(SubscriptionCloudCredentials credentials, Uri serviceUrl)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.ExecutionStarted, ResourceType.VirtualNetwork.ToString());

            Logger.Info(methodName, ProgressResources.GetVirtualNetworkConfigFromMSAzureStarted);
            using (var vnetClient = new NetworkManagementClient(credentials, serviceUrl))
            {
                try
                {
                    NetworkGetConfigurationResponse ventConfig = vnetClient.Networks.GetConfiguration();
                    Logger.Info(methodName, ProgressResources.GetVirtualNetworkConfigFromMSAzureCompleted, ResourceType.VirtualNetwork.ToString());
                    Logger.Info(methodName, ProgressResources.ExecutionCompleted, ResourceType.VirtualNetwork.ToString());

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
                        Logger.Error(methodName, cex);
                        throw cex;
                    }
                }
            }
        }
        #endregion
    }
}