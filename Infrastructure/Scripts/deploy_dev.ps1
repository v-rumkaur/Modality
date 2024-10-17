$SubscriptionName                  = "SxGICon_OneChat_RD"
$ResourceGroupName                 = "modalityservice-rg-managedidentity-ppe-global"
$Location                          = "westus"
$Environment                       = "Pre-Production"
$Instance                          = "Development"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$ResourcePrefix                    = "modalityservice"
$ResourceSuffix                    = "-dev-wus"
$GlobalResourceSuffix              = "" # Intentially left blank, global resources have no suffix.
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
    -AzureAdTenantId $AzureAdTenantId