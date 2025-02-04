param (
    [String]       $Location,                         # Azure region to deploy in.
    [String]       $ResourceSuffix,                   # Suffix for all resources used to indicate their Azure region.
    [int]          $RegionIPSegment,
    [String]       $AccessToken                       # Space to put IPs for resources in this region (ex. 10.{0}.0.0, where {0} is this segment).  Each region needs to be unique.
)

$SubscriptionName                  = "SxGICon_OneChat_RD"
$ResourceGroupName                 = "modalityservice-rg${ResourceSuffix}"
$Environment                       = "Pre-Production"
$Instance                          = "ppe"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$ResourcePrefix                    = "modalityservice"
$GlobalResourceGroupName           = "modalityservice-rg-managedidentity-ppe-global"
$GlobalResourceSuffix              = "-ppe" # Global resources have no location in suffix.
$GlobalIdentitySuffix              = "-ppe-wus" # Needed to reference global MSI due to legacy naming convention mismatch
$GlobalResourceLocation            = "westus"
$UsePremiumSku                     = $true
$OCCActionGroupResourceGroupName   = "occ-rg-common-ppe-wus"
$OCCActionGroupName                = "OCC-ServiceReliability-AG"

.\deploy_azure_region_resources.ps1 `
    -SubscriptionName $SubscriptionName `
    -ResourceGroupName $ResourceGroupName `
    -Location $Location `
    -Environment $Environment `
    -Instance $Instance `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix `
    -GlobalResourceGroupName $GlobalResourceGroupName `
    -GlobalResourceSuffix $GlobalResourceSuffix `
    -GlobalIdentitySuffix $GlobalIdentitySuffix `
    -RegionIPSegment $RegionIPSegment `
	-GlobalResourceLocation $GlobalResourceLocation `
    -UsePremiumSku $UsePremiumSku `
	-OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
	-OCCActionGroupName $OCCActionGroupName `
    -AccessToken $AccessToken