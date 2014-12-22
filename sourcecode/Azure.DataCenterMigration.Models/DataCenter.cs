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
using System.Collections.Generic;

namespace Azure.DataCenterMigration.Models
{
    /// <summary>
    /// Model class for logical grouping of resources at DataCenter level.
    /// </summary>
    public class DataCenter
    {
        #region Private Members

        private List<AffinityGroup> affinityGroups = new List<AffinityGroup>();
        private List<CloudService> cloudServices = new List<CloudService>();
        private List<StorageAccount> storageAccounts = new List<StorageAccount>();
        #endregion

        #region Properties        
        
        /// <summary>
        /// Location of the Datacenter
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// Status of Datacenter import 
        /// </summary>
        public bool IsImported { get; set; }


        /// <summary>
        /// Read-Write property for AffinityGroup associated with Datacenter
        /// </summary>
        public List<AffinityGroup> AffinityGroups
        {
            get
            {
                return affinityGroups ?? (affinityGroups = new List<AffinityGroup>());
            }
            set
            {
                affinityGroups = value;
            }
        }

        /// <summary>
        /// Read-Write property for StorageAccount associated with Datacenter
        /// </summary>
        public List<StorageAccount> StorageAccounts
        {
            get
            {
                return storageAccounts ?? (storageAccounts = new List<StorageAccount>());
            }
            set
            {
                storageAccounts = value;
            }
        }

        /// <summary>
        /// Read-Write property for Subscription NetworkConfiguration
        /// </summary>
        public NetworkConfiguration NetworkConfiguration { get; set; }

        /// <summary>
        /// Read-Write property for CloudServices associated with Datacenter
        /// </summary>
        public List<CloudService> CloudServices
        {
            get
            {
                return cloudServices ?? (cloudServices = new List<CloudService>());
            }
            set
            {
                cloudServices = value;
            }
        }
      
        #endregion

    }
}
