// ============================================================================
// Role Assignments Module
// ============================================================================
// RBAC role assignments for managed identity authentication
// Enables passwordless access from Function App to other Azure services
// ============================================================================

@description('Principal ID of the Function App managed identity')
param functionAppPrincipalId string

@description('Name of the storage account')
param storageAccountName string

@description('Name of the Key Vault')
param keyVaultName string

@description('Name of the Speech Services resource')
param speechServiceName string

@description('Name of the AI Services account')
param aiServicesName string

// Deploying user principal ID (Hammad Aslam)
var deployingUserPrincipalId = '716e5244-7a36-4bef-9fe6-18f8b62f3cce'

// ============================================================================
// BUILT-IN ROLE DEFINITION IDs
// ============================================================================

// Storage roles
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'

// Key Vault roles
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// Cognitive Services roles
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'
var cognitiveServicesContributorRoleId = '25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68'
var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
var cognitiveServicesOpenAIContributorRoleId = 'a001fd3d-188f-4b5d-821b-7da978bf7442'

// ============================================================================
// EXISTING RESOURCES
// ============================================================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource speechServices 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: speechServiceName
}

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: aiServicesName
}

// ============================================================================
// STORAGE ACCOUNT ROLE ASSIGNMENTS
// ============================================================================

// Storage Blob Data Contributor - for reading/writing video files
resource storageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Queue Data Contributor - for Durable Functions task hub
resource storageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionAppPrincipalId, storageQueueDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Table Data Contributor - for Durable Functions state
resource storageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionAppPrincipalId, storageTableDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// KEY VAULT ROLE ASSIGNMENTS
// ============================================================================

// Key Vault Secrets User - for reading secrets
resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, functionAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// SPEECH SERVICES ROLE ASSIGNMENTS
// ============================================================================

// Cognitive Services User - for using Speech Services
resource speechServicesUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(speechServices.id, functionAppPrincipalId, cognitiveServicesUserRoleId)
  scope: speechServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// AI SERVICES ROLE ASSIGNMENTS
// ============================================================================

// Cognitive Services User - for using AI Services
resource aiServicesUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, functionAppPrincipalId, cognitiveServicesUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Cognitive Services Contributor - for managing AI Services resources
resource aiServicesContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, functionAppPrincipalId, cognitiveServicesContributorRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesContributorRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// OPENAI MODEL ACCESS ROLE ASSIGNMENTS (Multi-Agent Support)
// ============================================================================

// Cognitive Services OpenAI User - for Function App to access GPT-4o-mini
resource aiServicesOpenAIUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, functionAppPrincipalId, cognitiveServicesOpenAIUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Cognitive Services OpenAI Contributor - for Function App to manage deployments if needed
resource aiServicesOpenAIContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, functionAppPrincipalId, cognitiveServicesOpenAIContributorRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIContributorRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Cognitive Services OpenAI User - for deploying user to access GPT-4o-mini
resource userAiServicesOpenAIAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, deployingUserPrincipalId, cognitiveServicesOpenAIUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: deployingUserPrincipalId
    principalType: 'User'
  }
}

// ============================================================================
// DEPLOYING USER ROLE ASSIGNMENTS
// ============================================================================

// Cognitive Services User - for deploying user to access AI Services
resource userAiServicesAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, deployingUserPrincipalId, cognitiveServicesUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: deployingUserPrincipalId
    principalType: 'User'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('List of role assignments created')
output roleAssignments array = [
  'Storage Blob Data Contributor'
  'Storage Queue Data Contributor'
  'Storage Table Data Contributor'
  'Key Vault Secrets User'
  'Cognitive Services User (Speech)'
  'Cognitive Services User (AI Services)'
  'Cognitive Services Contributor (AI Services)'
  'Cognitive Services OpenAI User (AI Services - Function App)'
  'Cognitive Services OpenAI Contributor (AI Services - Function App)'
  'Cognitive Services User (AI Services - Deploying User)'
  'Cognitive Services OpenAI User (AI Services - Deploying User)'
]
