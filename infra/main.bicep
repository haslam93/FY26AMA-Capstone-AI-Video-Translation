// ============================================================================
// Video Translation Service - Main Infrastructure Template
// ============================================================================
// This template deploys all Azure resources for the video translation service
// using Azure Durable Functions, Speech API, and AI Foundry.
//
// Naming Convention: {service}-AMA-{deploymentNumber}
// Resource Group: AMAFY26-deployment-{deploymentNumber}
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// PARAMETERS
// ============================================================================

@description('Deployment number for resource naming (1-999)')
@minValue(1)
@maxValue(999)
param deploymentNumber int

@description('Azure region for all resources')
param location string = 'eastus2'

@description('Tags to apply to all resources')
param tags object = {
  project: 'VideoTranslation'
  environment: 'AMA-Capstone'
  deploymentNumber: string(deploymentNumber)
}

// ============================================================================
// VARIABLES - Resource Naming
// ============================================================================

var resourceGroupName = 'AMAFY26-deployment-${deploymentNumber}'
var logAnalyticsName = 'LogAnalytics-AMA-${deploymentNumber}'
var appInsightsName = 'AppInsights-AMA-${deploymentNumber}'
var keyVaultName = 'KeyVault-AMA-${deploymentNumber}'
var storageAccountName = 'storageama${deploymentNumber}'
var speechServiceName = 'Speech-AMA-${deploymentNumber}'
var aiServicesName = 'AIServices-AMA-${deploymentNumber}'
var functionAppName = 'FuncApp-AMA-${deploymentNumber}'
var appServicePlanName = 'ASP-AMA-${deploymentNumber}'
var staticWebAppName = 'SWA-AMA-${deploymentNumber}'

// ============================================================================
// RESOURCE GROUP
// ============================================================================

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ============================================================================
// MONITORING RESOURCES
// ============================================================================

module logAnalytics 'modules/log-analytics.bicep' = {
  scope: rg
  name: 'deploy-log-analytics'
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

module appInsights 'modules/app-insights.bicep' = {
  scope: rg
  name: 'deploy-app-insights'
  params: {
    name: appInsightsName
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// ============================================================================
// STORAGE & SECURITY RESOURCES
// ============================================================================

module storageAccount 'modules/storage-account.bicep' = {
  scope: rg
  name: 'deploy-storage-account'
  params: {
    name: storageAccountName
    location: location
    tags: tags
    functionAppName: functionAppName
  }
}

module keyVault 'modules/keyvault.bicep' = {
  scope: rg
  name: 'deploy-keyvault'
  params: {
    name: keyVaultName
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// ============================================================================
// AI & COGNITIVE SERVICES
// ============================================================================

module speechServices 'modules/speech-services.bicep' = {
  scope: rg
  name: 'deploy-speech-services'
  params: {
    name: speechServiceName
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

module aiFoundry 'modules/ai-foundry.bicep' = {
  scope: rg
  name: 'deploy-ai-foundry'
  params: {
    accountName: aiServicesName
    location: location
    tags: tags
  }
}

// ============================================================================
// COMPUTE RESOURCES
// ============================================================================

module staticWebApp 'modules/static-web-app.bicep' = {
  scope: rg
  name: 'deploy-static-web-app'
  params: {
    name: staticWebAppName
    location: location
    tags: tags
    sku: 'Free'
  }
}

module functionApp 'modules/function-app.bicep' = {
  scope: rg
  name: 'deploy-function-app'
  params: {
    functionAppName: functionAppName
    appServicePlanName: appServicePlanName
    location: location
    tags: tags
    storageAccountName: storageAccount.outputs.name
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    appInsightsConnectionString: appInsights.outputs.connectionString
    corsAllowedOrigins: [
      'https://portal.azure.com'
      'https://${staticWebApp.outputs.defaultHostname}'
    ]
  }
}

// ============================================================================
// ROLE ASSIGNMENTS (Managed Identity RBAC)
// ============================================================================

module roleAssignments 'modules/role-assignments.bicep' = {
  scope: rg
  name: 'deploy-role-assignments'
  params: {
    functionAppPrincipalId: functionApp.outputs.principalId
    storageAccountName: storageAccount.outputs.name
    keyVaultName: keyVault.outputs.name
    speechServiceName: speechServices.outputs.name
    aiServicesName: aiFoundry.outputs.accountName
    foundryProjectPrincipalId: aiFoundry.outputs.projectPrincipalId
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output resourceGroupName string = rg.name
output functionAppName string = functionApp.outputs.name
output functionAppHostname string = functionApp.outputs.defaultHostname
output speechServiceEndpoint string = speechServices.outputs.endpoint
output aiFoundryEndpoint string = aiFoundry.outputs.endpoint
output aiFoundryProjectName string = aiFoundry.outputs.projectName
output aiFoundryProjectEndpoint string = aiFoundry.outputs.projectEndpoint
output aiFoundryGpt4oMiniDeployment string = aiFoundry.outputs.gpt4oMiniDeploymentName
output keyVaultUri string = keyVault.outputs.uri
output storageAccountName string = storageAccount.outputs.name
output appInsightsName string = appInsights.outputs.name
output logAnalyticsWorkspaceId string = logAnalytics.outputs.id
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppUrl string = staticWebApp.outputs.url
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
