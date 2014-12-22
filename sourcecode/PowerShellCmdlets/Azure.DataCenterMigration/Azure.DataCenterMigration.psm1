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


# Script Name  : Azure.DataCenterMigration.ps1
# Description  : This script allows user to call Export-AzureSubscriptionMetadata / Export-AzureSubscriptionMetadata / Migrate-AzureSubscription functions by calling Azure.DataCenterMigration.dll.
#                Data Center Migration Solution is used to migrate Microsoft Azure resources:
#                •	From one subscription to another subscription in same data center (region).
#                •	From one subscription to another subscription in different data center.
#                •	In same subscription with different data center.
#                •	In same subscription with same data center.
#                This version of the solution supports migration of following resources:
#                •	All Affinity Groups in the source data center.
#                •	All Networks in the source data center.
#                •	All Cloud Services in the source data center.
#                •	All Storage Accounts in the source data center. 
#                  ( All storage accounts irrespective of virtual machines will be created on destination however only blobs related to 
#                    virtual machines will be copied.)
#                •	All Virtual Machines in the data center.
# Created Date : 11/07/2014

##############################################################################################################################


##############################################################################################################################
# Function Name : Export-AzureSubscriptionMetadata = Exports information about source subscription and stores the metadata 
#                 into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json format on specified ExportMetadataFolderPath.
# Parameters    : 1] SourcePublishSettingsFilePath = SourcePublishSettings file path.
#                 2] SourceCertificateThumbprint = Certificate thumbprint for source subscription.
#                 3] SourceSubscriptionID = Source Subscription Id.
#                 4] SourceDCName = Source Data Center Name.
#                 5] ExportMetadataFolderPath = Folder path where the exported metadata file will be saved.
#                 6] QuietMode = (optional) true if user don&apos;t want to print progress messages on console. Default is false. 
#                 7] RetryCount = (optional) No. of times retry in case of exception, Default is '5'")]
#                 9] MinBackoff = (optional) Minimum backoff in seconds, Default is 3 seconds.
#                 10] MaxBackoff = (optional) Maximum backoff in seconds, Default is 90 seconds.
#                 11] DeltaBackoff = (optional) Delta backoff in seconds, Default is 90 seconds.
#                 12] GenerateMapperXml = (optional) true if user wants to create Resource Name Mapper xml file. Default is false.
#                 13] DestinationPrefixName = (optional) Destination Prefix Name.Required if GenerateMapperXml is set true.
# Returns       : None   
##############################################################################################################################  
Function Export-AzureSubscriptionMetadata()
{
    <#
    .SYNOPSIS
      Exports information about Microsoft Azure source subscription for specified datacenter name.
    .DESCRIPTION
      Exports information about Microsoft Azure source subscription and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath.
    .INPUTS
      None
    .OUTPUTS
      None
    .Link
      Import-AzureSubscriptionMetadata
      Migrate-AzureSubscription
    .EXAMPLE
      Export-AzureSubscriptionMetadata -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -ExportMetadataFolderPath "D:\\DataCenterMigration" 
      This command exports source subscription information using PublishSettings file path and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath. The progress messages will be printed on host to track which step is running on.
    .EXAMPLE
      Export-AzureSubscriptionMetadata -SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -ExportMetadataFolderPath "D:\\DataCenterMigration" 
      This command exports source subscription information using thumbprint of source subscription and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath. The progress messages will be printed on host to track which step is running on.
    .EXAMPLE
      Export-AzureSubscriptionMetadata -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -ExportMetadataFolderPath "D:\\DataCenterMigration" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -QuietMode "True"
      This command exports source subscription information and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath. The progress messages will not be printed on host since quietMode is True
    .EXAMPLE
      Export-AzureSubscriptionMetadata -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -ExportMetadataFolderPath "D:\\DataCenterMigration" -RetryCount "5" -MinBackoff "1" -MaxBackoff "120" -DeltaBackoff "120" -GenerateMapperXml "true" -DestinationPrefixName "dc"
      This command exports source subscription information using PublishSettings file path and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath. The progress messages will be printed on host to track which step is running on. On failure, the solution will retry 5 times with retry strategy backoff as Minimum 1 sec, maximum 120 sec and delta 120 sec. Resource Name mapper xml will be generated as GenerateMapperXml is true.
   #>
   [cmdletbinding()]
    param
    (
        [Parameter(Mandatory=$False,HelpMessage="SourcePublishSettings file path")]
        [string]$SourcePublishSettingsFilePath,
        [Parameter(Mandatory=$False,HelpMessage="Certificate thumbprint for source subscription")]
        [string]$SourceCertificateThumbprint,
        [Parameter(Mandatory=$True,HelpMessage="Source Subscription Id")]
        [string]$SourceSubscriptionID,
        [Parameter(Mandatory=$True,HelpMessage="Source Data Center Name")]
        [string]$SourceDCName,
        [Parameter(Mandatory=$True,HelpMessage="Folder path where the exported metadata file will be saved")]
        [string]$ExportMetadataFolderPath,
        [Parameter(Mandatory=$False,HelpMessage="(optional) true if user don't want to print progress messages on console. Default is false.")]
        [string]$QuietMode = "false",
        [Parameter(Mandatory=$False,HelpMessage="(optional) No. of times retry in case of exception, Default is '5'")]
        [string]$RetryCount="5",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Minimum backoff in seconds, Default is 3 seconds")]
        [string]$MinBackoff="3",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Maximum backoff in seconds, Default is 90 seconds")]
        [string]$MaxBackoff="90",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Delta backoff in seconds, Default is 90 seconds")]
        [string]$DeltaBackoff="90",
        [Parameter(Mandatory=$False,HelpMessage="(optional) true if user wants to create Resource Name Mapper xml file. Default is false.")]
        [string]$GenerateMapperXml = "false",
        [Parameter(Mandatory=$False,HelpMessage="Destination Prefix Name")]
        [string]$DestinationPrefixName =""
    )

    $script:quietMode = $QuietMode
    Setup-Environment
    
    if ((test-path $script:dllPath) -eq $False)
    {
        $message =  $ConstDLLNotFound + $script:dllPath
        Write-ConsoleMessage $message $ConstError
        write-host
        break;
    }
    else
    {        
        Add-Type -Path $script:dllPath
        try
        {
            $argDictionary = New-Object 'system.collections.generic.dictionary[string,string]'
    
            # Add all input arguments in dictionary and pass it as parameter to function ExportSubscriptionMetadata()
            if($SourcePublishSettingsFilePath -and $SourcePublishSettingsFilePath -ne '')
            {
                $argDictionary.Add($SourcePublishSettingsFilePathArg,$SourcePublishSettingsFilePath);
            }
            elseif($SourceCertificateThumbprint -and $SourceCertificateThumbprint -ne '')
            {
                $argDictionary.Add($SourceCertificateThumbprintArg,$SourceCertificateThumbprint);
            }
            else 
            {
                $message = $ConstMissingAuthenticationParameter -f $ConstSource
                Write-ConsoleMessage $message $ConstError
                break;
            }          
            $argDictionary.Add($SourceSubscriptionIDArg,$SourceSubscriptionID);
            $argDictionary.Add($SourceDCNameArg,$SourceDCName);
            $argDictionary.Add($ExportMetadataFolderPathArg,$ExportMetadataFolderPath);
            $argDictionary.Add($QuietModeArg,$QuietMode);
            $argDictionary.Add($MinBackoffArg,$MinBackoff);
            $argDictionary.Add($MaxBackoffArg,$MaxBackoff);
            $argDictionary.Add($DeltaBackoffArg,$DeltaBackoff);
            $argDictionary.Add($RetryCountArg,$RetryCount);            
            $argDictionary.Add($GenerateMapperXmlArg,$GenerateMapperXml);
            $argDictionary.Add($DestinationPrefixNameArg,$DestinationPrefixName);
            $dcMigration = New-Object Azure.DataCenterMigration.DCMigrationManager            
            
            Configure-Logging
            $dcMigration.add_Progress({ Write-Host ("{0} {1} " -f $_.EventDateTime , $_.Message) -ForegroundColor White})
            try
            {            
                #Export metadata    
                $dcMigration.ExportSubscriptionMetadata($argDictionary);                                
           }
            # Catch Validation exception
            catch  [Azure.DataCenterMigration.ValidationException] {
                $message = $ConstValidationException +  $_.Exception.Message
                Write-ConsoleMessage $message $ConstError
                cd $script:currentWorkingDirectory
                break;                
            }
        }
        # Catch exception
        catch  [Exception] {
            $message = $ConstException +  $_.Exception.Message
            Write-ConsoleMessage $message $ConstError
            cd $script:currentWorkingDirectory
            break;            
        }         
    }
    cd $script:currentWorkingDirectory
    
}

