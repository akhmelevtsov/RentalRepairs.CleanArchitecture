// CDN Module
// Creates Azure Front Door Standard Profile for content delivery

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Storage Account name for CDN origin')
param storageAccountName string

@description('Primary blob endpoint for CDN origin')
param storageBlobEndpoint string

@description('Tags to apply to resources')
param tags object = {}

var cdnProfileName = '${appName}-${environment}-afd'
var endpointName = '${appName}-${environment}-endpoint'
var originGroupName = 'storage-origin-group'
var originName = 'storage-origin'
var routeName = 'default-route'

// Extract hostname from blob endpoint (remove https:// and trailing /)
var originHostName = replace(replace(storageBlobEndpoint, 'https://', ''), '/', '')

// Azure Front Door Profile (Standard SKU)
resource cdnProfile 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: cdnProfileName
  location: 'Global'
  tags: tags
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
}

// Front Door Endpoint
resource endpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: cdnProfile
  name: endpointName
  location: 'Global'
  properties: {
    enabledState: 'Enabled'
  }
}

// Origin Group
resource originGroup 'Microsoft.Cdn/profiles/originGroups@2023-05-01' = {
  parent: cdnProfile
  name: originGroupName
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
    healthProbeSettings: {
      probePath: '/'
      probeRequestType: 'HEAD'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 100
    }
  }
}

// Origin (Storage Account)
resource origin 'Microsoft.Cdn/profiles/originGroups/origins@2023-05-01' = {
  parent: originGroup
  name: originName
  properties: {
    hostName: originHostName
    httpPort: 80
    httpsPort: 443
    originHostHeader: originHostName
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
  }
}

// Route (connects endpoint to origin group)
resource route 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = {
  parent: endpoint
  name: routeName
  properties: {
    originGroup: {
      id: originGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
  dependsOn: [
    origin
  ]
}

@description('The name of the Front Door Profile')
output cdnProfileName string = cdnProfile.name

@description('The name of the Front Door Endpoint')
output cdnEndpointName string = endpoint.name

@description('The Front Door Endpoint hostname')
output cdnEndpointHostName string = endpoint.properties.hostName

@description('The Front Door Endpoint URL')
output cdnEndpointUrl string = 'https://${endpoint.properties.hostName}'

@description('The Front Door Profile ID')
output cdnProfileId string = cdnProfile.id
