param(
        [String]   $SubscriptionName,
        [String]   $Location,
        [String]   $ResourceGroupName,
        [String]   $Environment,
        [String]   $SubscriptionId,
        [String]   $ResourcePrefix,
        [String]   $ActionGroupName,
        [String]   $GlobalResourceSuffix,
        [String]   $GlobalResourceGroupName
)

$TemplateName = $ResourcePrefix + "-deployment"

Write-Host "Setting up prerequisites"
Install-Module AzureAD -Force
Install-Module Az.Resources -Force
Set-AzContext -SubscriptionName $SubscriptionName
                   
Write-Host "Creating modality service alerts"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/alerts/modality_service_alerts.json" `
        -Location $Location `
        -ResourcePrefix $ResourcePrefix `
        -GlobalResourceSuffix $GlobalResourceSuffix `
        -APIEndpoints @('getAvailableModalities', 'getWidgetDetails') `
        -GlobalResourceGroupName $GlobalResourceGroupName	

Write-Host "Creating smart alerts"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/alerts/smart_detection_alerts.json" `
        -Environment $Environment `
        -SubscriptionId $SubscriptionId `
        -ActionGroupName $ActionGroupName `
        -Location $Location `
        -ResourcePrefix $ResourcePrefix `
        -GlobalResourceSuffix $GlobalResourceSuffix 
		
Write-Host "Creating activity alerts for subscription"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/alerts/activity_alerts.json" `
        -Environment $Environment `
        -SubscriptionId $SubscriptionId `
        -ActionGroupName $ActionGroupName 