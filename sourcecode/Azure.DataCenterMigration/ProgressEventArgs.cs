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
    /// Class to represent progress event arguments.
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        #region Private Members

        private string message;
        
        #endregion

        /// <summary>
        /// Constructor with message parameter
        /// </summary>
        /// <param name="msg">Message to display on console</param>
        public ProgressEventArgs(string msg)
        {
            message = msg;
        }

        /// <summary>
        /// Gets the message for the event.
        /// </summary>
        public string Message
        {
            get { return message; }
        }

        /// <summary>
        /// Gets the date and time when the event occurred.
        /// </summary>
        public DateTime EventDateTime
        {
            get { return DateTime.Now; }
        }
    }
}
