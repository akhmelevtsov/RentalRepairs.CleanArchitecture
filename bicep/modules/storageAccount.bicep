// Storage Account Module
// Creates Azure Storage Account for blob storage, logs, and static files

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Azure region')
param location string

@description('Enable static website hosting')
param enableStaticWebsite bool = true

@description('Create a container for static files')
param createStaticContainer bool = true

@description('Tags to apply to resources')
param tags object = {}

// Storage account names must be lowercase and without hyphens
var storageAccountName = toLower('${appName}${environment}sa')

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true // Required for CDN
    accessTier: 'Hot'
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Enable static website hosting
resource staticWebsite 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = if (enableStaticWebsite) {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: [
            '*'
          ]
          allowedMethods: [
            'GET'
            'HEAD'
            'OPTIONS'
          ]
          maxAgeInSeconds: 3600
          exposedHeaders: [
            '*'
          ]
          allowedHeaders: [
            '*'
          ]
        }
      ]
    }
  }
}

// Create container for static files
resource staticContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = if (createStaticContainer) {
  parent: staticWebsite
  name: 'static'
  properties: {
    publicAccess: 'Blob' // Allow public read access to blobs
  }
}

@description('The name of the Storage Account')
output storageAccountName string = storageAccount.name

@description('The ID of the Storage Account')
output storageAccountId string = storageAccount.id

@description('The primary blob endpoint')
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('The primary web endpoint (for static website)')
output primaryWebEndpoint string = storageAccount.properties.primaryEndpoints.web

@description('The name of the static container')
output staticContainerName string = createStaticContainer ? 'static' : ''
