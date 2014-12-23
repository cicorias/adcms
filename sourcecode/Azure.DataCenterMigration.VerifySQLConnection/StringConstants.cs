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

namespace Azure.DataCenterMigration.VerifySQLConnection
{/// <summary>
    /// This class contains the variables to store the constant values for CustomAction.cs
    /// </summary>
    internal class StringConstants
    {
        /// <summary>
        /// Constant to store WIX property SERVERNAME.
        /// </summary>
        internal const string ServerName = "SERVERNAME";
        /// <summary>
        /// Constant to store WIX property USERNAME.
        /// </summary>
        internal const string UserName = "DBUSERNAME";
        /// <summary>
        /// Constant to store WIX property PASSWORD.
        /// </summary>
        internal const string Password = "PASSWORD";
        /// <summary>
        /// Constant to store WIX property ODBC_ERROR.
        /// </summary>
        internal const string Odbc_Error = "ODBC_ERROR";
        /// <summary>
        /// Constant to store WIX property ODBC_CONNECTION_ESTABLISHED.
        /// </summary>
        internal const string Odbc_Connection = "ODBC_CONNECTION_ESTABLISHED";
        /// <summary>
        /// Constant to store WIX property DBAUTH.
        /// </summary>
        internal const string Authentication = "DBAUTH";
        /// <summary>
        /// Constant to store the user message for Connection succeeded.
        /// </summary>
        internal const string SuccessfulConnection = "Connection succeeded.";
        /// <summary>
        /// Constant to store the user message for Connection to the SQL Database could not be established. Please verify the name of the instance and the credentials provided.
        /// </summary>
        internal const string UnsuccessfulConnection = "Connection to the SQL Database could not be established. Please verify the name of the instance and the credentials provided.";
        /// <summary>
        /// Constant to store value for the message box header as Connect To SQL Database.
        /// </summary>
        internal const string MessageHeader = "Connect To SQL Database";
    }
}