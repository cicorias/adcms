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
using log4net;
using System;

namespace Azure.DataCenterMigration
{
    /// <summary>
    /// Class to set logging properties.
    /// </summary>
    public static class Logger
    {
        private static Object thisLockLogger = new Object();
        static Logger()
        {
            LogSetup();
        }
        /// <summary>
        /// Sets properties which can be used in log configuration
        /// </summary>
        public static void LogSetup()
        {            
            log4net.Config.XmlConfigurator.Configure();
        }
       
        /// <summary>
        /// Sets value for ResourceType and ResourceName properties in lof file
        /// </summary>
        /// <param name="log">Logger object</param>
        /// <param name="resourceType">Type of resource</param>
        /// <param name="resourceName">Name of resource</param>
        private static void SetLog(ILog log, string resourceType = null, string resourceName = null)
        {
            log4net.LogicalThreadContext.Properties[Constants.ResourceType] = string.IsNullOrEmpty(resourceType) ?
                Constants.StringHyphen : resourceType;

            log4net.LogicalThreadContext.Properties[Constants.ResourceName] = string.IsNullOrEmpty(resourceName) ?
              Constants.StringHyphen : resourceName;
        }

        /// <summary>
        /// Logs information in log file
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="message">Message</param>
        /// <param name="resourceType">Type of resource</param>
        /// <param name="resourceName">Name of resource</param>
        public static void Info(String methodName, string message, string resourceType = null, string resourceName = null)
        {
            lock (thisLockLogger)
            {
                ILog log = log4net.LogManager.GetLogger(methodName);
                SetLog(log, resourceType, resourceName);
                log.Info(message);
            }
        }

        /// <summary>
        /// Logs error in log file
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="message">Message</param>
        /// <param name="resourceType">Type of resource</param>
        /// <param name="resourceName">Name of resource</param>
        public static void Error(String methodName, string message, string resourceType = null, string resourceName = null)
        {
            lock (thisLockLogger)
            {
                ILog log = log4net.LogManager.GetLogger(methodName);
                SetLog(log, resourceType, resourceName);
                log.Error(message);
            }
        }

        /// <summary>
        /// Logs exception in log file
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="ex">Exception</param>
        /// <param name="resourceType">Type of resource</param>
        /// <param name="resourceName">Name of resource</param>
        public static void Error(String methodName, Exception ex, string resourceType = null, string resourceName = null)
        {
            lock (thisLockLogger)
            {
                ILog log = log4net.LogManager.GetLogger(methodName);
                SetLog(log, resourceType, resourceName);
                log.Error(string.Format(StringResources.ExceptionOccurred, ex.GetType().ToString(), ex.Message, ex.StackTrace));
            }
        }

        /// <summary>
        /// Logs Warnings with exceptions in log file
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="message">Waring message to be logged</param>
        /// <param name="ex">Exception</param>
        /// <param name="resourceType">Type of resource</param>
        /// <param name="resourceName">Name of resource</param>
        public static void Warning(String methodName, string message, Exception ex = null, string resourceType = null, string resourceName = null)
        {
            lock (thisLockLogger)
            {
                ILog log = log4net.LogManager.GetLogger(methodName);
                SetLog(log, resourceType, resourceName);
                if (ex != null)
                {
                    log.Warn(string.Format(StringResources.ExceptionOccurred, ex.GetType().ToString(), message + ": " + ex.Message, ex.StackTrace));
                }
                else
                {
                    log.Warn(message);
                }
            }
        }      
    }
}
