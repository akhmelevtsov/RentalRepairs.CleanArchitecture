// SQL Server Module
// Creates the Azure SQL Server instance

@description('Name of the application')
param appName string

@description('Environment name')
param environment string

@description('Azure region')
param location string

@description('SQL Server admin username')
param sqlAdminUsername string

@description('SQL Server admin password')
@secure()
param sqlAdminPassword string

@description('Azure AD admin login email (optional)')
param sqlAadAdminLogin string = ''

@description('Azure AD admin object ID (optional)')
param sqlAadAdminObjectId string = ''

@description('Enable Azure services to access this server')
param allowAzureServices bool = true

@description('Allow local IP address (optional)')
param allowLocalIp bool = false

@description('Local IP address to whitelist (optional)')
param localIpAddress string = ''

@description('Tags to apply to resources')
param tags object = {}

var sqlServerName = '${appName}-${environment}-sql'

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminUsername
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Azure AD Administrator (optional)
resource sqlServerAadAdmin 'Microsoft.Sql/servers/administrators@2023-05-01-preview' = if (!empty(sqlAadAdminLogin) && !empty(sqlAadAdminObjectId)) {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: sqlAadAdminLogin
    sid: sqlAadAdminObjectId
    tenantId: subscription().tenantId
  }
}

// Firewall rule to allow Azure services
resource firewallRuleAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = if (allowAzureServices) {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Firewall rule for local IP (optional)
resource firewallRuleLocalIp 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = if (allowLocalIp && !empty(localIpAddress)) {
  parent: sqlServer
  name: 'AllowLocalIP'
  properties: {
    startIpAddress: localIpAddress
    endIpAddress: localIpAddress
  }
}

@description('The name of the SQL Server')
output sqlServerName string = sqlServer.name

@description('The fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The SQL Server ID')
output sqlServerId string = sqlServer.id
