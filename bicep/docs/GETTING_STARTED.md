# Getting Started with Bicep Deployment

This quick start guide will help you deploy the Rental Repairs infrastructure to Azure in under 30 minutes.

## Prerequisites Checklist

Before you begin, ensure you have:

- [ ] Active Azure subscription
- [ ] Azure CLI installed (version 2.50.0+)
- [ ] Bicep CLI installed (comes with Azure CLI)
- [ ] PowerShell 7+ or Bash
- [ ] Contributor or Owner role on subscription
- [ ] Text editor (VS Code recommended)

## Quick Start Steps

### Step 1: Install Azure CLI

**Windows:**
```powershell
winget install Microsoft.AzureCLI
```

**macOS:**
```bash
brew install azure-cli
```

**Linux:**
```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

Verify installation:
```powershell
az --version
az bicep version
```

### Step 2: Login to Azure

```powershell
# Login interactively
az login

# If you have multiple subscriptions, list them
az account list --output table

# Set the subscription you want to use
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

### Step 3: Navigate to Bicep Directory

```powershell
cd C:\Users\akhme\source\repos\akhmelevtsov\RentalRepairsModernized\src\bicep
```

### Step 4: Create Parameters File

```powershell
# Copy the example file
Copy-Item parameters.example.json parameters.json

# Open in your default editor
notepad parameters.json
# Or use VS Code
code parameters.json
```

### Step 5: Configure Key Parameters

Edit `parameters.json` and update these required fields:

```json
{
  "parameters": {
    "appName": {
      "value": "rentalrepairs"
    },
    "environment": {
      "value": "dev"
    },
    "sqlAdminPassword": {
      "value": "CHANGE_THIS_PASSWORD"
    }
  }
}
```

**Generate a secure password:**
```powershell
# PowerShell
Add-Type -AssemblyName System.Web
[System.Web.Security.Membership]::GeneratePassword(24, 8)
```

### Step 6: Validate Template

```powershell
az deployment sub validate `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

If validation succeeds, you'll see: `"provisioningState": "Succeeded"`

### Step 7: Preview Changes (Optional but Recommended)

```powershell
az deployment sub what-if `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

Review the resources that will be created.

### Step 8: Deploy Infrastructure

```powershell
$deploymentName = "rentalrepairs-$(Get-Date -Format 'yyyyMMddHHmmss')"

az deployment sub create `
  --name $deploymentName `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

This will take 5-10 minutes. You'll see progress updates in the terminal.

### Step 9: Get Deployment Outputs

```powershell
# Get all outputs
az deployment sub show `
  --name $deploymentName `
  --query "properties.outputs"

