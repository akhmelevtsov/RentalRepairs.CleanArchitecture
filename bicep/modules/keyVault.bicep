// Key Vault Module
// Creates Azure Key Vault for secure storage of secrets

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Azure region')
param location string

@description('Tenant ID for Key Vault')
param tenantId string

@description('Object ID of the current user/service principal for admin access')
param adminObjectId string

@description('Principal ID of the Web App managed identity')
param webAppPrincipalId string = ''

@description('SQL Connection String to store as secret')
@secure()
param sqlConnectionString string

@description('SQL Admin Password to store as secret')
@secure()
param sqlAdminPassword string

@description('Tags to apply to resources')
param tags object = {}

var keyVaultName = '${appName}-${environment}-kv'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: false // Using access policies
    accessPolicies: [
      // Admin access policy
      {
        tenantId: tenantId
        objectId: adminObjectId
        permissions: {
          secrets: [
            'get'
            'list'
            'set'
            'delete'
            'recover'
            'backup'
            'restore'
          ]
        }
      }
    ]
  }
}

// Access policy for Web App managed identity (added separately if principal ID is provided)
resource keyVaultAccessPolicyWebApp 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = if (!empty(webAppPrincipalId)) {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: tenantId
        objectId: webAppPrincipalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

// Store SQL Connection String as secret
resource secretSqlConnectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'SqlConnectionString'
  properties: {
    value: sqlConnectionString
    contentType: 'text/plain'
  }
}

// Store SQL Admin Password as secret
resource secretSqlAdminPassword 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'SqlAdminPassword'
  properties: {
    value: sqlAdminPassword
    contentType: 'text/plain'
  }
}

@description('The name of the Key Vault')
output keyVaultName string = keyVault.name

@description('The URI of the Key Vault')
output keyVaultUri string = keyVault.properties.vaultUri

@description('The Key Vault ID')
output keyVaultId string = keyVault.id

@description('The SQL Connection String secret URI')
output sqlConnectionStringSecretUri string = secretSqlConnectionString.properties.secretUri
