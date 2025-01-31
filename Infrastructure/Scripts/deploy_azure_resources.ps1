param(
        [String]           $SubscriptionName,                   # Subscription to deploy in.
        [String]           $ResourceGroupName,                  # Name of the resource group to deploy in.
        [String]           $Location,                           # Azure region to deploy in.
        [String]           $Environment,                        # Name of the logical environment (Pre-Production, Production).
        [String]           $Instance,                           # Name of the specific environment instance (Development, Staging, Production).
        [String]           $ComponentId,                        # Id of the component in ServiceTree.
        [String]           $ResourcePrefix,                     # Prefix of all resources used to indicate their environment.
        [String]           $ResourceSuffix,                     # Suffix of all resources used to indicate their Azure region.
        [String]           $GlobalResourceSuffix,               # Suffix of resources that are shared across all regions.
        [String]           $GlobalIdentitySuffix,               # Suffix for global MSI. Needed due to legacy naming convention mismatch.
        [String]           $AzureAdTenantId,                     # Tenant of the service identity used by services. This is the id of the home tenant.
        [String]           $OCCActionGroupResourceGroupName,
        [String]           $OCCActionGroupName
)

$TemplateName = $ResourcePrefix + $ResourceSuffix + "-deployment"
$ModalityRegionSuffixes = @( "${GlobalResourceSuffix}-wus", "${GlobalResourceSuffix}-eus", "${GlobalResourceSuffix}-weu", "${GlobalResourceSuffix}-neu", "${GlobalResourceSuffix}-sea" ) # Regions where Modality service is deployed

Write-Host "Setting up prerequisites!"
Install-PackageProvider -Name NuGet -Force -Confirm:$false
Write-Host "NuGet Package Provider Installed Successfully."

$AccessToken = "$env:SYSTEM_ACCESSTOKEN"
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

Write-Host "Azure Infrastructure deployment started!"

# Deploy resources that are shared across regions
Write-Host "Deploying Global Resources"

# Write-Host "Setting up shared alert Action Group"
# New-AzResourceGroupDeployment `
#     -Name $TemplateName `
#    -ResourceGroupName $ResourceGroupName `
#    -TemplateFile "../Templates/resources/action_groups.json"

Write-Host "Setting up Modality App Identity"
$ManagedIdentityModality = New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/resources/managed_identity.json" `
        -Location $Location `
        -Environment $Environment `
        -ComponentId $ComponentId `
        -ResourcePrefix $ResourcePrefix `
        -ResourceSuffix $GlobalIdentitySuffix
$ManagedIdentityModality

Write-Host "Setting up shared Log Analytics Workspace"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/resources/log_analytics_workspace.json" `
        -Location $Location `
        -Environment $Environment `
        -ComponentId $ComponentId `
        -ResourcePrefix $ResourcePrefix `
        -ResourceSuffix $GlobalResourceSuffix

Write-Host "Setting up shared Application Insights"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/resources/application_insight.json" `
        -Location $Location `
        -Environment $Environment `
        -ComponentId $ComponentId `
        -ResourcePrefix $ResourcePrefix `
        -ResourceSuffix $GlobalResourceSuffix

Write-Host "Setting up shared KeyVault"
New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/key_vault.json" `
    -Location $Location `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $GlobalResourceSuffix `
    -GlobalResourceSuffix $GlobalResourceSuffix `
    -GlobalIdentitySuffix $GlobalIdentitySuffix `
    -AzureAdTenantId $AzureAdTenantId `
    -Instance $Instance `
    -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
    -OCCActionGroupName $OCCActionGroupName

$WafCustomRulesFilePath = ""
switch ($Instance) {
    "dev" {
        $WafCustomRulesFilePath = "../Templates/resources/waf_rules_dev.json";
        break
    }
    "int" {
        $WafCustomRulesFilePath = "../Templates/resources/waf_rules_int.json";
        break
    }
    "ppe" {
        $WafCustomRulesFilePath = "../Templates/resources/waf_rules_ppe.json";
        break
    }
    "prod" {
        $WafCustomRulesFilePath = "../Templates/resources/waf_rules_prod.json";
        break
    }
}
Write-Host "Setting up Front Door"
New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/front_door.json" `
    -TemplateParameterFile $WafCustomRulesFilePath `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $GlobalResourceSuffix `
    -ModalityRegionSuffixes $ModalityRegionSuffixes `
    -Instance $Instance `
    -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
    -OCCActionGroupName $OCCActionGroupName

Write-Host "Azure Infrastructure deployment completed!"