##############################################################################################################################
# Function Name : Import-AzureSubscriptionMetadata = Reads exported metadata json file and deploys all the source resources into destination subscription.
# Parameters    : 1] SourcePublishSettingsFilePath = Source PublishSettings file path.
#                 2] DestinationPublishSettingsFilePath = Destination PublishSettings file path.
#                 3] SourceCertificateThumbprint = Certificate thumbprint for source subscription.
#                 4] DestinationCertificateThumbprint = Certificate thumbprint for destination subscription.
#                 5] SourceSubscriptionID = Source Subscription Id.
#                 6] DestinationSubscriptionID = Destination Subscription Id.
#                 7] DestinationDCName = Destination Data Center Name.
#                 8] ImportMetadataFilePath = File path where the metadata is saved.
#                 9] DestinationPrefixName = Destination Prefix Name.
#                 10] RetryCount = (optional) No. of times retry in case of exception, Default is '5'")]
#                 11] ResumeImport = (optional)true if user wants to update import status in the same file. Default is false.
#                 12] RollBackOnFailure = (optional)true if user wants to rollback all imported resources. Default is false.
#                 13] QuietMode = (optional) true if user don&apos;t want to print progress messages on console. Default is false.
#                 14] MinBackoff = (optional) Minimum backoff in seconds, Default is 3 seconds
#                 15] MaxBackoff = (optional) Maximum backoff in seconds, Default is 90 seconds
#                 16] DeltaBackoff = (optional) Delta backoff in seconds, Default is 90 seconds
#                 17] MapperXmlFilePath = File Path  where resource name mapper xml is saved.
# Returns       : None   
##############################################################################################################################
Function Import-AzureSubscriptionMetadata()
{  
    <#
    .SYNOPSIS
      Reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription
    .DESCRIPTION
      Reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription
    .INPUTS
      None
    .OUTPUTS
      None
    .Link
      Import-AzureSubscriptionMetadata
      Migrate-AzureSubscription
    .EXAMPLE
      Import-AzureSubscriptionMetadata -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -DestinationPrefixName "dc"
      This command reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription using publish settings file. The progress messages will be printed on host to track which step is running on. This command will not resume import function on failure also will not delete the imported resources on failure.
    .EXAMPLE
      Import-AzureSubscriptionMetadata -SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -DestinationCertificateThumbprint "26ee0e5ddbcc6e8d2efa8ddb98c72180d7827689" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -DestinationPrefixName "dc"
      This command reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription using thumbprints of source and destination subscriptions. The progress messages will be printed on host to track which step is running on. This command will not resume import function on failure also will not delete the imported resources on failure.
     .EXAMPLE
      Import-AzureSubscriptionMetadata -SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -DestinationCertificateThumbprint "26ee0e5ddbcc6e8d2efa8ddb98c72180d7827689" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -MapperXmlFilePath "D:\\DataCenterMigration\mydata.xml"
      This command reads metadata json file and resource name mapper xml file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription using thumbprints of source and destination subscriptions. The progress messages will be printed on host to track which step is running on. This command will not resume import function on failure also will not delete the imported resources on failure.
    .EXAMPLE
      Import-AzureSubscriptionMetadata -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -DestinationPrefixName "dc" -ResumeImport "True"
      This command reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription. The progress messages will be printed on host to track which step is running on. This command will resume import function from point of failure. The imported resources will not get deleted on failure.
    .EXAMPLE
      Import-AzureSubscriptionMetadata -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -DestinationPrefixName "dc" -RollBackOnFailure "True"
      This command reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription. The progress messages will be printed on host to track which step is running on. This command will not resume import function from point of failure. The imported resources will get deleted on failure.
    .EXAMPLE
      Import-AzureSubscriptionMetadata -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -DestinationPrefixName "dc" -QuietMode "True"
      This command reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription. The progress messages will not be printed on host to track which step is running on. This command will not resume import function from point of failure, also the imported resources will not get deleted on failure.    
    .EXAMPLE
      Import-AzureSubscriptionMetadata -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -ImportMetadataFilePath "D:\\DataCenterMigration\mydata.json" -DestinationPrefixName "dc" -RetryCount "5" -MinBackoff "1" -MaxBackoff "120" -DeltaBackoff "120"
      This command reads metadata json file exported by Export-AzureSubscriptionMetadata and deploys all the source resources into destination subscription. The progress messages will be printed on host to track which step is running on. This command will not resume import function from point of failure, also the imported resources will not get deleted on failure. On failure, the solution will retry 5 times retry 5 times with retry strategy backoff as Minimum 1 sec, maximum 120 sec and delta 120 sec.
    #>
    [cmdletbinding()]
    param
    (
        [Parameter(Mandatory=$False,HelpMessage="Source PublishSettings file path")]
        [string]$SourcePublishSettingsFilePath,
        [Parameter(Mandatory=$False,HelpMessage="Destination PublishSettings file path")]
        [string]$DestinationPublishSettingsFilePath,
        [Parameter(Mandatory=$False,HelpMessage="Certificate thumbprint for source subscription")]
        [string]$SourceCertificateThumbprint,
        [Parameter(Mandatory=$False,HelpMessage="Certificate thumbprint for destination subscription")]
        [string]$DestinationCertificateThumbprint,
        [Parameter(Mandatory=$True,HelpMessage="Source Subscription Id")]
        [string]$SourceSubscriptionID,
        [Parameter(Mandatory=$True,HelpMessage="Destination Subscription Id")]
        [string]$DestinationSubscriptionID,
        [Parameter(Mandatory=$False,HelpMessage="Destination Data Center Name")]
        [string]$DestinationDCName="",        
        [Parameter(Mandatory=$True,HelpMessage="File path where the exported metadata is saved")]
        [string]$ImportMetadataFilePath,
        [Parameter(Mandatory=$False,HelpMessage="Destination Prefix Name")]
        [string]$DestinationPrefixName,
        [Parameter(Mandatory=$False,HelpMessage="(optional) No. of times retry in case of exception, Default is '5'")]
        [string]$RetryCount="5",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Minimum backoff in seconds, Default is 3 seconds")]
        [string]$MinBackoff="3",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Maximum backoff in seconds, Default is 90 seconds")]
        [string]$MaxBackoff="90",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Delta backoff in seconds, Default is 90 seconds")]
        [string]$DeltaBackoff="90",
        [Parameter(Mandatory=$False,HelpMessage="(optional) File Path  where resource name mapper xml is saved.")]
        [string]$MapperXmlFilePath = "",
        [Parameter(Mandatory=$False,HelpMessage="(optional) true if user wants to update import status in the same file. Default is false")]
        [string]$ResumeImport="false",
        [Parameter(Mandatory=$False,HelpMessage="(optional) true if user wants to rollback all imported resources. Default is false")]
        [string]$RollBackOnFailure = "false",
        [Parameter(Mandatory=$False,HelpMessage="(optional) true if user don't want to print progress messages on console. Default is false")]
        [string]$QuietMode="false"

    )
    $script:quietMode = $QuietMode
    Setup-Environment
    
    if ((test-path $script:dllPath) -eq $False)
    {
        $message =  $ConstDLLNotFound + $script:dllPath
        Write-ConsoleMessage $message $ConstError
        write-host
        break;
    }
    else
    {        
        Add-Type -Path $script:dllPath
        try
        {
            $dcMigration = New-Object Azure.DataCenterMigration.DCMigrationManager             
            $argDictionary = New-Object 'system.collections.generic.dictionary[string,string]'
    
            # Add all input arguments in dictionary and pass it as parameter to function ImportSubscriptionMetadata()
            if($SourcePublishSettingsFilePath -and $SourcePublishSettingsFilePath -ne '')
            {
                $argDictionary.Add($SourcePublishSettingsFilePathArg,$SourcePublishSettingsFilePath);
            }
            elseif($SourceCertificateThumbprint -and $SourceCertificateThumbprint -ne '')
            {
                $argDictionary.Add($SourceCertificateThumbprintArg,$SourceCertificateThumbprint);
            }
            else 
            {
                $message = $ConstMissingAuthenticationParameter -f $ConstSource
                Write-ConsoleMessage $message $ConstError
                break;
            }

            if($DestinationPublishSettingsFilePath -and $DestinationPublishSettingsFilePath -ne '')
            {
                $argDictionary.Add($DestinationPublishSettingsFilePathArg,$DestinationPublishSettingsFilePath);
            }
            elseif($DestinationCertificateThumbprint -and $DestinationCertificateThumbprint -ne '')
            {
                $argDictionary.Add($DestinationCertificateThumbprintArg,$DestinationCertificateThumbprint);
            }
            else
            {
                $message = $ConstMissingAuthenticationParameter -f $ConstDestination
                Write-ConsoleMessage $message $ConstError
                break;
            }
            
            if($MapperXmlFilePath -and $MapperXmlFilePath -ne '')
            {
                $argDictionary.Add($MapperXmlFilePathArg,$MapperXmlFilePath);
            }
            elseif($DestinationPrefixName -and $DestinationPrefixName -ne '')
            {
                $argDictionary.Add($DestinationPrefixNameArg,$DestinationPrefixName);
            }
            else 
            {
                $message = $ConstMissingXMlorDestinationPrefixParameter -f $ConstDestination
                Write-ConsoleMessage $message $ConstError
                break;
            }
            $argDictionary.Add($SourceSubscriptionIDArg,$SourceSubscriptionID);
            $argDictionary.Add($DestinationSubscriptionIDArg,$DestinationSubscriptionID);
            $argDictionary.Add($DestinationDCNameArg,$DestinationDCName);        
            $argDictionary.Add($MinBackoffArg,$MinBackoff);
            $argDictionary.Add($MaxBackoffArg,$MaxBackoff);
            $argDictionary.Add($DeltaBackoffArg,$DeltaBackoff);
            $argDictionary.Add($RetryCountArg,$RetryCount); 
            $argDictionary.Add($ResumeImportArg,$ResumeImport);
            $argDictionary.Add($RollBackOnFailureArg,$RollBackOnFailure);
            $argDictionary.Add($ImportMetadataFilePathArg,$ImportMetadataFilePath);
            $argDictionary.Add($QuietModeArg,$QuietMode);
                                    
            Configure-Logging
            #$dcMigration.add_Progress({ Write-Host ("{0} {1} " -f $_.EventDateTime , $_.Message) -ForegroundColor Yellow  })
            try
            {
                [Azure.DataCenterMigration.Logger]::Info($ConstFunctionImport,$ConstImportWarningLog);

                if($QuietMode -eq $True.ToString())
                {
                    Write-Host $ConstImportWarningLog -ForegroundColor Yellow
                    Write-Host "$ConstImportInfo"  -ForegroundColor Yellow
                    #Import subscription metadata
                    $dcMigration.ImportSubscriptionMetadata($argDictionary);
                }
                else
                {                   
                    Write-Host $ConstImportWarning -ForegroundColor Yellow -NoNewline
                    $choice = Read-Host 
                  
                    while(1)
                    {                        
                        if($choice -eq $ConstYes)
                        {
                            Write-Host "$ConstImportInfo"  -ForegroundColor Yellow
                            #Import subscription metadata
                            $dcMigration.ImportSubscriptionMetadata($argDictionary);
                            break;
                        }
                        elseif ($choice -eq $ConstNo)
                        {                        
                            break;
                        }
                        $choice =  Read-Host $ConstSelectValid
                    }       
                }      
            }
            # Catch Validation exception
            catch  [Azure.DataCenterMigration.ValidationException] {
                $message = $ConstValidationException +  $_.Exception.Message
                Write-ConsoleMessage $message $ConstError
                cd $script:currentWorkingDirectory
                break;
                
            }
        }
        # Catch exception
        catch  [Exception] {
            $message = $ConstException +  $_.Exception.Message
            Write-ConsoleMessage $message $ConstError
            cd $script:currentWorkingDirectory
            break;
        }          
    }    
    cd $script:currentWorkingDirectory
}
##############################################################################################################################
# Function Name : Migrate-AzureSubscriptionMetadata = Combination of Export and Import functionality. 
#                 Exports information about source subscription and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format 
#                 on specified ExportMetadataFolderPath.
#                 Reads exported metadata json file and deploys all the source resources into destination subscription.
# Parameters    : 1] SourceSubscriptionID = Source Subscription Id.
#                 2] DestinationSubscriptionID = Destination Subscription Id.
#                 3] SourceDCName = Source Data Center Name.
#                 4] DestinationDCName = Destination Data Center Name.
#                 5] SourcePublishSettingsFilePath = SourcePublishSettings file path.
#                 6] DestinationPublishSettingsFilePath = Destination PublishSettings file path.
#                 7] ExportMetadataFolderPath = Folder path where the exported metadata file will be saved.
#                 8] DestinationPrefixName = Destination Prefix Name.
#                 9] RetryCount = (optional) No. of times retry in case of exception, Default is '5'")]
#                 10] RetryInterval = (optional) Time interval in minutes for retry in case of exception, Default is 2 minutes")]
#                 11] RollBackOnFailure = (optional)true if user wants to rollback all imported resources. Default is false.
#                 12] QuietMode = (optional) true if user don&apos;t want to print progress messages on console. Default is false.
#                 13] MinBackoff = (optional) Minimum backoff in seconds, Default is 3 seconds
#                 14] MaxBackoff = (optional) Maximum backoff in seconds, Default is 90 seconds
#                 15] DeltaBackoff = (optional) Delta backoff in seconds, Default is 90 seconds
# Returns       : None   
##############################################################################################################################
Function Migrate-AzureSubscription()
{ 
    <#
    .SYNOPSIS
      Export and Import functionality in single run
    .DESCRIPTION
      Exports information about source subscription and stores the metadata into 'SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath.
      Reads exported metadata json file and deploys all the source resources into destination subscription.
    .INPUTS
      None
    .OUTPUTS
      None
    .Link
      Import-AzureSubscriptionMetadata
      Export-AzureSubscriptionMetadata
    .EXAMPLE
      Migrate-AzureSubscription -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -DestinationDCName "West US"  -ExportMetadataFolderPath "D:\\DataCenterMigration" -DestinationPrefixName "dc"
      This command exports information for Microsoft Azure source subscription and stores the metadata into SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath using publish settings file path.
      It also deploys all the source resources into destination subscription.The progress messages will be printed on host to track which step is running on. This command will not delete the imported resources on failure.    
    .EXAMPLE
      Migrate-AzureSubscription -SourceCertificateThumbprint "2180d782768926ee0e5ddbcc6e8d2efa8ddb98c7" -DestinationCertificateThumbprint "26ee0e5ddbcc6e8d2efa8ddb98c72180d7827689" -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -DestinationDCName "West US"  -ExportMetadataFolderPath "D:\\DataCenterMigration" -DestinationPrefixName "dc"
      This command exports information for Microsoft Azure source subscription and stores the metadata into SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath using thumbprints of source and destination subscriptions.
      It also deploys all the source resources into destination subscription.The progress messages will be printed on host to track which step is running on. This command will not delete the imported resources on failure.
    .EXAMPLE
      Migrate-AzureSubscription -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -ExportMetadataFolderPath "D:\\DataCenterMigration" -DestinationPrefixName "dc" -RollBackOnFailure "True"
      This command exports information for Microsoft Azure source subscription and stores the metadata into SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath.
      It also deploys all the source resources into destination subscription.The progress messages will be printed on host to track which step is running on. This command will also delete the imported resources on failure.
    .EXAMPLE
      Migrate-AzureSubscription -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -ExportMetadataFolderPath "D:\\DataCenterMigration" -DestinationPrefixName "dc" -QuietMode "True"
      This command exports information for Microsoft Azure source subscription and stores the metadata into SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath.
      It also deploys all the source resources into destination subscription.The progress messages will not be printed on host to track which step is running on. This command will also not delete the imported resources on failure.
    .EXAMPLE
      Migrate-AzureSubscription -SourceSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -DestinationSubscriptionID "5d14d4a2-8c5a-4fc5-8d7d-86efb48b3a07" -SourceDCName "East Asia" -DestinationDCName "West US" -SourcePublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -DestinationPublishSettingsFilePath "D:\\PublishSettings.PublishSettings" -ExportMetadataFolderPath "D:\\DataCenterMigration" -DestinationPrefixName "dc" -RetryCount "5" -MinBackoff "1" -MaxBackoff "120" -DeltaBackoff "120"
      This command exports information for Microsoft Azure source subscription and stores the metadata into SourceDataCenterName-MM-DD-YYYY-hh-mm.json' format on specified ExportMetadataFolderPath.
      It also deploys all the source resources into destination subscription.The progress messages will be printed on host to track which step is running on. This command will also not delete the imported resources on failure. On failure, the solution will retry 5 times with retry strategy backoff as Minimum 1 sec, maximum 120 sec and delta 120 sec.    
    #>

    
    [cmdletbinding()]
    param
    (
        [Parameter(Mandatory=$False,HelpMessage="SourcePublishSettings file path")]
        [string]$SourcePublishSettingsFilePath,
        [Parameter(Mandatory=$False,HelpMessage="Destination file path")]
        [string]$DestinationPublishSettingsFilePath,
        [Parameter(Mandatory=$False,HelpMessage="Certificate thumbprint for source subscription")]
        [string]$SourceCertificateThumbprint,
        [Parameter(Mandatory=$False,HelpMessage="Certificate thumbprint for destination subscription")]
        [string]$DestinationCertificateThumbprint,
        [Parameter(Mandatory=$True,HelpMessage="Source Subscription Id")]
        [string]$SourceSubscriptionID,
        [Parameter(Mandatory=$True,HelpMessage="Destination Subscription Id")]
        [string]$DestinationSubscriptionID,
        [Parameter(Mandatory=$True,HelpMessage="Source Data Center Name")]
        [string]$SourceDCName,
        [Parameter(Mandatory=$True,HelpMessage="Destination Data Center Name")]
        [string]$DestinationDCName,        
        [Parameter(Mandatory=$True,HelpMessage="Folder path where the exported metadata file will be saved")]
        [string]$ExportMetadataFolderPath,
        [Parameter(Mandatory=$True,HelpMessage="Destination Prefix Name")]
        [string]$DestinationPrefixName,        
        [Parameter(Mandatory=$False,HelpMessage="(optional) No. of times retry in case of exception, Default is '5'")]
        [string]$RetryCount="5",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Minimum backoff in seconds, Default is 3 seconds")]
        [string]$MinBackoff="3",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Maxmum backoff in seconds, Default is 90 seconds")]
        [string]$MaxBackoff="90",
        [Parameter(Mandatory=$False,HelpMessage="(optional) Delta backoff in seconds, Default is 90 seconds")]
        [string]$DeltaBackoff="90",
        [Parameter(Mandatory=$False,HelpMessage="(optional)true if user wants to rollback all imported resources. Default is false")]
        [string]$RollBackOnFailure="false",
        [Parameter(Mandatory=$False,HelpMessage="(optional) true if user don't want to print progress messages on console. Default is false")]
        [string]$QuietMode = "false"
    )       
    
    $script:quietMode = $QuietMode 
    Setup-Environment    
    if ((test-path $script:dllPath) -eq $False)
    {
        $message =  $ConstDLLNotFound + $script:dllPath
        Write-ConsoleMessage $message $ConstError
        write-host
        break;
    }
    else
    {        
        Add-Type -Path $script:dllPath
        try
        {
            $dcMigration = New-Object Azure.DataCenterMigration.DCMigrationManager
    
            $argDictionary = New-Object 'system.collections.generic.dictionary[string,string]'
    
            # Add all input arguments in dictionary and pass it as parameter to function MigrateSubscription()
            if($SourcePublishSettingsFilePath -and $SourcePublishSettingsFilePath -ne '')
            {
                $argDictionary.Add($SourcePublishSettingsFilePathArg,$SourcePublishSettingsFilePath);
            }
            elseif($SourceCertificateThumbprint -and $SourceCertificateThumbprint -ne '')
            {
                $argDictionary.Add($SourceCertificateThumbprintArg,$SourceCertificateThumbprint);
            }
            else 
            {
                $message = $ConstMissingAuthenticationParameter -f $ConstSource
                Write-ConsoleMessage $message $ConstError
                break;
            }

            if($DestinationPublishSettingsFilePath -and $DestinationPublishSettingsFilePath -ne '')
            {
                $argDictionary.Add($DestinationPublishSettingsFilePathArg,$DestinationPublishSettingsFilePath);
            }
            elseif($DestinationCertificateThumbprint -and $DestinationCertificateThumbprint -ne '')
            {
                $argDictionary.Add($DestinationCertificateThumbprintArg,$DestinationCertificateThumbprint);
            }
            else
            {
                $message = $ConstMissingAuthenticationParameter -f $ConstDestination
                Write-ConsoleMessage $message $ConstError
                break;
            }
            $argDictionary.Add($SourceSubscriptionIDArg,$SourceSubscriptionID);
            $argDictionary.Add($DestinationSubscriptionIDArg,$DestinationSubscriptionID);
            $argDictionary.Add($SourceDCNameArg,$SourceDCName);
            $argDictionary.Add($DestinationDCNameArg,$DestinationDCName);            
            $argDictionary.Add($ExportMetadataFolderPathArg,$ExportMetadataFolderPath);
            $argDictionary.Add($DestinationPrefixNameArg,$DestinationPrefixName);
            $argDictionary.Add($MinBackoffArg,$MinBackoff);
            $argDictionary.Add($MaxBackoffArg,$MaxBackoff);
            $argDictionary.Add($DeltaBackoffArg,$DeltaBackoff);
            $argDictionary.Add($RetryCountArg,$RetryCount); 
            $argDictionary.Add($RollBackOnFailureArg,$RollBackOnFailure);
            $argDictionary.Add($QuietModeArg,$QuietMode);
                        
            Configure-Logging
            #$dcMigration.add_Progress([system.Action]{ Write-Host ("{0} {1} " -f $_.EventDateTime , $_.Message) -ForegroundColor Yellow  })
            try
            {                    
                [Azure.DataCenterMigration.Logger]::Info($ConstFunctionMigrate,$ConstImportWarningLog);

                if($QuietMode -eq $True.ToString())
                {
                    Write-Host $ConstImportWarningLog -ForegroundColor Yellow
                    Write-Host "$ConstImportInfo"  -ForegroundColor Yellow
                    $dcMigration.MigrateSubscription($argDictionary);
                }
                else
                {
                    Write-Host $ConstImportWarning -ForegroundColor Yellow -NoNewline
                    $choice = Read-Host 
                                   
                    while(1)
                    {
                        if($choice -eq $ConstYes)
                        {
                            Write-Host "$ConstImportInfo"  -ForegroundColor Yellow
                            #Migrate subscription metadata
                            $dcMigration.MigrateSubscription($argDictionary);
                            break;
                        }
                        if($choice -eq $ConstNo)
                        {                        
                            break;
                        }                        
                        $choice =  Read-Host $ConstSelectValid
                    }       
                }                   
            }
            # Catch Validation exception
            catch  [Azure.DataCenterMigration.ValidationException] {
                $message = $ConstValidationException +  $_.Exception.Message
                Write-ConsoleMessage $message $ConstError
                cd $script:currentWorkingDirectory
                break;
            }
        }
        # Catch exception
        catch  [Exception] {
            $message = $ConstException +  $_.Exception.Message
            Write-ConsoleMessage $message $ConstError
            cd $script:currentWorkingDirectory
            break;
        }  
    }    
    cd $script:currentWorkingDirectory
}

