# ============================================================================
# Video Translation Service - Infrastructure Deployment Script
# ============================================================================
# This script deploys all Azure infrastructure for the video translation service
# Prerequisites: Azure CLI installed and logged in
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [int]$DeploymentNumber,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2",
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Variables
$SubscriptionName = "Your-Subscription-Name"  # Update this
$ResourceGroupName = "AMAFY26-deployment-$DeploymentNumber"
$TemplateFile = "$PSScriptRoot\main.bicep"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Video Translation Service - Infrastructure" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Deployment Number: $DeploymentNumber" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host ""

# Check Azure CLI login
Write-Host "Checking Azure CLI login status..." -ForegroundColor Gray
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Please login to Azure CLI first: az login" -ForegroundColor Red
    exit 1
}
Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "Subscription: $($account.name)" -ForegroundColor Green
Write-Host ""

# Validate the template
Write-Host "Validating Bicep template..." -ForegroundColor Gray
$validation = az deployment sub validate `
    --location $Location `
    --template-file $TemplateFile `
    --parameters deploymentNumber=$DeploymentNumber location=$Location `
    2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Template validation failed:" -ForegroundColor Red
    Write-Host $validation
    exit 1
}
Write-Host "Template validation successful!" -ForegroundColor Green
Write-Host ""

# What-If deployment
if ($WhatIf) {
    Write-Host "Running What-If analysis..." -ForegroundColor Gray
    az deployment sub what-if `
        --location $Location `
        --template-file $TemplateFile `
        --parameters deploymentNumber=$DeploymentNumber location=$Location
    exit 0
}

# Confirm deployment
$confirm = Read-Host "Do you want to proceed with the deployment? (y/n)"
if ($confirm -ne "y") {
    Write-Host "Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

# Deploy
Write-Host ""
Write-Host "Starting deployment..." -ForegroundColor Gray
$deploymentName = "VideoTranslation-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

$result = az deployment sub create `
    --name $deploymentName `
    --location $Location `
    --template-file $TemplateFile `
    --parameters deploymentNumber=$DeploymentNumber location=$Location `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0) {
    Write-Host "Deployment failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "Deployment Successful!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Outputs:" -ForegroundColor Yellow
Write-Host "  Resource Group: $($result.properties.outputs.resourceGroupName.value)"
Write-Host "  Function App: $($result.properties.outputs.functionAppName.value)"
Write-Host "  Function App URL: https://$($result.properties.outputs.functionAppHostname.value)"
Write-Host "  Static Web App: $($result.properties.outputs.staticWebAppName.value)"
Write-Host "  Static Web App URL: $($result.properties.outputs.staticWebAppUrl.value)"
Write-Host "  Speech Endpoint: $($result.properties.outputs.speechServiceEndpoint.value)"
Write-Host "  AI Foundry Endpoint: $($result.properties.outputs.aiFoundryEndpoint.value)"
Write-Host "  Key Vault URI: $($result.properties.outputs.keyVaultUri.value)"
Write-Host "  Storage Account: $($result.properties.outputs.storageAccountName.value)"
Write-Host ""

# Get Static Web App deployment token
Write-Host "Retrieving Static Web App deployment token..." -ForegroundColor Gray
$swaName = $result.properties.outputs.staticWebAppName.value
$swaToken = az staticwebapp secrets list --name $swaName --resource-group $ResourceGroupName --query "properties.apiKey" -o tsv

Write-Host ""
Write-Host "Static Web App Deployment Token:" -ForegroundColor Magenta
Write-Host $swaToken -ForegroundColor White
Write-Host ""
Write-Host "(Save this token - you'll need it to deploy the Blazor app)" -ForegroundColor Yellow
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Deploy the Function App code: func azure functionapp publish $($result.properties.outputs.functionAppName.value)"
Write-Host "  2. Deploy the Blazor app: swa deploy ./src/ui/bin/publish --deployment-token <token>"
Write-Host "  3. Test the video translation workflow"
