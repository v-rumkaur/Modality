param(
        [String]   $SubscriptionName,
        [String]   $Location,
        [String]   $ResourceGroupName,
        [String]   $Environment,
        [String]   $SubscriptionId,
        [String]   $ResourcePrefix,
        [String]   $ActionGroupName,
        [String]   $GlobalResourceSuffix,
        [String]   $GlobalResourceGroupName,
        [String]   $OCCActionGroupResourceGroupName,
        [String]   $OCCActionGroupName,
        [String]   $OCCEscalationActionGroupName,
        [String]   $AccessToken
)

$TemplateName = $ResourcePrefix + "-deployment"

Write-Host "Setting up prerequisites"
Install-PackageProvider -Name NuGet -Force -Confirm:$false
Write-Host "NuGet Package Provider Installed Successfully."

$SecureAccessToken = ConvertTo-SecureString $AccessToken -AsPlainText -Force
$CredentialObj = New-Object System.Management.Automation.PSCredential("AzureDevOps", $SecureAccessToken)
Write-Host "Credential object created."

$CentralFeedName = "ModalityCentralFeed"
$CentralFeedLocation = "https://pkgs.dev.azure.com/dynamicscrm/OneCRM/_packaging/CRM.ICon.OneChat/nuget/v2"
        
Write-Host "Started registering private central feed."
if (-not (Get-PSRepository -Name $CentralFeedName -ErrorAction SilentlyContinue)) {
    Register-PSRepository -Name $CentralFeedName -SourceLocation $CentralFeedLocation -InstallationPolicy Trusted -Credential $CredentialObj
Write-Output "Repository '$CentralFeedName' registered successfully."
} else {
    Write-Output "Repository '$CentralFeedName' is already registered."
}


Get-InstalledModule -Name AzureRM -AllVersions | Uninstall-Module -Force -ErrorAction SilentlyContinue
Write-Host "Installing Az modules."
Install-Module AzureAD -Force -Repository $CentralFeedName -Credential $CredentialObj
Install-Module Az.Resources -Force -Repository $CentralFeedName -Credential $CredentialObj
Set-AzContext -SubscriptionName $SubscriptionName
                   
Write-Host "Creating modality service alerts"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/alerts/modality_service_alerts.json" `
        -Location $Location `
        -ResourcePrefix $ResourcePrefix `
        -GlobalResourceSuffix $GlobalResourceSuffix `
        -APIEndpoints @('getAvailableModalities', 'getWidgetDetails') `
        -GlobalResourceGroupName $GlobalResourceGroupName `
        -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
        -OCCActionGroupName $OCCActionGroupName `
        -OCCEscalationActionGroupName $OCCEscalationActionGroupName

Write-Host "Creating smart alerts"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/alerts/smart_detection_alerts.json" `
        -Environment $Environment `
        -SubscriptionId $SubscriptionId `
        -ActionGroupName $ActionGroupName `
        -Location $Location `
        -ResourcePrefix $ResourcePrefix `
        -GlobalResourceSuffix $GlobalResourceSuffix `
        -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
        -OCCActionGroupName $OCCActionGroupName
		
Write-Host "Creating activity alerts for subscription"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/alerts/activity_alerts.json" `
        -Environment $Environment `
        -SubscriptionId $SubscriptionId `
        -ActionGroupName $ActionGroupName `
        -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
        -OCCActionGroupName $OCCActionGroupName `
        -Location $Location  