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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Azure.DataCenterMigration
{
    internal class ResourceNameMapper
    {
        /// <summary>
        /// Source Name of the resource
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Destination Name of the resource
        /// </summary>
        public string DestinationName { get; set; }
         

        /// <summary>
        /// Type of resource
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        ///  List of child resources
        /// </summary>
        private List<ResourceNameMapper> childResources = new List<ResourceNameMapper>();

        /// <summary>
        /// Child Resource
        /// </summary>
        public List<ResourceNameMapper> ChildResources
        {
            get { return childResources; }
            set { childResources = value; }
        }
    }

    /// <summary>
    /// Helper methods for ResourceNameMapper
    /// </summary>
    internal class ResourceNameMapperHelper
    {
        private Dictionary<ResourceType, List<ResourceNameMapper>> resourceNameCollection =
            new Dictionary<ResourceType, List<ResourceNameMapper>>();

        #region Internal Methods
        /// <summary>
        /// return xml string for the resource name mapping
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="destinationPrefixValue"></param>
        /// <returns></returns>
        internal string GenerateMapperXml(Subscription subscription, string destinationPrefixValue)
        {
            foreach (var datacenter in subscription.DataCenters)
            {
                foreach (var affinityGroup in datacenter.AffinityGroups)
                {
                    GenerateNewResourceName(ResourceType.AffinityGroup, affinityGroup.AffinityGroupDetails.Name, destinationPrefixValue);
                }
                foreach (var storageAccount in datacenter.StorageAccounts)
                {
                    GenerateNewResourceName(ResourceType.StorageAccount, storageAccount.StorageAccountDetails.Name, destinationPrefixValue);
                }

                foreach (var cloudService in datacenter.CloudServices)
                {
                    GenerateNewResourceName(ResourceType.CloudService, cloudService.CloudServiceDetails.ServiceName, destinationPrefixValue);

                    ResourceNameMapper resourceCloudService = resourceNameCollection[ResourceType.CloudService].Where
                        (s => s.SourceName.Equals(cloudService.CloudServiceDetails.ServiceName)).FirstOrDefault();

                    if (cloudService.DeploymentDetails != null)
                    {
                        if (resourceCloudService.ChildResources == null)
                        {
                            resourceCloudService.ChildResources = new List<ResourceNameMapper>();
                        }
                        ResourceNameMapper resourceDeployment = new ResourceNameMapper
                                {
                                    SourceName = cloudService.DeploymentDetails.Name,
                                    DestinationName = GenerateNewResourceName(ResourceType.Deployment, cloudService.DeploymentDetails.Name,
                                    destinationPrefixValue, false),
                                    //Import = true,
                                    ResourceType = ResourceType.Deployment.ToString()
                                };
                        foreach (var virtualMachine in cloudService.DeploymentDetails.VirtualMachines)
                        {
                            ResourceNameMapper resourceVirtaulMachine = new ResourceNameMapper
                                  {
                                      SourceName = virtualMachine.VirtualMachineDetails.RoleName,
                                      DestinationName = virtualMachine.VirtualMachineDetails.RoleName,
                                      // Import = true,
                                      ResourceType = ResourceType.VirtualMachine.ToString()
                                  };

                            resourceVirtaulMachine.ChildResources.Add(new ResourceNameMapper
                            {
                                SourceName = virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.Name,
                                DestinationName = string.Format("{0}{1}", destinationPrefixValue,
                                virtualMachine.VirtualMachineDetails.OSVirtualHardDisk.Name),
                                //Import = true,
                                ResourceType = ResourceType.OSDisk.ToString()
                            });
                            foreach (var disk in virtualMachine.VirtualMachineDetails.DataVirtualHardDisks)
                            {
                                resourceVirtaulMachine.ChildResources.Add(new ResourceNameMapper
                                {
                                    SourceName = disk.Name,
                                    DestinationName = string.Format("{0}{1}", destinationPrefixValue, disk.Name),
                                    // Import = true,
                                    ResourceType = ResourceType.HardDisk.ToString()
                                });
                            }
                            resourceDeployment.ChildResources.Add(resourceVirtaulMachine);
                        }
                        resourceCloudService.ChildResources.Add(
                              resourceDeployment);
                    }
                }
                #region Network Configuration
                if (datacenter.NetworkConfiguration != null && datacenter.NetworkConfiguration.VirtualNetworkConfiguration != null)
                {
                    if (datacenter.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites != null)
                    {
                        foreach (var virtualNetworkSite in datacenter.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites)
                        {
                            GenerateNewResourceName(ResourceType.VirtualNetworkSite,
                                virtualNetworkSite.name, destinationPrefixValue);

                            ResourceNameMapper resourceVirtualNetworkSite = resourceNameCollection[ResourceType.VirtualNetworkSite].Where
                       (s => s.SourceName.Equals(virtualNetworkSite.name)).FirstOrDefault();

                            if (resourceVirtualNetworkSite.ChildResources == null)
                            {
                                resourceVirtualNetworkSite.ChildResources = new List<ResourceNameMapper>();
                            }

                            if (virtualNetworkSite.DnsServersRef != null)
                            {
                                foreach (var dns in virtualNetworkSite.DnsServersRef)
                                {
                                    ResourceNameMapper resourceDNS = new ResourceNameMapper
                                    {
                                        SourceName = dns.name,
                                        DestinationName = GenerateNewResourceName(ResourceType.DnsServer, dns.name,
                                        destinationPrefixValue, false),
                                        //Import = true,
                                        ResourceType = ResourceType.DnsServer.ToString()
                                    };
                                    resourceVirtualNetworkSite.ChildResources.Add(resourceDNS);
                                }
                            }
                            if (virtualNetworkSite.Gateway != null && virtualNetworkSite.Gateway.ConnectionsToLocalNetwork != null &&
                                virtualNetworkSite.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef != null)
                            {
                                ResourceNameMapper resourceLocalNetwork = new ResourceNameMapper
                                {
                                    SourceName = virtualNetworkSite.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef.name,
                                    DestinationName = GenerateNewResourceName(ResourceType.LocalNetworkSite,
                                    virtualNetworkSite.Gateway.ConnectionsToLocalNetwork.LocalNetworkSiteRef.name,
                                    destinationPrefixValue, false),
                                    //Import = true,
                                    ResourceType = ResourceType.LocalNetworkSite.ToString()
                                };
                                resourceVirtualNetworkSite.ChildResources.Add(resourceLocalNetwork);
                            }
                        }
                    }
                }
                #endregion
            }
            var xmlRoot = new XElement("Resources", new XAttribute(Constants.Parameters.DestinationPrefixName,
                destinationPrefixValue));
            foreach (var item in resourceNameCollection)
            {
                XElement child = new XElement(item.Key + "s");
                foreach (var resourceMapper in item.Value)
                {
                    GetAllChildResources(resourceMapper, child);
                }
                xmlRoot.Add(child);
            }
            return (new XDocument(xmlRoot)).ToString();
        }

        /// <summary>
        /// Get child resource source or destination name as per isDestinationNameRequired true or false
        /// if false, resource source name is returned
        /// else get destination name
        /// </summary>
        /// <param name="root"></param>
        /// <param name="childResourceType"></param>
        /// <param name="resourceName"></param>
        /// <param name="isDestinationNameRequired"></param>
        /// <returns></returns>
        internal string GetChildResourceName(ResourceNameMapper root, ResourceType childResourceType, string resourceName,
            bool isDestinationNameRequired = true)
        {
            string name = string.Empty; ;

            foreach (ResourceNameMapper res in root.ChildResources)
            {
                if (isDestinationNameRequired && res.SourceName.Equals(resourceName) && res.ResourceType.Equals(childResourceType.ToString()))
                {
                    name = res.DestinationName;
                    break;
                }
                else if (!isDestinationNameRequired && res.DestinationName.Equals(resourceName) && res.ResourceType.Equals(childResourceType.ToString()))
                {
                    name = res.SourceName;
                    break;
                }
                else if (res.ChildResources.Count() > 0)
                {
                    for (int i = 0; i < res.ChildResources.Count(); i++)
                    {
                        name = GetChildResourceName(res, childResourceType, resourceName, isDestinationNameRequired);
                        if (name != null && !string.IsNullOrEmpty(name))
                        {
                            return name;
                        }
                    }
                }
            }
            return name;
        }

        /// <summary>
        /// Fill the dictinary from xml file content
        /// </summary>
        /// <param name="xmlPath"></param>
        /// <param name="destinationPrefixValue"></param>
        /// <returns></returns>
        internal Dictionary<ResourceType, List<ResourceNameMapper>> GetDestinationResourceNames(string xmlPath,
            out string destinationPrefixValue)
        {
            XElement document = XElement.Load(xmlPath);
            IEnumerable<XElement> xmlElements = document.Elements();

            destinationPrefixValue = document.Attribute(Constants.Parameters.DestinationPrefixName).Value;

            foreach (var element in xmlElements)
            {
                IEnumerable<XElement> innerElements = element.Elements();

                var resourceType = Enum.Parse(typeof(ResourceType), innerElements.FirstOrDefault().Name.ToString());
                List<ResourceNameMapper> lstResourceNameMapper = new List<ResourceNameMapper>();
                foreach (var innerElement in innerElements)
                {
                    lstResourceNameMapper.Add(GetResourcesFromXml(innerElement));
                }

                resourceNameCollection.Add((ResourceType)resourceType, lstResourceNameMapper);
            }
            return resourceNameCollection;
        }
        #endregion

        #region Private Methods
        private void GetAllChildResources(ResourceNameMapper resource, XElement parent)
        {
            XElement childElement = new XElement(resource.ResourceType,
                   new XAttribute(Constants.ResourceNameMapper.SourceName, resource.SourceName),
               new XAttribute(Constants.ResourceNameMapper.DestinationName, resource.DestinationName)
                //,new XAttribute(Constants.ResourceNameMapper.Import, resource.Import)
                    );
            parent.Add(childElement);
            foreach (ResourceNameMapper child in resource.ChildResources)
            {
                GetAllChildResources(child, childElement);
            };
        }

        private ResourceNameMapper GetResourcesFromXml(XElement element)
        {
            ResourceNameMapper resourceNameMapper = new ResourceNameMapper()
            {
                SourceName = element.Attribute(Constants.ResourceNameMapper.SourceName).Value,
                DestinationName = element.Attribute(Constants.ResourceNameMapper.DestinationName).Value,
                //Import = Boolean.Parse(element.Attribute(Constants.ResourceNameMapper.Import).Value),
                ResourceType = element.Name.ToString()
            };
            foreach (var innerElement in element.Elements())
            {
                resourceNameMapper.ChildResources.Add(GetResourcesFromXml(innerElement));
            }
            return resourceNameMapper;
        }

        /// <summary>
        /// Generates new name for destination resources by attaching Prefix to source  resources.
        /// </summary>        
        /// <param name="resourceType">Resource type</param>
        /// <param name="originalResourceName">Source subscription resource name</param>
        /// <param name="destinationPrefixValue">Prefix value for destination name</param>
        /// <param name="updateCollection">True to update the internal collection of resource names</param>
        /// <returns>Prefix attached value</returns>
        private string GenerateNewResourceName(ResourceType resourceType, string originalResourceName,
            string destinationPrefixValue, bool updateCollection = true)
        {
            if (originalResourceName == null)
            {
                return null;
            }
            else
            {
                string newResourceName = string.Format("{0}{1}", destinationPrefixValue, originalResourceName);

                if (resourceType != ResourceType.None)
                {
                    newResourceName = ((Constants.GetMaxLengthForResourceType(resourceType) != -1 &&  (newResourceName.Length > Constants.GetMaxLengthForResourceType(resourceType))?
                        newResourceName.Substring(0,  Constants.GetMaxLengthForResourceType(resourceType) - 1) :
                        newResourceName));

                    if (updateCollection)
                    {
                        if (resourceNameCollection.ContainsKey(resourceType) && resourceNameCollection[resourceType].Count() > 0)
                        {
                            if (resourceNameCollection[resourceType].Where(s => s.SourceName.Equals(originalResourceName)).Count() == 0)
                                resourceNameCollection[resourceType].Add(new ResourceNameMapper
                                {
                                    SourceName = originalResourceName,
                                    DestinationName = newResourceName,
                                    //Import = true,
                                    ResourceType = resourceType.ToString()
                                });
                        }
                        else
                        {
                            List<ResourceNameMapper> resourceNames = new List<ResourceNameMapper>();
                            resourceNames.Add(new ResourceNameMapper
                            {
                                SourceName = originalResourceName,
                                DestinationName = newResourceName,
                                //Import = true,
                                ResourceType = resourceType.ToString()
                            });
                            resourceNameCollection.Add(resourceType, resourceNames);
                        }
                    }
                }
                return newResourceName;
            }
        }
        #endregion
    }
}
