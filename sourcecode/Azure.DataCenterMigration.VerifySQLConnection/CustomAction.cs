using Microsoft.Deployment.WindowsInstaller;
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
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Azure.DataCenterMigration.VerifySQLConnection
{
    /// <summary>
    /// WIX Custom Action class used for creating Custom Actions to be invoked by the installer.
    /// </summary>
    public class CustomActions
    {
        /// <summary>
        /// VerifySqlConnection Method tests the connection to the SQL server before proceeding with the installtion if SQL database logging is required
        /// </summary>
        /// <param name="session"> It opens the install database that contains installation tables and data </param>
        /// <returns>Status Value of custom action </returns>
        [CustomAction]
        public static ActionResult VerifySqlConnection(Session session)
        {
            try
            {
                //Constructs the SQL Server Connectionstring 
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = session[StringConstants.ServerName],
                    ConnectTimeout = 5
                };

                if (session[StringConstants.Authentication] == "0")
                {
                    builder.IntegratedSecurity = true;
                }
                else
                {
                    builder.UserID = session[StringConstants.UserName];
                    builder.Password = session[StringConstants.Password];
                }

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    //Invokes Checkconnection method for testing the connectivity to SQL server based on the ConnectionString
                    if (CheckConnection(connection, session))
                    {
                        session[StringConstants.Odbc_Connection] = "1";
                        MessageBox.Show(StringConstants.SuccessfulConnection, StringConstants.MessageHeader);
                    }
                    else
                    {
                        session[StringConstants.Odbc_Connection] = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
                throw;
            }

            return ActionResult.Success;
        }

        /// <summary>
        /// CheckConnection method tries to open connection to the SQL server based on the ServerName and the Credentials provided
        /// </summary>
        /// <param name="connection"> ConnectionString to be used to connect to the SQL Server</param>
        /// <param name="session"> It opens the install database that contains installation tables and data</param>
        /// <returns>Status Value of custom action </returns>
        public static bool CheckConnection(SqlConnection connection, Session session)
        {
            try
            {
                if (connection == null)
                {
                    return false;
                }
                //Opens connection to the SQL Server
                connection.Open();

                var canOpen = Convert.ToBoolean(connection.State);

                connection.Close();
                session[StringConstants.Odbc_Error] = "0";
                return canOpen;
            }
            catch (SqlException ex)
            {
                session[StringConstants.Odbc_Error] = ex.Message;

                MessageBox.Show(StringConstants.UnsuccessfulConnection, StringConstants.MessageHeader);
                session[StringConstants.Odbc_Error] = "1";
                return false;
            }
        }
    }
}
