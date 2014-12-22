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

namespace Azure.DataCenterMigration.Models
{
    /// <summary>
    /// Model class for Cloud Service.
    /// </summary>
    public class CloudService
    {
        #region Properties

        /// <summary>
        /// Details of Cloud Service
        /// </summary>
        public HostedServiceListResponse.HostedService CloudServiceDetails { get; set; }
       
        /// <summary>
        /// Deployment in the Cloud Service
        /// </summary>
        public Deployment DeploymentDetails { get; set; }

        /// <summary>
        /// Status of Cloud Service import 
        /// </summary>
        public bool IsImported { get; set; }

        #endregion

    }
}