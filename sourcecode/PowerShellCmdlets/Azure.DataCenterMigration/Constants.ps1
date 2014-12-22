##############################################################################################################################
 # Copyright 2014 Persistent Systems Ltd.
 # 
 # Licensed under the Apache License, Version 2.0 (the "License");
 # you may not use this file except in compliance with the License.
 # You may obtain a copy of the License at
 # 
 #   http://www.apache.org/licenses/LICENSE-2.0
 # 
 # Unless required by applicable law or agreed to in writing, software
 # distributed under the License is distributed on an "AS IS" BASIS,
 # WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 # See the License for the specific language governing permissions and
 # limitations under the License.
 
  # Script Name  : Constants.ps1
 # Description  : This script  stores the constants required for Azure.DataCenterMigration PowerShell Cmdlets.
 # Created Date : 11/07/2014

##############################################################################################################################

# Arguments for Export-SubscriptionMetadata

if(!$SourceSubscriptionIDArg)
{
    New-Variable -Name SourceSubscriptionIDArg -Value "SourceSubscriptionID"-Scope "Global" 
}

if(!$SourceDCNameArg)
{
    New-Variable -Name SourceDCNameArg -Value "SourceDCName"-Scope "Global" 
}

if(!$ExportMetadataFolderPathArg)
{
    New-Variable -Name ExportMetadataFolderPathArg -Value "ExportMetadataFolderPath"-Scope "Global" 
}

if(!$SourcePublishSettingsFilePathArg)
{
    New-Variable -Name SourcePublishSettingsFilePathArg -Value "SourcePublishSettingsFilePath"-Scope "Global" 
}

if(!$SourceCertificateThumbprintArg)
{
    New-Variable -Name SourceCertificateThumbprintArg -Value "SourceCertificateThumbprint"-Scope "Global" 
}

if(!$QuietModeArg)
{
    New-Variable -Name QuietModeArg -Value "QuietMode" -Scope "Global" 
}

# Arguments for Import-SubscriptionMetadata

if(!$DestinationSubscriptionIDArg)
{
    New-Variable -Name DestinationSubscriptionIDArg -Value "DestinationSubscriptionID"-Scope "Global" 
}

if(!$DestinationDCNameArg)
{
    New-Variable -Name DestinationDCNameArg -Value "DestinationDCName"-Scope "Global" 
}

if(!$DestinationPublishSettingsFilePathArg)
{
    New-Variable -Name DestinationPublishSettingsFilePathArg -Value "DestinationPublishSettingsFilePath"-Scope "Global" 
}

if(!$DestinationCertificateThumbprintArg)
{
    New-Variable -Name DestinationCertificateThumbprintArg -Value "DestinationCertificateThumbprint"-Scope "Global" 
}


if(!$ImportMetadataFilePathArg)
{
    New-Variable -Name ImportMetadataFilePathArg -Value "ImportMetadataFilePath"-Scope "Global" 
}

if(!$MapperXmlFilePathArg)
{
    New-Variable -Name MapperXmlFilePathArg -Value "MapperXmlFilePath"-Scope "Global" 
}

if(!$DestinationPrefixNameArg)
{
    New-Variable -Name DestinationPrefixNameArg -Value "DestinationPrefixName"-Scope "Global" 
}

if(!$RollBackOnFailureArg)
{
    New-Variable -Name RollBackOnFailureArg -Value "RollBackOnFailure"-Scope "Global" 
}

if(!$ResumeImportArg)
{
    New-Variable -Name ResumeImportArg -Value "ResumeImport"-Scope "Global" 
}

if(!$RetryCountArg)
{
    New-Variable -Name RetryCountArg -Value "RetryCount"-Scope "Global" 
}

if(!$MinBackoffArg)
{
    New-Variable -Name MinBackoffArg -Value "MinBackoff"-Scope "Global" 
}

if(!$MaxBackoffArg)
{
    New-Variable -Name MaxBackoffArg -Value "MaxBackoff"-Scope "Global" 
}

if(!$DeltaBackoffArg)
{
    New-Variable -Name DeltaBackoffArg -Value "DeltaBackoff"-Scope "Global" 
}

if(!$GenerateMapperXmlArg)
{
    New-Variable -Name GenerateMapperXmlArg -Value "GenerateMapperXml"-Scope "Global" 
}

if(!$ConstDLLNotFound)
{
    New-Variable -Name ConstDLLNotFound -Value "Dll file not found :"-Scope "Global" 
}

if(!$ConstError)
{
    New-Variable -Name ConstError -Value "Error"-Scope "Global" 
}

if(!$ConstInfo)
{
    New-Variable -Name ConstInfo -Value "Info"-Scope "Global" 
}

if(!$ConstValidationException)
{
    New-Variable -Name ConstValidationException -Value "Validation exception occurred: "-Scope "Global" 
}

if(!$ConstException)
{
    New-Variable -Name ConstException -Value "Exception occurred: "-Scope "Global" 
}

if(!$ConstSelectValid)
{
    New-Variable -Name ConstSelectValid -Value "Please Select a Valid Option (Y/N)" -Scope "Global" 
}

if(!$ConstImportWarning)
{
    New-Variable -Name ConstImportWarning -Value "`nWARNING: Import process will stop virtual machines of source subscription. Do you want to continue? (Y/N) : " -Scope "Global" 
}

if(!$ConstImportInfo)
{
    New-Variable -Name ConstImportInfo -Value "`nINFO: Progress reporting on console is not provided for this operation. To check the progress, please check the log file." -Scope "Global" 
}

if(!$ConstImportWarningLog)
{
    New-Variable -Name ConstImportWarningLog -Value "`WARNING: Import process will stop virtual machines of source subscription." -Scope "Global" 
}

if(!$ConstYes)
{
    New-Variable -Name ConstYes -Value "Y" -Scope "Global" 
}

if(!$ConstNo)
{
    New-Variable -Name ConstNo -Value "N" -Scope "Global" 
}

if(!$ConstFunctionImport)
{
    New-Variable -Name ConstFunctionImport -Value "Powershell Import" -Scope "Global" 
}

if(!$ConstFunctionMigrate)
{
    New-Variable -Name ConstFunctionMigrate -Value "Powershell Migrate" -Scope "Global" 
}

if(!$ConstMissingAuthenticationParameter)
{
    New-Variable -Name ConstMissingAuthenticationParameter -Value "Either publish setting file or certificate file should be selected for {0}" -Scope "Global" 
}

if(!$ConstMissingXMlorDestinationPrefixParameter)
{
    New-Variable -Name ConstMissingXMlorDestinationPrefixParameter -Value "Either destination prefix or mapper xml file path should be set for {0}" -Scope "Global" 
}

if(!$ConstSource)
{
    New-Variable -Name ConstSource -Value "Source" -Scope "Global" 
}

if(!$ConstDestination)
{
    New-Variable -Name ConstDestination -Value "Destination" -Scope "Global" 
}
