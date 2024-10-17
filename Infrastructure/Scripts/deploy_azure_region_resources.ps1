param(
        [String]         $SubscriptionName,               # Subscription to deploy in.
        [String]         $ResourceGroupName,              # Name of the resource group to deploy in.
        [String]         $Location,                       # Azure region to deploy in.
        [String]         $Environment,                    # Name of the logical environment (Pre-Production, Production).
        [String]         $Instance,                       # Helps determine which instance of the modality service to target.
        [String]         $ComponentId,                    # Id of the component in ServiceTree.
        [String]         $ResourcePrefix,                 # Prefix of all resources used to indicate their environment.
        [String]         $ResourceSuffix,                 # Suffix of all resources used to indicate their Azure region.
        [String]         $GlobalResourceGroupName,        # Name of the resource group where global resources are deployed.
        [String]         $GlobalResourceSuffix,           # Suffix of resources that are shared across all regions.
        [String]         $GlobalIdentitySuffix,           # Suffix global MSI. Needed due to legacy naming convention mismatch.
        [int]            $RegionIPSegment                 # Space to put IPs for resources in this region (ex. 10.{0}.0.0, where {0} is this segment).  Each region needs to be unique.
)

$TemplateName = $ResourcePrefix + $ResourceSuffix + "-deployment"

Write-Host "Setting up prerequisites!"
Install-Module AzureAD -Force
Install-Module Az.Resources -Force
Set-AzContext -SubscriptionName $SubscriptionName

Write-Host "Azure Infrastructure deployment started!"

Write-Host "Setting up Network Security Group"
New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/network_security_group.json" `
    -Location $Location `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix

Write-Host "Setting up Virtual Network"
$VirtualNetwork = New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/virtual_network.json" `
    -Location $Location `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix `
    -RegionIPSegment $RegionIPSegment
$VirtualNetwork

Write-Host "Setting up App Service Plan"
New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/app_service_plan.json" `
    -Location $Location `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix

Write-Host "Setting up Storage Account"
$StorageAccount = New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/storage_account.json" `
    -Location $Location `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix
$StorageAccount

Write-Host "Setting up Modality App Service"
New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/modality_app_service.json" `
    -Location $Location `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix `
    -GlobalResourceGroupName $GlobalResourceGroupName `
    -GlobalResourceSuffix $GlobalResourceSuffix `
    -GlobalIdentitySuffix $GlobalIdentitySuffix

Write-Host "Azure Infrastructure deployment completed!"