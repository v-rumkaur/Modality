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
$ActionGroupShortName              = "sxgchatall"
$GlobalResourceSuffix              = "-ppe" 
$GlobalResourceGroupName           = "modalityservice-rg-managedidentity-ppe-global"
$OCCActionGroupResourceGroupName   = "modalityservice-rg-managedidentity-ppe-global"
$OCCActionGroupName                = "SxG ICon Chat Team"
$OCCEscalationActionGroupName      = "SxG ICon Chat Team"
$Instance						   = "ppe"

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
	-Instance $Instance `
	-AccessToken $AccessToken