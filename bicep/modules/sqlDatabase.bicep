// SQL Database Module
// Creates the Azure SQL Database with support for Serverless or Provisioned tiers

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Azure region')
param location string

@description('SQL Server name')
param sqlServerName string

@description('SQL Database SKU name')
@allowed([
  'GP_S_Gen5'  // Serverless General Purpose
  'Basic'      // Basic tier
  'S0'         // Standard S0
  'S1'         // Standard S1
  'P1'         // Premium P1
])
param skuName string = 'GP_S_Gen5'

@description('Auto-pause delay in minutes for serverless (set to -1 to disable)')
param autoPauseDelayInMinutes int = 60

@description('Minimum capacity (vCores) for serverless')
param minCapacity int = 1

@description('Maximum capacity (vCores) for serverless')
param maxCapacity int = 2

@description('Maximum size in GB')
param maxSizeGb int = 2

@description('Tags to apply to resources')
param tags object = {}

var databaseName = '${appName}-${environment}-db'
var isServerless = skuName == 'GP_S_Gen5'

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  name: '${sqlServerName}/${databaseName}'
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: isServerless ? 'GeneralPurpose' : skuName == 'Basic' ? 'Basic' : startsWith(skuName, 'S') ? 'Standard' : 'Premium'
    capacity: isServerless ? maxCapacity : null
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: maxSizeGb * 1024 * 1024 * 1024
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
    // Serverless-specific properties
    autoPauseDelay: isServerless ? autoPauseDelayInMinutes : null
    minCapacity: isServerless ? minCapacity : null
  }
}

@description('The name of the SQL Database')
output databaseName string = databaseName

@description('The SQL Database ID')
output databaseId string = sqlDatabase.id

@description('The SKU of the database')
output databaseSku string = sqlDatabase.sku.name
