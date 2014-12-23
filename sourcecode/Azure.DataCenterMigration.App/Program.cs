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
using Azure.DataCenterMigration;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Azure.DataCenterMigration.App
{
    /// <summary>
    /// Main program for Azure.DataCenterMigration.App console application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main Class
        /// </summary>
        /// <param name="args"> Argument parameters :
        /// Example1 : -Operation "Export" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -ExportMetadataFolderPath "D:\\DataCenterMigration" -SourcePublishSettingsFilePath  "D:\\PublishSettings.PublishSettings" -SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -QuietMode "True" -GenerateMapperXml "True" -RetryCount "5" -MinBackoff "3" -MaxBackoff "3" -DeltaBackoff "90"
        /// Example2 : -Operation "Import" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings"  -SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -MapperXmlFilePath "D:\\DataCenterMigration\mydata.xml" -DestinationPrefixName "dc" -RetryCount "5" -MinBackoff "3" -MaxBackoff "3" -DeltaBackoff "90" -QuietMode "True" -RollBackOnFailure "True" -ResumeImport "True"
        /// Example3 : -Operation "Migrate" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings"  -SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -ExportMetadataFolderPath "D:\\DataCenterMigration" -DestinationPrefixName "dc" -RetryCount "5" -MinBackoff "3" -MaxBackoff "3" -DeltaBackoff "90" -QuietMode "True" -RollBackOnFailure "True"
        /// </param>
        static void Main(string[] args)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            DCMigrationManager dcMigration = new DCMigrationManager();
            bool isQuietMode = false;
            try
            {
                Dictionary<string, string> parameters = null;
                // Read input command line parameters.                
                if (args.Length > 0)
                {
                    if (!TryParseCommandLineArguments(args, out parameters))
                    {
                        ExitApplication(isQuietMode);
                        return;
                    }
                }
                else
                {
                    parameters = new Dictionary<string, string>();
                    // If input parameters are not specified, read config file.
                    foreach (string strkey in ConfigurationManager.AppSettings)
                    {
                        if (ConfigurationManager.AppSettings[strkey] != "")
                        {
                            parameters.Add(strkey, ConfigurationManager.AppSettings[strkey]);
                        }
                    }
                    if (parameters.Count == 0)
                    {
                        Console.WriteLine(StringResources.MissingInputParameters);
                        ExitApplication(isQuietMode);
                        return;
                    }
                }

                // Add progress handler to print progress to console.
                dcMigration.Progress += DCMigrationManager_Progress;

                if (string.Compare(parameters[Constants.Parameters.Operation], Constants.Operations.Export, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    // Export subscription Metadata.
                    dcMigration.ExportSubscriptionMetadata(parameters);
                }
                else
                {
                    Logger.Info(methodName, StringResources.ImportWarningLog);
                    if (!parameters.Keys.Contains(Constants.Parameters.QuietMode))
                    {
                        isQuietMode = false;
                    }
                    else
                    {
                        bool.TryParse(parameters[Constants.Parameters.QuietMode], out isQuietMode);
                    }
                    if (isQuietMode || ConfirmContinue())
                    {
                        if (string.Compare(parameters[Constants.Parameters.Operation], Constants.Operations.Import, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            // Import subscription Metadata.
                            dcMigration.ImportSubscriptionMetadata(parameters);
                        }
                        else if (string.Compare(parameters[Constants.Parameters.Operation], Constants.Operations.Migrate, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            // Migrate subscription Metadata.
                            dcMigration.MigrateSubscription(parameters);
                        }
                    }
                }
                ExitApplication(isQuietMode);
                return;
            }
            catch (ValidationException vex)
            {
                Logger.Error(methodName, string.Format(StringResources.ValidationError, vex.Message));
                Console.WriteLine(StringResources.ValidationError, vex.Message);
                ShowCommonHelp(isQuietMode);
                ExitApplication(isQuietMode);
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(methodName, ex);
                Console.WriteLine(StringResources.ExceptionOccurred, ex.GetType().ToString(), ex.Message);
                ExitApplication(isQuietMode);
                return;
            }
        }

        private static bool ConfirmContinue()
        {
            Console.Write(StringResources.ImportWarning);
            char continueInput = Console.ReadKey(false).KeyChar;
            while (true)
            {
                Console.WriteLine();
                switch (char.ToLower(continueInput))
                {
                    case AppConstants.Yes:
                        return true;
                    case AppConstants.No:
                        return false;
                    default:
                        Console.Write(StringResources.SelectValidOption);
                        continueInput = Console.ReadKey(false).KeyChar;
                        break;
                }
            }
        }

        /// <summary>
        /// Show help to the user for Export input arguments
        /// </summary>
        static private void ShowExportHelp()
        {
            Console.WriteLine(StringResources.HelpDash);
            Console.WriteLine(StringResources.HelpForExportFunctionality);
            Console.WriteLine(StringResources.HelpDash);
            Console.WriteLine(StringResources.HelpSourceSubscriptionId);
            Console.WriteLine(StringResources.HelpSourceDCName);
            Console.WriteLine(StringResources.HelpSourcePublishSettingsFilePath);
            Console.WriteLine(StringResources.HelpSourceCertificateThumbprint);
            Console.WriteLine(StringResources.HelpExportMetadataFolderPath);
            Console.WriteLine(StringResources.HelpQuietMode);
            Console.WriteLine(StringResources.HelpGenerateMapperXml);
            Console.WriteLine(StringResources.HelpRetryCount);
            Console.WriteLine(StringResources.HelpMinBackoff);
            Console.WriteLine(StringResources.HelpMaxBackoff);
            Console.WriteLine(StringResources.HelpDeltaBackOff);
            Console.WriteLine(StringResources.HelpExportExample);
            Console.WriteLine(StringResources.HelpDash);
        }

        /// <summary>
        /// Show help to the user for Import input arguments
        /// </summary>
        static private void ShowImportHelp()
        {
            Console.WriteLine(StringResources.HelpDash);
            Console.WriteLine(StringResources.HelpForImportFunctionality);
            Console.WriteLine(StringResources.HelpDash);
            Console.WriteLine(StringResources.HelpSourceSubscriptionId);
            Console.WriteLine(StringResources.HelpDestinationSubscriptionId);
            Console.WriteLine(StringResources.HelpSourcePublishSettingsFilePath);
            Console.WriteLine(StringResources.HelpSourceCertificateThumbprint);
            Console.WriteLine(StringResources.HelpDestinationPublishSettingsFilePath);
            Console.WriteLine(StringResources.HelpDestinationCertificateThumbprint);
            Console.WriteLine(StringResources.HelpDestinationDCName);
            Console.WriteLine(StringResources.HelpImportMetadataFilePath);
            Console.WriteLine(StringResources.HelpMapperXmlFilePath);
            Console.WriteLine(StringResources.HelpImportDestinationPrefixName);
            Console.WriteLine(StringResources.HelpRetryCount);
            Console.WriteLine(StringResources.HelpMinBackoff);
            Console.WriteLine(StringResources.HelpMaxBackoff);
            Console.WriteLine(StringResources.HelpDeltaBackOff);
            Console.WriteLine(StringResources.HelpQuietMode);
            Console.WriteLine(StringResources.HelpResumeImport);
            Console.WriteLine(StringResources.HelpRollbackOnFailure);
            Console.WriteLine(StringResources.HelpImportExample);
            Console.WriteLine(StringResources.HelpDash);
        }

        /// <summary>
        /// Show help to the user for Migrate input arguments
        /// </summary>
        static private void ShowMigrateHelp()
        {
            Console.WriteLine(StringResources.HelpDash);
            Console.WriteLine(StringResources.HelpForMigrateFunctionality);
            Console.WriteLine(StringResources.HelpDash);
            Console.WriteLine(StringResources.HelpSourceSubscriptionId);
            Console.WriteLine(StringResources.HelpDestinationSubscriptionId);
            Console.WriteLine(StringResources.HelpSourceDCName);
            Console.WriteLine(StringResources.HelpDestinationDCName);
            Console.WriteLine(StringResources.HelpSourcePublishSettingsFilePath);
            Console.WriteLine(StringResources.HelpSourceCertificateThumbprint);
            Console.WriteLine(StringResources.HelpDestinationPublishSettingsFilePath);
            Console.WriteLine(StringResources.HelpDestinationCertificateThumbprint);
            Console.WriteLine(StringResources.HelpExportMetadataFolderPath);
            Console.WriteLine(StringResources.HelpRetryCount);
            Console.WriteLine(StringResources.HelpMinBackoff);
            Console.WriteLine(StringResources.HelpMaxBackoff);
            Console.WriteLine(StringResources.HelpDeltaBackOff);
            Console.WriteLine(StringResources.HelpQuietMode);
            Console.WriteLine(StringResources.HelpRollbackOnFailure);
            Console.WriteLine(StringResources.HelpMigrateExample);
            Console.WriteLine(StringResources.HelpDash);
        }

        /// <summary>
        /// Show help to the user for all options
        /// </summary>
        static private void ShowCommonHelp(bool isQuietMode)
        {
            if (!isQuietMode)
            {
                ShowExportHelp();
                Console.WriteLine("\n");
                ShowImportHelp();
                Console.WriteLine("\n");
                ShowMigrateHelp();
            }
        }

        /// <summary>
        /// Exits an application.
        /// </summary>
        static private void ExitApplication(bool isQuietMode)
        {
            if (!isQuietMode)
            {
                Console.WriteLine(StringResources.ExitApplicationMessage);
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Parse the input arguments and store it in dictionary object .
        /// </summary>
        /// <param name="args">List of commandline arguments </param>
        /// <param name="parameters">Dictionary for parameters where key is parameter name and value is parameter value</param>
        /// <returns>Input arguments structured in key value pairs</returns>
        static private bool TryParseCommandLineArguments(string[] args, out Dictionary<string, string> parameters)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            List<string> requiredParameters = null;
            parameters = null;
            // If arguments count is odd number.
            if (!(args.Count() % 2 == 0))
            {
                if (args.Count() == 1 && string.Compare(args[0].Substring(1, args[0].Length - 1), AppConstants.Help, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    ShowCommonHelp(true);
                    return false;
                }
                Logger.Error(methodName, StringResources.InvalidNumberOfArguments);
                Console.WriteLine(StringResources.InvalidNumberOfArguments);
                ShowCommonHelp(true);
                return false;
            }

            parameters = new Dictionary<string, string>();

            if (string.Compare(args[0].Substring(1, args[0].Length - 1), Constants.Parameters.Operation, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                Logger.Error(methodName, StringResources.MissingOperationParameter);
                Console.WriteLine(StringResources.MissingOperationParameter);
                return false;
            }

            if (string.Compare(args[1], Constants.Operations.Export, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                parameters.Add(args[0].Substring(1, args[0].Length - 1), args[1]);
                requiredParameters = new List<string>() { Constants.Parameters.SourcePublishSettingsFilePath,
                                                        Constants.Parameters.SourceCertificateThumbprint,
                                                        Constants.Parameters.SourceDCName, 
                                                        Constants.Parameters.SourceSubscriptionID, 
                                                        Constants.Parameters.ExportMetadataFolderPath,
                                                        Constants.Parameters.QuietMode,                                                        
                                                        Constants.Parameters.RetryCount,
                                                        Constants.Parameters.MaxBackoff,
                                                        Constants.Parameters.MinBackoff,
                                                        Constants.Parameters.DeltaBackoff
                };
                //Parsing input arguments.
                for (int i = 2; i < args.Length; )
                {
                    // Checks if argument is not defined with special character(-).
                    if (args[i][0] != '-')
                    {
                        Logger.Error(methodName, StringResources.InvalidArgumentDeclaration);
                        Console.WriteLine(StringResources.InvalidArgumentDeclaration);
                        ShowExportHelp();
                        return false;
                    }

                    // Checks for valid argument names.                 
                    string argument = args[i].Substring(1, args[i].Length - 1);
                    bool validParameter = requiredParameters.Any(a => string.Compare(a, argument, StringComparison.InvariantCultureIgnoreCase) == 0);
                    if (!validParameter)
                    {
                        Logger.Error(methodName, string.Format(StringResources.InvalidArgumentName, argument));
                        Console.WriteLine(StringResources.InvalidArgumentName, argument);
                        ShowExportHelp();
                        return false;
                    }
                    bool boolValue;
                    //Check if QuietMode parameter does not contain value other than true/ false.
                    if (string.Compare(argument, Constants.Parameters.QuietMode, StringComparison.CurrentCultureIgnoreCase) == 0
                        && (!bool.TryParse(args[i + 1], out boolValue)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.WrongInputForBoolArg, argument));
                        Console.WriteLine(StringResources.WrongInputForBoolArg, argument);
                        ShowExportHelp();
                        return false;
                    }
                    //Check if GenerateMapperXml parameter does not contain value other than true/ false.
                    if (string.Compare(argument, Constants.Parameters.GenerateMapperXml, StringComparison.CurrentCultureIgnoreCase) == 0
                        && (!bool.TryParse(args[i + 1], out boolValue)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.WrongInputForBoolArg, argument));
                        Console.WriteLine(StringResources.WrongInputForBoolArg, argument);
                        ShowExportHelp();
                        return false;
                    }

                    //Checking mandatary parameters.
                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourceSubscriptionID)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter, argument));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceSubscriptionID, AppConstants.CommandLineArguments);
                        ShowExportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourcePublishSettingsFilePath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourcePublishSettingsFilePath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourcePublishSettingsFilePath, AppConstants.CommandLineArguments);
                        ShowExportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourceDCName)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceDCName, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceDCName, AppConstants.CommandLineArguments);
                        ShowExportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.ExportMetadataFolderPath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                         Constants.Parameters.ExportMetadataFolderPath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.ExportMetadataFolderPath, AppConstants.CommandLineArguments);
                        ShowExportHelp();
                        return false;
                    }

                    // Adds valid argument with values in dictionary.
                    parameters.Add(argument, args[i + 1]);
                    i = i + 2;
                }
            }
            // Validates Import input parameters.
            else if (string.Compare(args[1], Constants.Operations.Import, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                parameters.Add(args[0].Substring(1, args[0].Length - 1), args[1]);
                requiredParameters = new List<string>() {Constants.Parameters.SourceSubscriptionID,
                                                        Constants.Parameters.DestinationSubscriptionID,
                                                        Constants.Parameters.SourcePublishSettingsFilePath,
                                                        Constants.Parameters.DestinationPublishSettingsFilePath,
                                                        Constants.Parameters.SourceCertificateThumbprint,
                                                        Constants.Parameters.DestinationCertificateThumbprint,
                                                        Constants.Parameters.DestinationDCName,
                                                        Constants.Parameters.ImportMetadataFilePath,
                                                        Constants.Parameters.MapperXmlFilePath,
                                                        Constants.Parameters.RetryCount,
                                                        Constants.Parameters.MaxBackoff,
                                                        Constants.Parameters.MinBackoff,
                                                        Constants.Parameters.DeltaBackoff,
                                                        Constants.Parameters.QuietMode,
                                                        Constants.Parameters.ResumeImport,
                                                        Constants.Parameters.RollBackOnFailure,
                                                        Constants.Parameters.DestinationPrefixName
                };
                //Parsing input arguments.
                for (int i = 2; i < args.Length; )
                {
                    // Checks if argument is not defined with special character(-).
                    if (args[i][0] != '-')
                    {
                        Logger.Error(methodName, StringResources.MissingRequiredParameter);
                        Console.WriteLine(StringResources.InvalidArgumentDeclaration);
                        ShowImportHelp();
                        return false;
                    }

                    // Checks for valid argument names.                 
                    string argument = args[i].Substring(1, args[i].Length - 1);
                    bool validParameter = requiredParameters.Any(a => string.Compare(a, argument, StringComparison.InvariantCultureIgnoreCase) == 0);
                    if (!validParameter)
                    {
                        Logger.Error(methodName, string.Format(StringResources.InvalidArgumentName, argument));
                        Console.WriteLine(StringResources.InvalidArgumentName, argument);
                        ShowImportHelp();
                        return false;
                    }
                    bool boolValue;
                    //Check if QuietMode parameter does not contain value other than true/false.
                    if (string.Compare(argument, Constants.Parameters.QuietMode, StringComparison.CurrentCultureIgnoreCase) == 0
                       && (!bool.TryParse(args[i + 1], out boolValue)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.WrongInputForBoolArg, argument));
                        Console.WriteLine(StringResources.WrongInputForBoolArg, argument);
                        ShowImportHelp();
                        return false;
                    }

                    //Check if RollBackOnFailure parameter does not contain value other than true/ false.
                    if (string.Compare(argument, Constants.Parameters.RollBackOnFailure, StringComparison.CurrentCultureIgnoreCase) == 0
                       && (!bool.TryParse(args[i + 1], out boolValue)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.WrongInputForBoolArg, argument));
                        Console.WriteLine(StringResources.WrongInputForBoolArg, argument);
                        ShowImportHelp();
                        return false;
                    }

                    //Check if ResumeImport parameter does not contain value other than true/ false.
                    if (string.Compare(argument, Constants.Parameters.ResumeImport, StringComparison.CurrentCultureIgnoreCase) == 0
                         && (!bool.TryParse(args[i + 1], out boolValue)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.WrongInputForBoolArg, argument));
                        Console.WriteLine(StringResources.WrongInputForBoolArg, argument);
                        ShowImportHelp();
                        return false;
                    }

                    //Checking mandatary parameters.
                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourceSubscriptionID)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceSubscriptionID, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceSubscriptionID, AppConstants.CommandLineArguments);
                        ShowExportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.DestinationSubscriptionID)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationSubscriptionID, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationSubscriptionID, AppConstants.CommandLineArguments);
                        ShowImportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourcePublishSettingsFilePath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourcePublishSettingsFilePath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourcePublishSettingsFilePath, AppConstants.CommandLineArguments);
                        ShowExportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.DestinationPublishSettingsFilePath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationPublishSettingsFilePath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationPublishSettingsFilePath, AppConstants.CommandLineArguments);
                        ShowImportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.DestinationDCName)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationDCName, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationDCName, AppConstants.CommandLineArguments);
                        ShowImportHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.ImportMetadataFilePath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.ImportMetadataFilePath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.ImportMetadataFilePath, AppConstants.CommandLineArguments);
                        ShowImportHelp();
                        return false;
                    }

                    // Adds valid argument with values in dictionary.
                    parameters.Add(argument, args[i + 1]);
                    i = i + 2;
                }
            }
            // Validates Migrate input parameters.
            else if (string.Compare(args[1], Constants.Operations.Migrate, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                requiredParameters = new List<string>() { Constants.Parameters.SourceSubscriptionID,
                                                        Constants.Parameters.DestinationSubscriptionID,
                                                        Constants.Parameters.SourcePublishSettingsFilePath,
                                                        Constants.Parameters.DestinationPublishSettingsFilePath,
                                                        Constants.Parameters.SourceCertificateThumbprint,
                                                        Constants.Parameters.DestinationCertificateThumbprint,
                                                        Constants.Parameters.SourceDCName,
                                                        Constants.Parameters.DestinationDCName,                                                        
                                                        Constants.Parameters.ExportMetadataFolderPath,
                                                        Constants.Parameters.RetryCount,
                                                        Constants.Parameters.MaxBackoff,
                                                        Constants.Parameters.MinBackoff,
                                                        Constants.Parameters.DeltaBackoff,
                                                        Constants.Parameters.QuietMode,
                                                        Constants.Parameters.ResumeImport,
                                                        Constants.Parameters.RollBackOnFailure,
                                                        Constants.Parameters.DestinationPrefixName
                };
                //Parsing input arguments.
                for (int i = 2; i < args.Length; )
                {
                    // Checks if argument is not defined with special character(-).
                    if (args[i][0] != '-')
                    {
                        Logger.Error(methodName, StringResources.InvalidArgumentDeclaration);
                        Console.WriteLine(StringResources.InvalidArgumentDeclaration);
                        ShowMigrateHelp();
                        return false;
                    }
                    bool boolValue;
                    // Checks for valid argument names.                 
                    string argument = args[i].Substring(1, args[i].Length - 1);
                    bool validParameter = requiredParameters.Any(a => string.Compare(a, argument, StringComparison.InvariantCultureIgnoreCase) == 0);
                    if (!validParameter)
                    {
                        Logger.Error(methodName, string.Format(StringResources.InvalidArgumentName, argument));
                        Console.WriteLine(StringResources.InvalidArgumentName, argument);
                        ShowMigrateHelp();
                        return false;
                    }

                    //Check if QuietMode parameter does not contain value other than true/false.
                    if (string.Compare(argument, Constants.Parameters.QuietMode, StringComparison.CurrentCultureIgnoreCase) == 0
                          && (!bool.TryParse(args[i + 1], out boolValue)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.WrongInputForBoolArg, argument));
                        Console.WriteLine(StringResources.WrongInputForBoolArg, argument);
                        ShowMigrateHelp();
                        return false;
                    }

                    //Check if RollBackOnFailure parameter does not contain value other than true/ false.
                    if (string.Compare(argument, Constants.Parameters.RollBackOnFailure, StringComparison.CurrentCultureIgnoreCase) == 0
                       && (!bool.TryParse(args[i + 1], out boolValue)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.WrongInputForBoolArg, argument));
                        Console.WriteLine(StringResources.WrongInputForBoolArg, argument);
                        ShowMigrateHelp();
                        return false;
                    }

                    //Checking mandatary parameters.                    
                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourceSubscriptionID)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceSubscriptionID, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceSubscriptionID, AppConstants.CommandLineArguments);
                        ShowMigrateHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.DestinationSubscriptionID)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationSubscriptionID, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationSubscriptionID, AppConstants.CommandLineArguments);
                        ShowMigrateHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourcePublishSettingsFilePath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourcePublishSettingsFilePath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourcePublishSettingsFilePath, AppConstants.CommandLineArguments);
                        ShowMigrateHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.DestinationPublishSettingsFilePath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationPublishSettingsFilePath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationPublishSettingsFilePath, AppConstants.CommandLineArguments);
                        ShowMigrateHelp();
                        return false;
                    }
                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.SourceDCName)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceDCName, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.SourceDCName, AppConstants.CommandLineArguments);
                        ShowMigrateHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.DestinationDCName)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationDCName, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.DestinationDCName, AppConstants.CommandLineArguments);
                        ShowMigrateHelp();
                        return false;
                    }

                    if (!args.Contains(string.Format(AppConstants.CommandLineParam, Constants.Parameters.ExportMetadataFolderPath)))
                    {
                        Logger.Error(methodName, string.Format(StringResources.MissingRequiredParameter,
                            Constants.Parameters.ExportMetadataFolderPath, AppConstants.CommandLineArguments));
                        Console.WriteLine(StringResources.MissingRequiredParameter,
                            Constants.Parameters.ExportMetadataFolderPath, AppConstants.CommandLineArguments);
                        ShowMigrateHelp();
                        return false;
                    }

                    // Adds valid argument with values in dictionary.
                    parameters.Add(argument, args[i + 1]);
                    i = i + 2;
                }
            }
            return true;
        }

        /// <summary>
        /// Shows DC migration progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void DCMigrationManager_Progress(object sender, ProgressEventArgs e)
        {
            Console.WriteLine("{0}: {1}", e.EventDateTime, e.Message);
        }
    }
}
