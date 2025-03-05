param(
        [String]           $AccessToken
)
$SubscriptionName                  = "SxGICon_OneChat_Prod"
$ResourceGroupName                 = "modalityservice-rg-prod-global"
$Location                          = "westus"
$Environment                       = "Production"
$Instance                          = "prod"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$ResourcePrefix                    = "modalityservice"
$ResourceSuffix                    = "-prod-wus"
$GlobalResourceSuffix              = "-prod" # Global resources have no location in suffix.
$GlobalIdentitySuffix              = "-prod-wus" # Needed to reference global MSI due to legacy naming convention mismatch
$AzureAdTenantId                   = "975f013f-7f24-47e8-a7d3-abc4752bf346"
$OCCActionGroupResourceGroupName   = "occ-rg-monitors-prod-wus"
$OCCActionGroupName                = "OCC-ServiceReliability-AG"
$SubscriptionId                    = "d9d18e84-40fe-4f31-ab9d-92231d96e8fd"

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