##############################################################################################################################
# Function Name : Configure-Logging = Reads log4net configuration file and configures logging mechanism.
# Parameters    : 1] Log4netConfigurationFilePath = Path of the configuration file of log4net.
#                 2] log4netDllPath = Log4net dll path.
# Returns       : None   
##############################################################################################################################
Function Configure-Logging()
{    
    Add-Type -Path $script:log4netDllPath    
    $LogManager = [log4net.LogManager]
    $logger = $LogManager::GetLogger(“PowerShell”);
    if ( (test-path $script:logConfigFilePath) -eq $False)
    {
        $message =  $ConstDLLNotFound + $script:logConfigFilePath
        Write-ConsoleMessage $message $ConstError
        write-host       
        break; 
    }
    else
    {
        $configFile = new-object System.IO.FileInfo( "$script:logConfigFilePath" );        
        $xmlConfigurator = [log4net.Config.XmlConfigurator]::ConfigureAndWatch($configFile);        
    }        
 }

##############################################################################################################################
# Function Name : Write-ConsoleMessage = Writes message on console
# Parameters    : 1] Message = Message to be printed
#                 2] Type = Message Type (Info/Error)
# Returns       : 
##############################################################################################################################
Function Write-ConsoleMessage([string]$Message,[string]$type = "Info")
{    
    #Print the message only if user has chosed QuietMode value to False 
    if($script:quietMode -eq $False.ToString())
    {
        if($type -match $ConstError)
        {
            Write-Host $Message -ForegroundColor Red
        }
        else
        {
            Write-Host $Message -ForegroundColor Green
        }            
    }
}

##############################################################################################################################
# Function Name : Setup-Environment = Sets up an environment for Azure.DataMigration solution run
# Parameters    : None
# Returns       : None   
##############################################################################################################################
Function Setup-Environment()
{    
   # get current console directory
    $script:currentWorkingDirectory = Get-Location    
    $moduleRoot = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\Modules\Azure.DataCenterMigration"
    & $moduleRoot\Constants.ps1

    $script:dllPath = "$moduleRoot\bin\Azure.DataCenterMigration.dll"
    $script:logConfigFilePath = "$moduleRoot\bin\Azure.DataCenterMigration.App.exe.config"
    $script:log4netDllPath = "$moduleRoot\bin\log4net.dll"    
 }

#############################################################################################################################

#Export only following functions to end user
Export-ModuleMember -function Export-AzureSubscriptionMetadata
Export-ModuleMember -function Import-AzureSubscriptionMetadata
Export-ModuleMember -function Migrate-AzureSubscription
