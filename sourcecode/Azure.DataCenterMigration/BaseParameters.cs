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

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Class to store common paramters value for Export/Import functionality.
    /// </summary>
    internal class BaseParameters
    {
        /// <summary>
        /// Minimum backoff for exponential retry strategy
        /// </summary>
        public TimeSpan MinBackOff { get; set; }

        /// <summary>
        /// Maximum backoff for exponential retry strategy
        /// </summary>
        public TimeSpan MaxBackOff { get; set; }

        /// <summary>
        /// Delta backoff for exponential retry strategy
        /// </summary>
        public TimeSpan DeltaBackOff { get; set; }

        /// <summary>
        /// Number of retries if exception occurs.
        /// </summary>
        public int RetryCount { get; set; }
    }
}