# Save Web App URL
$webAppUrl = (az deployment sub show `
  --name $deploymentName `
  --query "properties.outputs.webAppUrl.value" `
  --output tsv)

Write-Host "Your Web App URL: $webAppUrl"
```

### Step 10: Grant Key Vault Access to Web App

```powershell
# Get necessary values
$outputs = az deployment sub show --name $deploymentName --query "properties.outputs" | ConvertFrom-Json

$webAppPrincipalId = $outputs.webAppPrincipalId.value
$keyVaultName = $outputs.keyVaultName.value
$resourceGroup = $outputs.deploymentSummary.value.resourceGroup

# Grant access
az keyvault set-policy `
  --name $keyVaultName `
  --resource-group $resourceGroup `
  --object-id $webAppPrincipalId `
  --secret-permissions get list

Write-Host "Key Vault access granted!"
```

### Step 11: Deploy Application Code

**Option A: Using Visual Studio**
1. Open the solution in Visual Studio
2. Right-click `RentalRepairs.Web` project
3. Select "Publish"
4. Choose "Azure" → "Azure App Service (Windows)"
5. Sign in and select your Web App
6. Click "Publish"

**Option B: Using Azure CLI**
```powershell
# Navigate to web project and publish
cd ..\RentalRepairs.Web
dotnet publish -c Release -o ./publish

# Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath ./app.zip -Force

# Get Web App name
$webAppName = $outputs.webAppName.value

# Deploy
az webapp deploy `
  --resource-group $resourceGroup `
  --name $webAppName `
  --src-path ./app.zip `
  --type zip

Write-Host "Application deployed!"
```

### Step 12: Run Database Migrations

```powershell
# Get connection string
$connectionString = az keyvault secret show `
  --vault-name $keyVaultName `
  --name SqlConnectionString `
  --query "value" `
  --output tsv

# Navigate to Infrastructure project
cd ..\RentalRepairs.Infrastructure

# Apply migrations
dotnet ef database update `
  --connection "$connectionString" `
  --startup-project ..\RentalRepairs.Web\RentalRepairs.Web.csproj

Write-Host "Database migrations completed!"
```

### Step 13: Verify Deployment

```powershell
# Open the Web App in browser
Start-Process $webAppUrl

# Check Web App status
az webapp show `
  --name $webAppName `
  --resource-group $resourceGroup `
  --query "state"
```

Visit your Web App URL and verify the application is running!

## Configuration Options

### Minimal Configuration (Free/Low Cost)

For demos and development:

```json
{
  "parameters": {
    "appServiceSku": { "value": "F1" },
    "sqlDatabaseSku": { "value": "GP_S_Gen5" },
    "sqlAutoPauseDelay": { "value": 60 }
  }
}
```

**Estimated cost:** $0.84 - $3.50/month

### Production Configuration

For production workloads:

```json
{
  "parameters": {
    "environment": { "value": "prod" },
    "appServiceSku": { "value": "S1" },
    "sqlDatabaseSku": { "value": "S0" },
    "appInsightsRetentionDays": { "value": 90 }
  }
}
```

### Development with Local Access

To access SQL Server from your local machine:

```json
{
  "parameters": {
    "allowLocalIp": { "value": true },
    "localIpAddress": { "value": "YOUR.PUBLIC.IP.ADDRESS" }
  }
}
```

Get your public IP:
```powershell
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
Write-Host "Your public IP: $myIp"
```

## Common Tasks

### View All Resources

```powershell
az resource list `
  --resource-group $resourceGroup `
  --output table
```

### View Application Logs

```powershell
az webapp log tail `
  --name $webAppName `
  --resource-group $resourceGroup
```

### Update Application Settings

```powershell
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings "NewSetting=NewValue"
```

### Scale App Service

```powershell
# Scale up (change tier)
az appservice plan update `
  --name "$appName-$environment-asp" `
  --resource-group $resourceGroup `
  --sku S1

# Scale out (add instances)
az appservice plan update `
  --name "$appName-$environment-asp" `
  --resource-group $resourceGroup `
  --number-of-workers 2
```

### Backup Database

```powershell
# Export database to bacpac
$storageKey = az storage account keys list `
  --account-name $storageAccountName `
  --resource-group $resourceGroup `
  --query "[0].value" `
  --output tsv

az sql db export `
  --name $databaseName `
  --server $sqlServerName `
  --resource-group $resourceGroup `
  --admin-user $sqlAdminUsername `
  --admin-password $sqlAdminPassword `
  --storage-key $storageKey `
  --storage-key-type StorageAccessKey `
  --storage-uri "https://${storageAccountName}.blob.core.windows.net/backups/backup.bacpac"
```

## Troubleshooting

### Issue: "Template validation failed"

**Check:**
1. Verify all required parameters are set
2. Ensure SQL password meets complexity requirements
3. Check that location is a valid Azure region

**Fix:**
```powershell
# View detailed error
az deployment sub validate `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json `
  --verbose
```

### Issue: "Resource name already exists"

**Solution:** Change `appName` or `environment` in parameters.json:

```json
{
  "parameters": {
    "appName": { "value": "rentalrepairs2" }
  }
}
```

### Issue: "Insufficient permissions"

**Check your role:**
```powershell
az role assignment list --assignee YOUR_EMAIL --output table
```

**Solution:** Contact your Azure subscription administrator to grant Contributor or Owner role.

### Issue: Web App shows "Service Unavailable"

**Common causes:**
1. Application not deployed yet
2. Database migrations not run
3. Key Vault access not granted

**Fix:**
```powershell
# Check Web App logs
az webapp log tail `
  --name $webAppName `
  --resource-group $resourceGroup

# Verify Key Vault access
az keyvault show `
  --name $keyVaultName `
  --resource-group $resourceGroup `
  --query "properties.accessPolicies"
```

## Next Steps

Now that your infrastructure is deployed:

1. **Set Up CI/CD**
   - Configure GitHub Actions or Azure DevOps
   - Automate deployments on code changes

2. **Configure Monitoring**
   - Set up Application Insights alerts
   - Create dashboard for key metrics

3. **Implement Backup Strategy**
   - Schedule database backups
   - Configure retention policies

4. **Security Hardening**
   - Enable Advanced Threat Protection
   - Configure firewall rules
   - Review access policies

5. **Performance Optimization**
   - Enable caching
   - Configure CDN
   - Optimize database queries

## Additional Resources

- [Full Deployment Guide](DEPLOYMENT_GUIDE.md) - Detailed instructions
- [Quick Reference](QUICK_REFERENCE.md) - Common commands
- [Azure Bicep Docs](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure CLI Reference](https://learn.microsoft.com/cli/azure/)

## Get Help

- **GitHub Issues**: Report problems in the project repository
- **Azure Support**: Create a support ticket in Azure Portal
- **Documentation**: Review the comprehensive guides in the `docs` folder

## Success Checklist

After completing this guide, you should have:

- [ ] Azure CLI and Bicep installed
- [ ] Successful deployment to Azure
- [ ] Web App accessible via URL
- [ ] Database created and migrations applied
- [ ] Key Vault configured with secrets
- [ ] Application Insights monitoring enabled
- [ ] Storage Account and CDN configured
- [ ] Application running and verified

Congratulations! Your Rental Repairs application is now running on Azure!
