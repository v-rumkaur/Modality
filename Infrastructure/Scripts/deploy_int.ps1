$SubscriptionName                  = "SxGICon_OneChat_RD"
$ResourceGroupName                 = "modalityservice-rg-int-global"
$Location                          = "westus"
$Environment                       = "Pre-Production"
$Instance                          = "int"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$ResourcePrefix                    = "modalityservice"
$ResourceSuffix                    = "-int-wus"
$GlobalResourceSuffix              = "-int" # Global resources have no location in suffix.
$GlobalIdentitySuffix              = "-int-wus" # Needed to reference global MSI due to legacy naming convention mismatch
$AzureAdTenantId                   = "975f013f-7f24-47e8-a7d3-abc4752bf346"

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
    -AzureAdTenantId $AzureAdTenantId