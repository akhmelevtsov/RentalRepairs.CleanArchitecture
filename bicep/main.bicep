// Main Bicep Template for Rental Repairs Application
// This template orchestrates all Azure resources needed for the application

targetScope = 'subscription'

// ============================================================================
// PARAMETERS
// ============================================================================

@description('Name of the application (used in resource naming)')
@minLength(3)
@maxLength(20)
param appName string = 'rentalrepairs'

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('Azure region for all resources')
param location string = 'canadacentral'

@description('App Service Plan SKU')
@allowed([
  'F1'  // Free tier
  'B1'  // Basic tier
  'B2'  // Basic tier
  'S1'  // Standard tier
  'P1v2' // Premium v2 tier
])
param appServiceSku string = 'B1'

@description('SQL Server admin username')
param sqlAdminUsername string = 'sqladmin'

@description('SQL Server admin password')
@secure()
@minLength(12)
param sqlAdminPassword string

@description('SQL Database SKU name')
@allowed([
  'GP_S_Gen5'  // Serverless General Purpose
  'Basic'      // Basic tier
  'S0'         // Standard S0
  'S1'         // Standard S1
  'P1'         // Premium P1
])
param sqlDatabaseSku string = 'GP_S_Gen5'

@description('Auto-pause delay in minutes for serverless SQL (set to -1 to disable)')
param sqlAutoPauseDelay int = 60

@description('Minimum capacity (vCores) for serverless SQL')
param sqlMinCapacity int = 1

@description('Maximum capacity (vCores) for serverless SQL')
param sqlMaxCapacity int = 2

@description('Maximum database size in GB')
param sqlMaxSizeGb int = 2

@description('Azure AD admin login email (optional)')
param sqlAadAdminLogin string = ''

@description('Azure AD admin object ID (optional)')
param sqlAadAdminObjectId string = ''

@description('Allow Azure services to access SQL Server')
param allowAzureServices bool = true

@description('Allow local IP address to access SQL Server')
param allowLocalIp bool = false

@description('Local IP address to whitelist (optional)')
param localIpAddress string = ''

@description('Application Insights retention period in days')
@minValue(30)
@maxValue(730)
param appInsightsRetentionDays int = 30

@description('Object ID of the user/service principal for Key Vault admin access (leave empty to use current principal)')
param keyVaultAdminObjectId string = ''

@description('Additional tags to apply to all resources')
param tags object = {}

// ============================================================================
// VARIABLES
// ============================================================================

var commonTags = union(tags, {
  Environment: environment
  Application: appName
  ManagedBy: 'Bicep'
})

var resourceGroupName = '${appName}-${environment}-rg'

// ============================================================================
// RESOURCE GROUP
// ============================================================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: commonTags
}

// ============================================================================
// APPLICATION INSIGHTS (Deploy first, needed by Web App)
// ============================================================================

module applicationInsights 'modules/applicationInsights.bicep' = {
  scope: resourceGroup
  name: 'applicationInsights-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    retentionInDays: appInsightsRetentionDays
    tags: commonTags
  }
}

// ============================================================================
// APP SERVICE PLAN
// ============================================================================

module appServicePlan 'modules/appServicePlan.bicep' = {
  scope: resourceGroup
  name: 'appServicePlan-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    sku: appServiceSku
    tags: commonTags
  }
}

// ============================================================================
// SQL SERVER & DATABASE
// ============================================================================

module sqlServer 'modules/sqlServer.bicep' = {
  scope: resourceGroup
  name: 'sqlServer-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    sqlAdminUsername: sqlAdminUsername
    sqlAdminPassword: sqlAdminPassword
    sqlAadAdminLogin: sqlAadAdminLogin
    sqlAadAdminObjectId: sqlAadAdminObjectId
    allowAzureServices: allowAzureServices
    allowLocalIp: allowLocalIp
    localIpAddress: localIpAddress
    tags: commonTags
  }
}

module sqlDatabase 'modules/sqlDatabase.bicep' = {
  scope: resourceGroup
  name: 'sqlDatabase-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    sqlServerName: sqlServer.outputs.sqlServerName
    skuName: sqlDatabaseSku
    autoPauseDelayInMinutes: sqlAutoPauseDelay
    minCapacity: sqlMinCapacity
    maxCapacity: sqlMaxCapacity
    maxSizeGb: sqlMaxSizeGb
    tags: commonTags
  }
}

// ============================================================================
// KEY VAULT (Deploy before Web App to store secrets)
// ============================================================================

var sqlConnectionString = 'Server=tcp:${sqlServer.outputs.sqlServerFqdn},1433;Initial Catalog=${sqlDatabase.outputs.databaseName};Persist Security Info=False;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

