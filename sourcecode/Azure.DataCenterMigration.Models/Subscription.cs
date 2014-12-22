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
    /// Model class for Subscription.
    /// </summary>
    public class Subscription
    {
        #region Private Members

        private List<DataCenter> dataCenters = new List<DataCenter>();

        #endregion

        #region Properties       

        /// <summary>
        /// Read-Write property for Subscription Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Read-Write property for DataCenters associated with subscription
        /// </summary>
        public List<DataCenter> DataCenters
        {
            get
            {
                return dataCenters ?? (dataCenters = new List<DataCenter>());
            }
            set
            {
                dataCenters = value;
            }
        }       

        #endregion
    }
}
