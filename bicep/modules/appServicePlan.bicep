// App Service Plan Module
// Creates the compute resources for hosting the web application

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Azure region')
param location string

@description('App Service Plan SKU')
@allowed([
  'F1'  // Free tier
  'B1'  // Basic tier
  'B2'  // Basic tier
  'S1'  // Standard tier
  'P1v2' // Premium v2 tier
])
param sku string = 'B1'

@description('Tags to apply to resources')
param tags object = {}

var appServicePlanName = '${appName}-${environment}-asp'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku == 'F1' ? 'Free' : sku == 'B1' || sku == 'B2' ? 'Basic' : sku == 'S1' ? 'Standard' : 'PremiumV2'
  }
  kind: 'windows'
  properties: {
    reserved: false // false for Windows, true for Linux
  }
}

@description('The ID of the App Service Plan')
output appServicePlanId string = appServicePlan.id

@description('The name of the App Service Plan')
output appServicePlanName string = appServicePlan.name

@description('The SKU of the App Service Plan')
output appServicePlanSku string = appServicePlan.sku.name
