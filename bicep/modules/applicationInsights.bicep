// Application Insights Module
// Creates Application Insights for application monitoring and diagnostics

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Azure region')
param location string

@description('Log Analytics Workspace ID (optional)')
param workspaceId string = ''

@description('Retention period in days')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

@description('Tags to apply to resources')
param tags object = {}

var appInsightsName = '${appName}-${environment}-ai'

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: retentionInDays
    WorkspaceResourceId: !empty(workspaceId) ? workspaceId : null
    IngestionMode: !empty(workspaceId) ? 'LogAnalytics' : 'ApplicationInsights'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('The name of the Application Insights')
output applicationInsightsName string = applicationInsights.name

@description('The instrumentation key')
@secure()
output instrumentationKey string = applicationInsights.properties.InstrumentationKey

@description('The connection string')
@secure()
output connectionString string = applicationInsights.properties.ConnectionString

@description('The Application ID')
output applicationId string = applicationInsights.properties.AppId

@description('The Application Insights ID')
output applicationInsightsId string = applicationInsights.id
