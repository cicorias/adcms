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
using Microsoft.WindowsAzure.Management.Compute.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.DataCenterMigration.Models
{
    /// <summary>
    /// Model class for cloud service deployment.
    /// </summary>
    public class Deployment
    {
        #region Private Members
              
        private List<VirtualMachine> virtualMachines = new List<VirtualMachine>();

        #endregion

        #region Properties
        /// <summary>
        /// DNS settings that are specified for deployment
        /// </summary>
        public DnsSettings DnsSettings { get; set; }

        /// <summary>
        /// Extended properties related to deployment
        /// </summary>
        public IDictionary<string, string> ExtendedProperties { get; set; }

        /// <summary>
        /// Label for deployment
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// List of Load Balancers
        /// </summary>
        public IList<LoadBalancer> LoadBalancers { get; set; }

        /// <summary>
        /// Name of the deployment
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Reserved IP for the deplyment 
        /// </summary>
        public string ReservedIPName { get; set; }

        /// <summary>
        /// Virtual IP addresses related to deployment
        /// </summary>
        public IList<VirtualIPAddress> VirtualIPAddresses { get; set; }

        /// <summary>
        /// Virtual network name if deployment is in virtual network
        /// </summary>
        public string VirtualNetworkName { get; set; }
        
        /// <summary>
        /// List of virtual machines associated with deployments
        /// </summary>
        public List<VirtualMachine> VirtualMachines
        {
            get
            {
                return virtualMachines ?? (virtualMachines = new List<VirtualMachine>());
            }
            set
            {
                virtualMachines = value;
            }
        }

        /// <summary>
        /// Status of Deployment import 
        /// </summary>
        public bool IsImported { get; set; }
        #endregion



    }
}
