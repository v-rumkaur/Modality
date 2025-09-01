param(
        [String]           $AccessToken
)
$SubscriptionName                  = "SxGICon_OneChat_RD"
$ResourceGroupName                 = "modalityservice-rg-managedidentity-ppe-global"
$Location                          = "westus"
$Environment                       = "Pre-Production"
$Instance                          = "ppe"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$ResourcePrefix                    = "modalityservice"
$ResourceSuffix                    = "-ppe-wus"
$GlobalResourceSuffix              = "-ppe" # Global resources have no location in suffix.
$GlobalIdentitySuffix              = "-ppe-wus" # Needed to reference global MSI due to legacy naming convention mismatch
$AzureAdTenantId                   = "975f013f-7f24-47e8-a7d3-abc4752bf346"
$OCCActionGroupResourceGroupName   = "modalityservice-rg-managedidentity-ppe-global"
$OCCActionGroupName                = "SxG ICon Chat Team"
$SubscriptionId                    = "bc970a64-0a20-44e7-a673-a1b9f20b0ca0"

.\deploy_azure_resources.ps1 `
    -SubscriptionName $SubscriptionName `
    -ResourceGroupName $ResourceGroupName `
    -Location $Location `
    -Environment $Environment `
    -Instance $Instance `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix `
    -GlobalResourceSuffix $GlobalResourceSuffix `
    -GlobalIdentitySuffix $GlobalIdentitySuffix `
    -AzureAdTenantId $AzureAdTenantId `
	-OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
	-OCCActionGroupName $OCCActionGroupName `
    -AccessToken $AccessToken `
    -SubscriptionId $SubscriptionId
