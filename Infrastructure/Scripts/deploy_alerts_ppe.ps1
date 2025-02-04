param(
        [String]           $AccessToken
)
$SubscriptionName                  = "SxGICon_OneChat_RD"
$ResourceGroupName                 = "modalityservice-rg-managedidentity-ppe-global"
$Location                          = "westus"
$Environment                       = "modalityservice-rg-managedidentity-ppe-global"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$SubscriptionId                    = "bc970a64-0a20-44e7-a673-a1b9f20b0ca0"
$ResourcePrefix                    = "modalityservice"
$ActionGroupName                   = "SxG ICon Chat Team"
$ActionGroupShortName              = "sxgchatall"
$GlobalResourceSuffix              = "-ppe" 
$GlobalResourceGroupName           = "modalityservice-rg-managedidentity-ppe-global"
$OCCActionGroupResourceGroupName   = "occ-rg-common-ppe-wus"
$OCCActionGroupName                = "OCC-ServiceReliability-AG"
$OCCEscalationActionGroupName      = "OCC-ServiceReliability-AG"

.\manage_alerts.ps1 `
	-SubscriptionName $SubscriptionName `
	-ResourceGroupName $ResourceGroupName `
	-Location $Location `
	-Environment $Environment `
	-ComponentId $ComponentId `
	-SubscriptionId $SubscriptionId `
	-ResourcePrefix	$ResourcePrefix `
	-ActionGroupName $ActionGroupName `
	-ActionGroupShortName $ActionGroupShortName `
	-GlobalResourceSuffix $GlobalResourceSuffix `
	-GlobalResourceGroupName $GlobalResourceGroupName `
	-OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
	-OCCActionGroupName $OCCActionGroupName `
	-OCCEscalationActionGroupName $OCCEscalationActionGroupName `
	-AccessToken $AccessToken