module keyVault 'modules/keyVault.bicep' = {
  scope: resourceGroup
  name: 'keyVault-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    tenantId: tenant().tenantId
    adminObjectId: !empty(keyVaultAdminObjectId) ? keyVaultAdminObjectId : subscription().subscriptionId // Placeholder - update manually after deployment
    webAppPrincipalId: '' // Will be updated after Web App is created
    sqlConnectionString: sqlConnectionString
    sqlAdminPassword: sqlAdminPassword
    tags: commonTags
  }
}

// ============================================================================
// WEB APP
// ============================================================================

module webApp 'modules/webApp.bicep' = {
  scope: resourceGroup
  name: 'webApp-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    appServicePlanId: appServicePlan.outputs.appServicePlanId
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    applicationInsightsInstrumentationKey: applicationInsights.outputs.instrumentationKey
    keyVaultUri: keyVault.outputs.keyVaultUri
    tags: commonTags
  }
}

// ============================================================================
// UPDATE KEY VAULT ACCESS POLICY FOR WEB APP
// ============================================================================

// Note: This is a limitation of Bicep - we need to update Key Vault access policy
// after the Web App is created. In practice, you may need to run a separate
// deployment or use a deployment script to grant the Web App's managed identity
// access to Key Vault after the initial deployment.

// ============================================================================
// STORAGE ACCOUNT & CDN - REMOVED FOR COST OPTIMIZATION
// ============================================================================
// CDN and Storage Account removed to optimize costs for demo application
// These resources cost $35-51/month but only serve 7 KB of static files
// Static files are served directly from App Service using UseStaticFiles()
// External resources (Bootstrap, Font Awesome, etc.) already use public CDNs
//
// If you need to re-enable for production:
// 1. Uncomment the storageAccount and cdn modules below
// 2. Add back the enableStaticWebsite parameter (line 87)
// 3. Restore storage/CDN outputs in the outputs section
// ============================================================================

// module storageAccount 'modules/storageAccount.bicep' = {
//   scope: resourceGroup
//   name: 'storageAccount-deployment'
//   params: {
//     appName: appName
//     environment: environment
//     location: location
//     enableStaticWebsite: true
//     createStaticContainer: true
//     tags: commonTags
//   }
// }

// module cdn 'modules/cdn.bicep' = {
//   scope: resourceGroup
//   name: 'cdn-deployment'
//   params: {
//     appName: appName
//     environment: environment
//     storageAccountName: storageAccount.outputs.storageAccountName
//     storageBlobEndpoint: storageAccount.outputs.primaryBlobEndpoint
//     tags: commonTags
//   }
// }

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Summary of the deployment')
output deploymentSummary object = {
  resourceGroup: resourceGroupName
  location: location
  environment: environment
  webAppUrl: webApp.outputs.webAppUrl
  sqlServerFqdn: sqlServer.outputs.sqlServerFqdn
  databaseName: sqlDatabase.outputs.databaseName
  keyVaultName: keyVault.outputs.keyVaultName
}

// Application Outputs
@description('The URL of the deployed Web App')
output webAppUrl string = webApp.outputs.webAppUrl

@description('The name of the Web App')
output webAppName string = webApp.outputs.webAppName

@description('The principal ID of the Web App managed identity')
output webAppPrincipalId string = webApp.outputs.webAppPrincipalId

// Database Outputs
@description('The fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.outputs.sqlServerFqdn

@description('The name of the SQL Database')
output sqlDatabaseName string = sqlDatabase.outputs.databaseName

@description('The SQL admin username')
output sqlAdminUsername string = sqlAdminUsername

// Key Vault Outputs
@description('The name of the Key Vault')
output keyVaultName string = keyVault.outputs.keyVaultName

@description('The URI of the Key Vault')
output keyVaultUri string = keyVault.outputs.keyVaultUri

// Monitoring Outputs
@description('The Application Insights Instrumentation Key')
@secure()
output applicationInsightsInstrumentationKey string = applicationInsights.outputs.instrumentationKey

@description('The Application Insights Connection String')
@secure()
output applicationInsightsConnectionString string = applicationInsights.outputs.connectionString

@description('The Application Insights Application ID')
output applicationInsightsAppId string = applicationInsights.outputs.applicationId

// Storage & CDN Outputs - REMOVED (see line 236 for explanation)
// If you re-enable Storage Account and CDN, uncomment the outputs below:
// output storageAccountName string = storageAccount.outputs.storageAccountName
// output storageBlobEndpoint string = storageAccount.outputs.primaryBlobEndpoint
// output cdnEndpointUrl string = cdn.outputs.cdnEndpointUrl
// output cdnEndpointHostName string = cdn.outputs.cdnEndpointHostName

// Next Steps
@description('Next steps after deployment')
output nextSteps array = [
  '1. Grant Key Vault access to Web App managed identity (see deployment guide)'
  '2. Deploy your application code to the Web App'
  '3. Run database migrations'
  '4. Verify the application at ${webApp.outputs.webAppUrl}'
]
