param(
        [String]           $AccessToken
)
$SubscriptionName                  = "SxGICon_OneChat_Prod"
$ResourceGroupName                 = "modalityservice-rg-prod-global"
$Location                          = "westus"
$Environment                       = "modalityservice-rg-prod-global"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$SubscriptionId                    = "d9d18e84-40fe-4f31-ab9d-92231d96e8fd"
$ResourcePrefix                    = "modalityservice"
$ActionGroupShortName              = "sxgchatall"
$GlobalResourceSuffix              = "-prod"
$GlobalResourceGroupName           = "modalityservice-rg-prod-global"
$OCCActionGroupResourceGroupName   = "occ-rg-monitors-prod-wus"
$OCCActionGroupName                = "OCC-ServiceReliability-AG"
$OCCEscalationActionGroupName      = "OCC-ServiceReliability-Escalation-AG"

.\manage_alerts.ps1 `
	-SubscriptionName $SubscriptionName `
	-ResourceGroupName $ResourceGroupName `
	-Location $Location `
	-Environment $Environment `
	-ComponentId $ComponentId `
	-SubscriptionId $SubscriptionId `
	-ResourcePrefix	$ResourcePrefix `
	-ActionGroupShortName $ActionGroupShortName `
	-GlobalResourceSuffix $GlobalResourceSuffix `
	-GlobalResourceGroupName $GlobalResourceGroupName `
	-OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
	-OCCActionGroupName $OCCActionGroupName `
	-OCCEscalationActionGroupName $OCCEscalationActionGroupName `
	-AccessToken $AccessToken