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
        [String]         $GlobalIdentitySuffix,           # Suffix for global MSI. Needed due to legacy naming convention mismatch.
        [int]            $RegionIPSegment,                # Space to put IPs for resources in this region (ex. 10.{0}.0.0, where {0} is this segment).  Each region needs to be unique.
        [bool]           $UsePremiumSku,                   # Flag to determine if the app service plan should use premium SKU.
		[String]         $GlobalResourceLocation,          # Azure region of resources that are shared across all regions.
        [String]         $OCCActionGroupResourceGroupName,
        [String]         $OCCActionGroupName,
        [String]         $AccessToken
)

$TemplateName = $ResourcePrefix + $ResourceSuffix + "-deployment"

Write-Host "Setting up prerequisites!"
Install-PackageProvider -Name NuGet -Force -Confirm:$false
Write-Host "NuGet Package Provider Installed Successfully."

$SecureAccessToken = ConvertTo-SecureString $AccessToken -AsPlainText -Force
$CredentialObj = New-Object System.Management.Automation.PSCredential("AzureDevOps", $SecureAccessToken)
Write-Host "Credential object created."

$CentralFeedName = "ModalityCentralFeed"
$CentralFeedLocation = "https://pkgs.dev.azure.com/dynamicscrm/OneCRM/_packaging/CRM.ICon.OneChat/nuget/v2"
        
Write-Host "Started registering private central feed."
if (-not (Get-PSRepository -Name $CentralFeedName -ErrorAction SilentlyContinue)) {
    Register-PSRepository -Name $CentralFeedName -SourceLocation $CentralFeedLocation -InstallationPolicy Trusted -Credential $CredentialObj
Write-Output "Repository '$CentralFeedName' registered successfully."
} else {
    Write-Output "Repository '$CentralFeedName' is already registered."
}

$AzureRM = Get-InstalledModule -Name AzureRM -ErrorAction SilentlyContinue
if ($AzureRM) {
Write-Host "AzureRM module found. Uninstalling..."
Get-InstalledModule -Name AzureRM -AllVersions | Uninstall-Module -Force -ErrorAction SilentlyContinue
} else {
Write-Host "AzureRM module is not installed. Skipping uninstallation."
}

Write-Host "Installing Az modules."
Install-Module AzureAD -Force -Repository $CentralFeedName -Credential $CredentialObj
Install-Module Az.Resources -Force -Repository $CentralFeedName -Credential $CredentialObj
Install-Module Az.KeyVault -Force -Repository $CentralFeedName -Credential $CredentialObj
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
    -ResourceSuffix $ResourceSuffix `
	-Instance $Instance `
    -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
    -OCCActionGroupName $OCCActionGroupName

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
    -RegionIPSegment $RegionIPSegment `
	-GlobalResourceGroupName $GlobalResourceGroupName `
	-Instance $Instance `
    -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
    -OCCActionGroupName $OCCActionGroupName
$VirtualNetwork

Write-Host "Granting access to Key Vault from Virtual Network"
# Keyvault names should not have dashes and total character length should be less than 24 characters
$VaultName = "$ResourcePrefix-kv$GlobalResourceSuffix".Replace('-','');
$VaultName = $VaultName.Substring(0, [System.Math]::Min(24, $VaultName.Length));
Add-AzKeyVaultNetworkRule `
        -VaultName $VaultName `
        -ResourceGroupName $GlobalResourceGroupName `
        -VirtualNetworkResourceId $VirtualNetwork.Outputs["subnetResourceId"].Value

Write-Host "Setting up App Service Plan"
New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/app_service_plan.json" `
    -Location $Location `
    -Environment $Environment `
    -ComponentId $ComponentId `
    -ResourcePrefix $ResourcePrefix `
    -ResourceSuffix $ResourceSuffix `
    -UsePremiumSku $UsePremiumSku `
	-GlobalResourceGroupName $GlobalResourceGroupName `
	-Instance $Instance `
    -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
    -OCCActionGroupName $OCCActionGroupName

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
    -GlobalIdentitySuffix $GlobalIdentitySuffix `
    -Instance $Instance `
	-GlobalResourceLocation $GlobalResourceLocation `
    -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
    -OCCActionGroupName $OCCActionGroupName

Write-Host "Setting up Network Security Perimeter"
New-AzResourceGroupDeployment `
    -Name $TemplateName `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "../Templates/resources/network_security_perimeter.json" `
    -Location $Location `
    -ResourceSuffix $ResourceSuffix `
    -ResourcePrefix $ResourcePrefix `
    -GlobalResourceSuffix $GlobalResourceSuffix

Write-Host "Azure Infrastructure deployment completed!"