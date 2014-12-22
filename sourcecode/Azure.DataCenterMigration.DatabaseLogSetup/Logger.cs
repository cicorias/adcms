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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Azure.DataCenterMigration.DatabaseLogSetup
{
    /// <summary>
    /// Logging context for database logging
    /// </summary>
    public class LoggingContext : DbContext
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        public LoggingContext()
            : base("DbConnection")
        {

        }

        /// <summary>
        /// Property for database logs.
        /// </summary>
        public DbSet<DataCenterLog> logs { get; set; }

    }

    /// <summary>
    /// Class to store Log values into database 
    /// </summary>
    public class DataCenterLog
    {
        /// <summary>
        /// Log Id
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Time and Date of log
        /// </summary>
        [Column(TypeName = "datetime2")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Log level as Info / Error
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// Current method name from where entry called 
        /// </summary>
        public string Logger { get; set; }

        /// <summary>
        /// Resource Type
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Resource Name
        /// </summary>
        public string ResourceName { get; set; }
        /// <summary>
        /// Log message
        /// </summary>
        public string Message { get; set; }

    }
  
}
