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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Static class for retry operation on failure
    /// </summary>
    internal static class Retry
    {
        private static DCMigrationManager dcMigration = new DCMigrationManager();
        
        /// <summary>
        /// Generic method for retry
        /// </summary>
        /// <param name="action">Action to be retry</param>        
        /// <param name="baseParams">object of <see cref="BaseParameters"></see> class/></param>        
        /// <param name="resourceType">Resource Type</param>
        /// <param name="resourceName">Resource Name</param>
        /// <param name="preRetryAction">Function/Action to get ran before main action executes</param>
        /// <param name="ignoreResourceNotFoundEx">True to ignore if Resource Not Found exception occurred</param>
        /// <returns>Function output on successful operation. Retruns AggregateException if any exception occures</returns>
        public static T RetryOperation<T>(Func<T> action, BaseParameters baseParams, ResourceType resourceType, string resourceName = null, Action preRetryAction = null,bool ignoreResourceNotFoundEx =false)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var exceptions = new List<Exception>();
            for (int currentRetryCount = 0; currentRetryCount < baseParams.RetryCount; currentRetryCount++)
            {
                try
                {
                    if (preRetryAction != null && currentRetryCount > 0)
                    {
                        preRetryAction();
                    }
                    return action();
                }
                catch (Exception ex)
                {
                    if (ignoreResourceNotFoundEx && (ex.GetType() == typeof(CloudException)))
                    {    
                        //Return if error code is Resource Not Found
                        if (string.Compare(((CloudException)ex).ErrorCode, Constants.ResourceNotFound, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {                            
                            return default(T);
                        }
                    }
                    TimeSpan retryInterval = TimeSpan.Zero;                  
                    Logger.Warning(methodName, string.Format(ProgressResources.RetryWait, currentRetryCount), ex, resourceType.ToString(), resourceName);
                    Random r = new Random();

                    // Calculate Exponential backoff with +/- 20% tolerance
                    int increment = (int)((Math.Pow(2, currentRetryCount) - 1) * r.Next((int)(baseParams.DeltaBackOff.TotalMilliseconds * 0.8),
                        (int)(baseParams.DeltaBackOff.TotalMilliseconds * 1.2)));

                    // Enforce backoff boundaries
                    int timeToSleepMsec = (int)Math.Min(baseParams.MinBackOff.TotalMilliseconds + increment, baseParams.MaxBackOff.TotalMilliseconds);
                    retryInterval = TimeSpan.FromMilliseconds(timeToSleepMsec);
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
            }
            throw new AggregateException(exceptions);
        }
        /// <summary>
        /// Creates Request option for Blob using Exponential retry policy
        /// </summary>
        /// <param name="deltaBackOff">Delta Backoff in seconds</param>
        /// <param name="retryCount">No. of times to retry in case of exception</param>
        /// <returns>BlobRequestOption</returns>
        internal static BlobRequestOptions GetBlobRequestOptions(TimeSpan deltaBackOff, int retryCount)
        {
            IRetryPolicy exponentialRetryPolicy = new ExponentialRetry(deltaBackOff, retryCount);
            BlobRequestOptions requestOptions = new BlobRequestOptions() { RetryPolicy = exponentialRetryPolicy };
            return requestOptions;
        }

        /// <summary>        
        /// Gets list of affinity group operation response from MS azure using API call.        
        /// </summary>
        /// <param name="credentials">Subscription Cloud Credentials</param>        
        /// <param name="importParameters">object of <see cref="ImportParameters"/> which contains the input parameters passed for import functionality </param>
        /// <returns>List of affinity group operation response for subscription </returns>
        private static AffinityGroupListResponse GetAffinityGroupListResponseFromMSAzure(SubscriptionCloudCredentials credentials, ImportParameters importParameters)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName, ProgressResources.GetAffinityGroupsFromMSAzureStarted, ResourceType.AffinityGroup.ToString());
            using (var client = new ManagementClient(credentials))
            {
                // Call management API to get list of affinity groups.
                AffinityGroupListResponse agResponse = Retry.RetryOperation(() => client.AffinityGroups.List(),
                  (BaseParameters)importParameters,
                    ResourceType.AffinityGroup);
                Logger.Info(methodName, ProgressResources.GetAffinityGroupsFromMSAzureCompleted, ResourceType.AffinityGroup.ToString());
                return agResponse;
            }
        }
    }

}
