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
        [String]           $AzureAdTenantId                     # Tenant of the service identity used by services. This is the id of the home tenant.
)

$TemplateName = $ResourcePrefix + $ResourceSuffix + "-deployment"
$ModalityRegionSuffixes = @( "${GlobalResourceSuffix}-wus", "${GlobalResourceSuffix}-eus", "${GlobalResourceSuffix}-weu", "${GlobalResourceSuffix}-neu", "${GlobalResourceSuffix}-sea" ) # Regions where Modality service is deployed

Write-Host "Setting up prerequisites!"
Install-Module AzureAD -Force
Install-Module Az.Resources -Force
Set-AzContext -SubscriptionName $SubscriptionName

Write-Host "Azure Infrastructure deployment started!"

# Deploy resources that are shared across regions
Write-Host "Deploying Global Resources"

Write-Host "Setting up Modality App Identity"
$ManagedIdentityModality = New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/resources/managed_identity.json" `
        -Location $Location `
        -Environment $Environment `
        -ComponentId $ComponentId `
        -ResourcePrefix $ResourcePrefix `
        -ResourceSuffix $GlobalResourceSuffix
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
    -AzureAdTenantId $AzureAdTenantId

$WafCustomRulesFilePath = ""
switch ($Instance) {
    "Development" {
        $WafCustomRulesFilePath = "../Templates/resources/waf_rules_dev.json";
        break
    }
    "Staging" {
        $WafCustomRulesFilePath = "../Templates/resources/waf_rules_ppe.json";
        break
    }
    "Production" {
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
    -ModalityRegionSuffixes $ModalityRegionSuffixes

Write-Host "Azure Infrastructure deployment completed!"