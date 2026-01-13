// ============================================================================
// Storage Account Module
// ============================================================================
// Azure Storage for Function App state and video file storage
// ============================================================================

@description('Name of the storage account (3-24 lowercase letters and numbers)')
@minLength(3)
@maxLength(24)
param name string

@description('Azure region for the resource')
param location string

@description('Tags to apply to the resource')
param tags object

@description('Storage account SKU')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
  'Standard_ZRS'
  'Premium_LRS'
])
param sku string = 'Standard_LRS'

@description('Storage account kind')
@allowed([
  'StorageV2'
  'BlobStorage'
  'Storage'
])
param kind string = 'StorageV2'

@description('Name of the Function App (for file share)')
param functionAppName string = ''

// ============================================================================
// RESOURCE
// ============================================================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  kind: kind
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Required for Azure Functions
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    encryption: {
      services: {
        blob: {
          enabled: true
          keyType: 'Account'
        }
        file: {
          enabled: true
          keyType: 'Account'
        }
        queue: {
          enabled: true
          keyType: 'Account'
        }
        table: {
          enabled: true
          keyType: 'Account'
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Blob Services configuration
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

// Container for video files
resource videoContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'videos'
  properties: {
    publicAccess: 'None'
  }
}

// Container for translated outputs
resource outputContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'outputs'
  properties: {
    publicAccess: 'None'
  }
}

// Container for subtitles
resource subtitlesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'subtitles'
  properties: {
    publicAccess: 'None'
  }
}

// File Services configuration (for Function App)
resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {}
}

// File share for Function App content
resource functionAppFileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = if (!empty(functionAppName)) {
  parent: fileServices
  name: toLower(functionAppName)
  properties: {
    shareQuota: 50 // 50 GB
    accessTier: 'TransactionOptimized'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Resource ID of the storage account')
output id string = storageAccount.id

@description('Name of the storage account')
output name string = storageAccount.name

@description('Primary endpoint for blob storage')
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('Primary endpoint for queue storage')
output queueEndpoint string = storageAccount.properties.primaryEndpoints.queue

@description('Primary endpoint for table storage')
output tableEndpoint string = storageAccount.properties.primaryEndpoints.table
