param(
        [String]   $SubscriptionName,
        [String]   $Location,
        [String]   $ResourceGroupName,
        [String]   $Environment,
        [String]   $SubscriptionId,
        [String]   $ResourcePrefix,
        [String]   $ActionGroupName,
        [String]   $GlobalResourceSuffix,
        [String]   $GlobalResourceGroupName,
        [String]   $OCCActionGroupResourceGroupName,
        [String]   $OCCActionGroupName,
        [String]   $OCCEscalationActionGroupName
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
        -GlobalResourceGroupName $GlobalResourceGroupName `
        -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
        -OCCActionGroupName $OCCActionGroupName `
        -OCCEscalationActionGroupName $OCCEscalationActionGroupName

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
        -GlobalResourceSuffix $GlobalResourceSuffix `
        -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
        -OCCActionGroupName $OCCActionGroupName
		
Write-Host "Creating activity alerts for subscription"
New-AzResourceGroupDeployment `
        -Name $TemplateName `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "../Templates/alerts/activity_alerts.json" `
        -Environment $Environment `
        -SubscriptionId $SubscriptionId `
        -ActionGroupName $ActionGroupName `
        -OCCActionGroupResourceGroupName $OCCActionGroupResourceGroupName `
        -OCCActionGroupName $OCCActionGroupName `
        -Location $Location  