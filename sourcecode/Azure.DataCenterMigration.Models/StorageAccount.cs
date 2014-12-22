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
using Microsoft.WindowsAzure.Management.Storage.Models;

namespace Azure.DataCenterMigration.Models
{
    /// <summary>
    /// Model class for Storage Account.
    /// </summary>
    public class StorageAccount
    {
        #region Properties

        /// <summary>
        /// Storage Account details associated with the subscription.
        /// </summary>
        public Microsoft.WindowsAzure.Management.Storage.Models.StorageAccount StorageAccountDetails { get; set; }

        /// <summary>
        /// Status of Storage Account import 
        /// </summary>
        public bool IsImported { get; set; }

        #endregion
    }
}
