// Resource Group Module
// Creates the Azure Resource Group that will contain all resources

@description('Name of the application (used in resource naming)')
param appName string

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string

@description('Azure region for all resources')
param location string

@description('Tags to apply to all resources')
param tags object = {}

var resourceGroupName = '${appName}-${environment}-rg'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: union(tags, {
    Environment: environment
    Application: appName
    ManagedBy: 'Bicep'
  })
}

@description('The name of the created resource group')
output resourceGroupName string = resourceGroup.name

@description('The location of the resource group')
output location string = resourceGroup.location
