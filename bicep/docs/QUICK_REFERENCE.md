# Bicep Quick Reference

A quick reference guide for common Bicep deployment commands and operations.

## Table of Contents

- [Essential Commands](#essential-commands)
- [Deployment Commands](#deployment-commands)
- [Resource Management](#resource-management)
- [Monitoring & Diagnostics](#monitoring--diagnostics)
- [Database Operations](#database-operations)
- [Key Vault Operations](#key-vault-operations)
- [Storage Operations](#storage-operations)
- [Troubleshooting Commands](#troubleshooting-commands)

## Essential Commands

### Login and Subscription

```powershell
# Login to Azure
az login

# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription "SUBSCRIPTION_NAME_OR_ID"

# Show current subscription
az account show
```

### Quick Deploy

```powershell
# One-command deployment
az deployment sub create `
  --name "rentalrepairs-$(Get-Date -Format 'yyyyMMddHHmmss')" `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

## Deployment Commands

### Validate Template

```powershell
# Validate Bicep template
az deployment sub validate `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

### Preview Changes (What-If)

```powershell
# See what will change before deploying
az deployment sub what-if `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

### Deploy Template

```powershell
# Deploy with custom name
$deploymentName = "rentalrepairs-$(Get-Date -Format 'yyyyMMddHHmmss')"

az deployment sub create `
  --name $deploymentName `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

### Monitor Deployment

```powershell
# Check deployment status
az deployment sub show `
  --name $deploymentName `
  --query "properties.provisioningState"

# Get deployment outputs
az deployment sub show `
  --name $deploymentName `
  --query "properties.outputs"

# List all deployments
az deployment sub list --output table
```

### Override Parameters

```powershell
# Override specific parameters at deployment time
az deployment sub create `
  --name $deploymentName `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json `
  --parameters environment=staging appServiceSku=S1
```

## Resource Management

### List Resources

```powershell
# List all resources in resource group
az resource list `
  --resource-group rentalrepairs-dev-rg `
  --output table

# List resources of specific type
az resource list `
  --resource-group rentalrepairs-dev-rg `
  --resource-type "Microsoft.Web/sites" `
  --output table
```

### Get Resource Details

```powershell
# Get resource details
az resource show `
  --resource-group rentalrepairs-dev-rg `
  --name rentalrepairs-dev-app `
  --resource-type "Microsoft.Web/sites"
```

### Update Tags

```powershell
# Add or update tags
az resource tag `
  --resource-group rentalrepairs-dev-rg `
  --name rentalrepairs-dev-app `
  --resource-type "Microsoft.Web/sites" `
  --tags Environment=dev Owner=TeamA
```

### Delete Resources

```powershell
# Delete entire resource group (CAREFUL!)
az group delete `
  --name rentalrepairs-dev-rg `
  --yes `
  --no-wait

# Delete specific resource
az resource delete `
  --resource-group rentalrepairs-dev-rg `
  --name rentalrepairs-dev-app `
  --resource-type "Microsoft.Web/sites"
```

## Web App Operations

### View Web App

```powershell
# Get Web App details
az webapp show `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg

# Get default hostname
az webapp show `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --query "defaultHostName" `
  --output tsv
```

### Deploy Application

```powershell
# Deploy from zip file
az webapp deploy `
  --resource-group rentalrepairs-dev-rg `
  --name rentalrepairs-dev-app `
  --src-path ./app.zip `
  --type zip

# Deploy from GitHub
az webapp deployment source config `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --repo-url https://github.com/username/repo `
  --branch main `
  --manual-integration
```

### Restart Web App

```powershell
# Restart Web App
az webapp restart `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg
```

### App Settings

```powershell
# List app settings
az webapp config appsettings list `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --output table

# Set app setting
az webapp config appsettings set `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --settings "MySetting=MyValue"

# Delete app setting
az webapp config appsettings delete `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --setting-names "MySetting"
```

### Scale App Service

```powershell
# Scale up (change tier)
az appservice plan update `
  --name rentalrepairs-dev-asp `
  --resource-group rentalrepairs-dev-rg `
  --sku S1

# Scale out (increase instances)
az appservice plan update `
  --name rentalrepairs-dev-asp `
  --resource-group rentalrepairs-dev-rg `
  --number-of-workers 3
```

## Monitoring & Diagnostics

### Application Insights

```powershell
# Get Application Insights details
az monitor app-insights component show `
  --app rentalrepairs-dev-ai `
  --resource-group rentalrepairs-dev-rg

# Get instrumentation key
az monitor app-insights component show `
  --app rentalrepairs-dev-ai `
  --resource-group rentalrepairs-dev-rg `
  --query "instrumentationKey" `
  --output tsv
```

### Web App Logs

```powershell
# Enable logging
az webapp log config `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --application-logging filesystem `
  --level information

# Stream logs in real-time
az webapp log tail `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg

# Download logs
az webapp log download `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --log-file logs.zip
```

### Metrics

```powershell
# Get CPU metrics
az monitor metrics list `
  --resource rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --resource-type "Microsoft.Web/sites" `
  --metric "CpuPercentage" `
  --start-time 2024-01-01T00:00:00Z `
  --end-time 2024-01-02T00:00:00Z

# Get memory metrics
az monitor metrics list `
  --resource rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --resource-type "Microsoft.Web/sites" `
  --metric "MemoryPercentage"
```

## Database Operations

### SQL Server

```powershell
# List SQL servers
az sql server list `
  --resource-group rentalrepairs-dev-rg `
  --output table

# Show SQL server
az sql server show `
  --name rentalrepairs-dev-sql `
  --resource-group rentalrepairs-dev-rg
```

### SQL Database

```powershell
# List databases
az sql db list `
  --resource-group rentalrepairs-dev-rg `
  --server rentalrepairs-dev-sql `
  --output table

# Show database
az sql db show `
  --name rentalrepairs-dev-db `
  --resource-group rentalrepairs-dev-rg `
  --server rentalrepairs-dev-sql

# Get connection string
az sql db show-connection-string `
  --client ado.net `
  --name rentalrepairs-dev-db `
  --server rentalrepairs-dev-sql
```

### Firewall Rules

```powershell
# List firewall rules
az sql server firewall-rule list `
  --resource-group rentalrepairs-dev-rg `
  --server rentalrepairs-dev-sql `
  --output table

# Add firewall rule
az sql server firewall-rule create `
  --resource-group rentalrepairs-dev-rg `
  --server rentalrepairs-dev-sql `
  --name MyHomeIP `
  --start-ip-address 1.2.3.4 `
  --end-ip-address 1.2.3.4

# Delete firewall rule
az sql server firewall-rule delete `
  --resource-group rentalrepairs-dev-rg `
  --server rentalrepairs-dev-sql `
  --name MyHomeIP
```

### Database Backup

```powershell
# Export database to bacpac
az sql db export `
  --name rentalrepairs-dev-db `
  --server rentalrepairs-dev-sql `
  --resource-group rentalrepairs-dev-rg `
  --admin-user sqladmin `
  --admin-password "YOUR_PASSWORD" `
  --storage-key "STORAGE_KEY" `
  --storage-key-type StorageAccessKey `
  --storage-uri "https://storageaccount.blob.core.windows.net/backups/backup.bacpac"

# List backups
az sql db ltr-backup list `
  --location canadacentral `
  --server rentalrepairs-dev-sql `
  --database rentalrepairs-dev-db
```

## Key Vault Operations

### View Key Vault

```powershell
# Show Key Vault
az keyvault show `
  --name rentalrepairs-dev-kv `
  --resource-group rentalrepairs-dev-rg

# Get Key Vault URI
az keyvault show `
  --name rentalrepairs-dev-kv `
  --resource-group rentalrepairs-dev-rg `
  --query "properties.vaultUri" `
  --output tsv
```

### Secrets

```powershell
# List secrets
az keyvault secret list `
  --vault-name rentalrepairs-dev-kv `
  --output table

# Get secret value
az keyvault secret show `
  --vault-name rentalrepairs-dev-kv `
  --name SqlConnectionString `
  --query "value" `
  --output tsv

# Set secret
az keyvault secret set `
  --vault-name rentalrepairs-dev-kv `
  --name MySecret `
  --value "MySecretValue"

# Delete secret
az keyvault secret delete `
  --vault-name rentalrepairs-dev-kv `
  --name MySecret
```

### Access Policies

```powershell
# Show access policies
az keyvault show `
  --name rentalrepairs-dev-kv `
  --resource-group rentalrepairs-dev-rg `
  --query "properties.accessPolicies"

# Grant access to managed identity
az keyvault set-policy `
  --name rentalrepairs-dev-kv `
  --resource-group rentalrepairs-dev-rg `
  --object-id MANAGED_IDENTITY_PRINCIPAL_ID `
  --secret-permissions get list

# Remove access policy
az keyvault delete-policy `
  --name rentalrepairs-dev-kv `
  --resource-group rentalrepairs-dev-rg `
  --object-id OBJECT_ID
```

## Storage Operations

### Storage Account

```powershell
# List storage accounts
az storage account list `
  --resource-group rentalrepairs-dev-rg `
  --output table

# Get storage account keys
az storage account keys list `
  --resource-group rentalrepairs-dev-rg `
  --account-name rentalrepairsdevsa `
  --output table

# Get connection string
az storage account show-connection-string `
  --resource-group rentalrepairs-dev-rg `
  --name rentalrepairsdevsa `
  --output tsv
```

### Blob Containers

```powershell
# List containers
az storage container list `
  --account-name rentalrepairsdevsa `
  --output table

# Create container
az storage container create `
  --account-name rentalrepairsdevsa `
  --name mycontainer

# Upload file
az storage blob upload `
  --account-name rentalrepairsdevsa `
  --container-name static `
  --name myfile.txt `
  --file ./myfile.txt

# List blobs
az storage blob list `
  --account-name rentalrepairsdevsa `
  --container-name static `
  --output table
```

### CDN

```powershell
# Show CDN endpoint
az cdn endpoint show `
  --name rentalrepairs-dev-endpoint `
  --profile-name rentalrepairs-dev-cdn `
  --resource-group rentalrepairs-dev-rg

# Purge CDN cache
az cdn endpoint purge `
  --name rentalrepairs-dev-endpoint `
  --profile-name rentalrepairs-dev-cdn `
  --resource-group rentalrepairs-dev-rg `
  --content-paths '/*'
```

## Troubleshooting Commands

### Check Resource Health

```powershell
# Check Web App availability
az webapp show `
  --name rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --query "state"

# Test network connectivity
Test-NetConnection -ComputerName rentalrepairs-dev-sql.database.windows.net -Port 1433
```

### Get Error Details

```powershell
# View deployment errors
az deployment sub show `
  --name $deploymentName `
  --query "properties.error"

# View resource group deployments
az deployment group list `
  --resource-group rentalrepairs-dev-rg `
  --output table
```

### Diagnostic Settings

```powershell
# Enable diagnostic settings for Web App
az monitor diagnostic-settings create `
  --name DiagnosticSettings `
  --resource rentalrepairs-dev-app `
  --resource-type "Microsoft.Web/sites" `
  --resource-group rentalrepairs-dev-rg `
  --logs '[{"category": "AppServiceHTTPLogs", "enabled": true}]' `
  --metrics '[{"category": "AllMetrics", "enabled": true}]'
```

### View Activity Log

```powershell
# View activity log for resource group
az monitor activity-log list `
  --resource-group rentalrepairs-dev-rg `
  --start-time 2024-01-01T00:00:00Z `
  --output table
```

## Useful PowerShell Snippets

### Get Your Public IP

```powershell
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
Write-Host "Your public IP: $myIp"
```

### Generate Secure Password

```powershell
Add-Type -AssemblyName System.Web
$password = [System.Web.Security.Membership]::GeneratePassword(24, 8)
Write-Host "Generated password: $password"
```

### Save Deployment Outputs

```powershell
# Get and save all outputs to file
az deployment sub show `
  --name $deploymentName `
  --query "properties.outputs" | Out-File -FilePath deployment-outputs.json

# Parse outputs
$outputs = Get-Content deployment-outputs.json | ConvertFrom-Json
$webAppUrl = $outputs.webAppUrl.value
$sqlServer = $outputs.sqlServerFqdn.value
```

### Bulk Resource Operations

```powershell
# Stop all Web Apps in resource group
az webapp list `
  --resource-group rentalrepairs-dev-rg `
  --query "[].name" `
  --output tsv | ForEach-Object {
    az webapp stop --name $_ --resource-group rentalrepairs-dev-rg
}

# Start all Web Apps
az webapp list `
  --resource-group rentalrepairs-dev-rg `
  --query "[].name" `
  --output tsv | ForEach-Object {
    az webapp start --name $_ --resource-group rentalrepairs-dev-rg
}
```

## Environment Variables

Set common values as environment variables:

```powershell
# Set environment variables
$env:RESOURCE_GROUP = "rentalrepairs-dev-rg"
$env:WEB_APP_NAME = "rentalrepairs-dev-app"
$env:SQL_SERVER = "rentalrepairs-dev-sql"
$env:KEY_VAULT = "rentalrepairs-dev-kv"

# Use in commands
az webapp show --name $env:WEB_APP_NAME --resource-group $env:RESOURCE_GROUP
```

## Bicep CLI Commands

### Bicep Operations

```powershell
# Build Bicep to ARM JSON
az bicep build --file main.bicep

# Decompile ARM JSON to Bicep
az bicep decompile --file template.json

# Format Bicep file
az bicep format --file main.bicep

# Lint Bicep file
az bicep lint --file main.bicep

# List available Bicep versions
az bicep list-versions

# Upgrade Bicep
az bicep upgrade
```

## Additional Resources

- [Azure CLI Documentation](https://learn.microsoft.com/cli/azure/)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure Resource Manager](https://learn.microsoft.com/azure/azure-resource-manager/)

## Tips & Best Practices

1. **Always validate** before deploying: `az deployment sub validate`
2. **Use what-if** to preview changes: `az deployment sub what-if`
3. **Tag resources** for easy identification and cost tracking
4. **Use parameters files** for different environments
5. **Version control** your Bicep templates
6. **Test in dev** before deploying to production
7. **Monitor deployments** and check for errors
8. **Save deployment outputs** for reference
9. **Use managed identities** instead of passwords
10. **Enable diagnostic logging** for troubleshooting
