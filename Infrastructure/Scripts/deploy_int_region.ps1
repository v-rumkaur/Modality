param (
    [String]       $Location,                         # Azure region to deploy in.
    [String]       $ResourceSuffix,                   # Suffix for all resources used to indicate their Azure region.
    [int]          $RegionIPSegment                   # Space to put IPs for resources in this region (ex. 10.{0}.0.0, where {0} is this segment).  Each region needs to be unique.
)

$SubscriptionName                  = "SxGICon_OneChat_RD"
$ResourceGroupName                 = "modalityservice-rg${ResourceSuffix}"
$Environment                       = "Pre-Production"
$Instance                          = "int"
$ComponentId                       = "e982eb41-6f74-4863-a475-2f1ecf15a166"
$ResourcePrefix                    = "modalityservice"
$GlobalResourceGroupName           = "modalityservice-rg-int-global"
$GlobalResourceSuffix              = "-int" # Global resources have no location in suffix.
$GlobalIdentitySuffix              = "-int-wus" # Needed to reference global MSI due to legacy naming convention mismatch
$UsePremiumSku                     = $false
$GlobalResourceLocation            = "westus"

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
    -RegionIPSegment $RegionIPSegmentt `
	-GlobalResourceLocation $GlobalResourceLocation `
    -UsePremiumSku $UsePremiumSku