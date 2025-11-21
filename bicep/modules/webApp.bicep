// Web App Module
// Creates the Windows Web App for hosting the .NET 8 Razor Pages application

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Azure region')
param location string

@description('App Service Plan ID')
param appServicePlanId string

@description('Application Insights Connection String')
@secure()
param applicationInsightsConnectionString string

@description('Application Insights Instrumentation Key')
@secure()
param applicationInsightsInstrumentationKey string

@description('Key Vault URI for secret references')
param keyVaultUri string

@description('Tags to apply to resources')
param tags object = {}

var webAppName = '${appName}-${environment}-app'

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  tags: tags
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      alwaysOn: false // Set to true for production with non-Free tier
      http20Enabled: true
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      // Enable Application Logging to filesystem
      detailedErrorLoggingEnabled: true
      httpLoggingEnabled: true
      requestTracingEnabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Development' // Set to Development for demo environment with demo logins
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsightsInstrumentationKey
        }
        {
          name: 'Logging__LogLevel__Default'
          value: 'Information'
        }
        {
          name: 'Logging__LogLevel__Microsoft.AspNetCore'
          value: 'Warning'
        }
        {
          name: 'Infrastructure__Performance__EnableCaching'
          value: 'true'
        }
        {
          name: 'Infrastructure__Email__Provider'
          value: 'Mock'
        }
        {
          name: 'DemoAuthentication__EnableDemoMode'
          value: 'true'
        }
        {
          name: 'Seeding__EnableSeeding'
          value: 'true'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/SqlConnectionString/)'
        }
      ]
    }
  }
}

// Configure application logs settings
resource webAppLogs 'Microsoft.Web/sites/config@2023-01-01' = {
  name: 'logs'
  parent: webApp
  properties: {
    applicationLogs: {
      fileSystem: {
        level: 'Information' // Options: Off, Error, Warning, Information, Verbose
      }
    }
    httpLogs: {
      fileSystem: {
        enabled: true
        retentionInMb: 35
        retentionInDays: 7
      }
    }
    detailedErrorMessages: {
      enabled: true
    }
    failedRequestsTracing: {
      enabled: true
    }
  }
}

@description('The name of the Web App')
output webAppName string = webApp.name

@description('The default hostname of the Web App')
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'

@description('The principal ID of the Web App managed identity')
output webAppPrincipalId string = webApp.identity.principalId

@description('The Web App ID')
output webAppId string = webApp.id
