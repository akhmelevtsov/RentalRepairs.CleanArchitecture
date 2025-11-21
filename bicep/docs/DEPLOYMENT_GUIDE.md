# Bicep Deployment Guide

This comprehensive guide walks you through deploying the Rental Repairs application infrastructure to Azure using Bicep templates.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Pre-Deployment Setup](#pre-deployment-setup)
3. [Configuration](#configuration)
4. [Deployment Methods](#deployment-methods)
5. [Post-Deployment Steps](#post-deployment-steps)
6. [Verification](#verification)
7. [Troubleshooting](#troubleshooting)
8. [Cleanup](#cleanup)

## Prerequisites

### Required Software

1. **Azure CLI** (version 2.50.0 or later)
   ```powershell
   # Install on Windows
   winget install Microsoft.AzureCLI

   # Verify installation
   az --version
   ```

2. **Bicep CLI** (installed with Azure CLI)
   ```powershell
   # Verify Bicep installation
   az bicep version

   # Update Bicep to latest
   az bicep upgrade
   ```

3. **PowerShell 7+** or Bash
   ```powershell
   # Check PowerShell version
   $PSVersionTable.PSVersion
   ```

### Azure Requirements

1. **Azure Subscription**
   - Active subscription with available credits
   - Subscription ID (found in Azure Portal)

2. **Permissions**
   - Contributor or Owner role on the subscription
   - Ability to create service principals (for advanced scenarios)

3. **Resource Providers**
   - Ensure required providers are registered:
     - Microsoft.Web
     - Microsoft.Sql
     - Microsoft.KeyVault
     - Microsoft.Storage
     - Microsoft.Cdn
     - Microsoft.Insights

   ```powershell
   # Check provider registration status
   az provider show --namespace Microsoft.Web --query "registrationState"

   # Register a provider if needed
   az provider register --namespace Microsoft.Web
   ```

## Pre-Deployment Setup

### 1. Clone or Navigate to Repository

```powershell
cd C:\Users\akhme\source\repos\akhmelevtsov\RentalRepairsModernized\src\bicep
```

### 2. Login to Azure

```powershell
# Login interactively
az login

# List available subscriptions
az account list --output table

# Set the subscription you want to use
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Verify current subscription
az account show
```

### 3. Get Your Public IP Address (Optional)

If you want to access SQL Server from your local machine:

```powershell
# Get your public IP
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
Write-Host "Your public IP: $myIp"
```

### 4. Generate SQL Admin Password

Generate a secure password for SQL Server:

```powershell
# Generate a random 24-character password
Add-Type -AssemblyName System.Web
$password = [System.Web.Security.Membership]::GeneratePassword(24, 8)
Write-Host "Generated password: $password"
# IMPORTANT: Save this password securely!
```

## Configuration

### 1. Create Parameters File

Copy the example file and customize it:

```powershell
Copy-Item parameters.example.json parameters.json
```

### 2. Edit Parameters File

Open `parameters.json` and configure the following:

#### Required Parameters

```json
{
  "parameters": {
    "appName": {
      "value": "rentalrepairs"  // Your app name (3-20 chars, lowercase)
    },
    "environment": {
      "value": "dev"  // dev, staging, or prod
    },
    "location": {
      "value": "canadacentral"  // Azure region
    },
    "sqlAdminPassword": {
      "value": "YOUR_SECURE_PASSWORD_HERE"  // Replace with generated password
    }
  }
}
```

#### Optional Parameters

**App Service Configuration:**
```json
"appServiceSku": {
  "value": "F1"  // F1 (Free), B1 (Basic), S1 (Standard), P1v2 (Premium)
}
```

**SQL Database Configuration:**
```json
"sqlDatabaseSku": {
  "value": "GP_S_Gen5"  // Serverless (cost-effective)
},
"sqlAutoPauseDelay": {
  "value": 60  // Minutes before auto-pause (-1 to disable)
},
"sqlMinCapacity": {
  "value": 1  // Minimum vCores when active
},
"sqlMaxCapacity": {
  "value": 2  // Maximum vCores when active
}
```

**Network Configuration:**
```json
"allowLocalIp": {
  "value": true  // Allow your IP to access SQL Server
},
"localIpAddress": {
  "value": "YOUR.PUBLIC.IP.ADDRESS"  // Your public IP
}
```

### 3. Validate Parameters

```powershell
# Basic validation
Get-Content parameters.json | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Check for required fields
$params = Get-Content parameters.json | ConvertFrom-Json
if ($params.parameters.sqlAdminPassword.value -eq "YOUR_SECURE_PASSWORD_HERE") {
    Write-Warning "Please set a secure SQL admin password!"
}
```

## Deployment Methods

### Method 1: Azure CLI (Recommended)

#### Step 1: Validate Template

```powershell
# Validate the Bicep template
az deployment sub validate `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

#### Step 2: Preview Changes (What-If)

```powershell
# Preview what will be created
az deployment sub what-if `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json
```

#### Step 3: Deploy

```powershell
# Deploy the infrastructure
$deploymentName = "rentalrepairs-$(Get-Date -Format 'yyyyMMddHHmmss')"

az deployment sub create `
  --name $deploymentName `
  --location canadacentral `
  --template-file main.bicep `
  --parameters @parameters.json `
  --verbose
```

This will take approximately 5-10 minutes.

#### Step 4: Monitor Deployment

```powershell
# Watch deployment progress
az deployment sub show `
  --name $deploymentName `
  --query "properties.provisioningState"

# Get deployment outputs
az deployment sub show `
  --name $deploymentName `
  --query "properties.outputs"
```

### Method 2: PowerShell Script

Create a deployment script `deploy.ps1`:

```powershell
# deploy.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,

    [Parameter(Mandatory=$false)]
    [string]$Location = "canadacentral",

    [Parameter(Mandatory=$false)]
    [string]$ParametersFile = "parameters.json"
)

# Login and set subscription
Write-Host "Setting Azure subscription..." -ForegroundColor Green
az account set --subscription $SubscriptionId

# Validate template
Write-Host "Validating Bicep template..." -ForegroundColor Green
az deployment sub validate `
  --location $Location `
  --template-file main.bicep `
  --parameters @$ParametersFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "Template validation failed!"
    exit 1
}

# Preview changes
Write-Host "Previewing changes..." -ForegroundColor Green
az deployment sub what-if `
  --location $Location `
  --template-file main.bicep `
  --parameters @$ParametersFile

# Confirm deployment
$confirm = Read-Host "Do you want to proceed with deployment? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

# Deploy
$deploymentName = "rentalrepairs-$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Host "Starting deployment: $deploymentName" -ForegroundColor Green

az deployment sub create `
  --name $deploymentName `
  --location $Location `
  --template-file main.bicep `
  --parameters @$ParametersFile `
  --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host "Deployment completed successfully!" -ForegroundColor Green

    # Display outputs
    Write-Host "`nDeployment Outputs:" -ForegroundColor Cyan
    az deployment sub show `
      --name $deploymentName `
      --query "properties.outputs" `
      --output json
} else {
    Write-Error "Deployment failed!"
    exit 1
}
```

Run the script:

```powershell
.\deploy.ps1 -SubscriptionId "YOUR_SUBSCRIPTION_ID"
```

### Method 3: Azure Portal (Manual)

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource" → "Template deployment (custom template)"
3. Click "Build your own template in the editor"
4. Copy contents of `main.bicep` and paste
5. Click "Save"
6. Fill in parameter values
7. Review and create

## Post-Deployment Steps

### 1. Get Deployment Outputs

```powershell
# Get all outputs
$outputs = az deployment sub show `
  --name $deploymentName `
  --query "properties.outputs" | ConvertFrom-Json

# Display important information
Write-Host "Web App URL: $($outputs.webAppUrl.value)"
Write-Host "SQL Server: $($outputs.sqlServerFqdn.value)"
Write-Host "Database: $($outputs.sqlDatabaseName.value)"
Write-Host "Key Vault: $($outputs.keyVaultName.value)"
```

### 2. Update Key Vault Access Policy

The Web App's managed identity needs access to Key Vault. This may require a manual step:

```powershell
# Get Web App principal ID
$webAppPrincipalId = $outputs.webAppPrincipalId.value
$keyVaultName = $outputs.keyVaultName.value
$resourceGroup = $outputs.deploymentSummary.value.resourceGroup

# Grant Key Vault access
az keyvault set-policy `
  --name $keyVaultName `
  --resource-group $resourceGroup `
  --object-id $webAppPrincipalId `
  --secret-permissions get list
```

### 3. Verify Key Vault Secrets

```powershell
# List secrets in Key Vault
az keyvault secret list `
  --vault-name $keyVaultName `
  --output table

# Verify connection string (optional - be careful not to expose)
# az keyvault secret show `
#   --vault-name $keyVaultName `
#   --name SqlConnectionString
```

### 4. Deploy Application Code

#### Option A: Visual Studio

1. Right-click the Web project
2. Select "Publish"
3. Choose "Azure"
4. Select your App Service
5. Click "Publish"

#### Option B: Azure CLI

```powershell
# Navigate to your web project
cd ..\RentalRepairs.Web

# Build and publish
dotnet publish -c Release -o ./publish

# Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath ./app.zip -Force

# Deploy to App Service
$webAppName = $outputs.webAppName.value
az webapp deploy `
  --resource-group $resourceGroup `
  --name $webAppName `
  --src-path ./app.zip `
  --type zip
```

#### Option C: GitHub Actions (CI/CD)

See the main project documentation for setting up GitHub Actions.

### 5. Run Database Migrations

```powershell
# Get connection string from Key Vault
$connectionString = az keyvault secret show `
  --vault-name $keyVaultName `
  --name SqlConnectionString `
  --query "value" `
  --output tsv

# Apply migrations (from your project directory)
cd ..\RentalRepairs.Infrastructure
dotnet ef database update `
  --connection "$connectionString" `
  --project RentalRepairs.Infrastructure.csproj `
  --startup-project ..\RentalRepairs.Web\RentalRepairs.Web.csproj
```

### 6. Configure Custom Domain (Optional)

```powershell
$customDomain = "www.yourapp.com"

# Add custom domain
az webapp config hostname add `
  --webapp-name $webAppName `
  --resource-group $resourceGroup `
  --hostname $customDomain

# Enable HTTPS
az webapp config ssl bind `
  --name $webAppName `
  --resource-group $resourceGroup `
  --certificate-thumbprint YOUR_CERT_THUMBPRINT `
  --ssl-type SNI
```

## Verification

### 1. Check Resource Deployment

```powershell
# List all resources in resource group
az resource list `
  --resource-group $resourceGroup `
  --output table

# Expected resources:
# - App Service Plan
# - App Service (Web App)
# - SQL Server
# - SQL Database
# - Key Vault
# - Storage Account
# - Application Insights
# - CDN Profile
# - CDN Endpoint
```

### 2. Verify Web App

```powershell
# Check Web App status
az webapp show `
  --name $webAppName `
  --resource-group $resourceGroup `
  --query "state"

# Open Web App in browser
Start-Process $outputs.webAppUrl.value
```

### 3. Test SQL Connection

```powershell
# Test SQL Server connectivity (requires SQL Server PowerShell module)
$sqlServer = $outputs.sqlServerFqdn.value
$database = $outputs.sqlDatabaseName.value

Test-NetConnection -ComputerName $sqlServer -Port 1433
```

### 4. Check Application Insights

```powershell
# Get Application Insights data
$appInsightsName = "$appName-$environment-ai"

az monitor app-insights component show `
  --app $appInsightsName `
  --resource-group $resourceGroup
```

### 5. Verify Storage Account

```powershell
# List storage containers
$storageAccountName = $outputs.storageAccountName.value

az storage container list `
  --account-name $storageAccountName `
  --output table
```

## Troubleshooting

### Common Issues

#### Issue 1: Deployment Fails with "Location Not Available"

**Solution:** Change the `location` parameter to a different Azure region:

```json
"location": {
  "value": "eastus"  // or "westeurope", "eastus2", etc.
}
```

#### Issue 2: SQL Password Doesn't Meet Complexity Requirements

**Solution:** Ensure password has:
- At least 12 characters
- Uppercase and lowercase letters
- Numbers
- Special characters

```powershell
# Generate compliant password
Add-Type -AssemblyName System.Web
$password = [System.Web.Security.Membership]::GeneratePassword(24, 8)
```

#### Issue 3: Key Vault Name Already Exists

**Solution:** Key Vault names must be globally unique. Change `appName` or `environment`:

```json
"appName": {
  "value": "rentalrepairs2"  // Add suffix or different name
}
```

#### Issue 4: Insufficient Permissions

**Error:** "Authorization failed for template resource..."

**Solution:** Ensure you have Contributor or Owner role:

```powershell
# Check your role assignments
az role assignment list --assignee YOUR_EMAIL --output table

# Request access from subscription administrator
```

#### Issue 5: Web App Can't Access Key Vault

**Solution:** Manually grant access:

```powershell
az keyvault set-policy `
  --name $keyVaultName `
  --resource-group $resourceGroup `
  --object-id $webAppPrincipalId `
  --secret-permissions get list
```

### Deployment Logs

```powershell
# View detailed deployment logs
az deployment sub show `
  --name $deploymentName `
  --query "properties.error"

# View specific resource deployment
az deployment group list `
  --resource-group $resourceGroup `
  --output table
```

### Get Support

1. **Check deployment status in Azure Portal**
   - Navigate to Subscriptions → Deployments
   - Click on deployment name
   - Review error details

2. **Enable diagnostic logging**
   ```powershell
   az webapp log config `
     --name $webAppName `
     --resource-group $resourceGroup `
     --application-logging filesystem `
     --level information

   # View logs
   az webapp log tail `
     --name $webAppName `
     --resource-group $resourceGroup
   ```

3. **Contact Support**
   - GitHub Issues: Report in project repository
   - Azure Support: Create support ticket in Azure Portal

## Cleanup

### Delete All Resources

```powershell
# Delete resource group (WARNING: This deletes everything!)
az group delete `
  --name $resourceGroup `
  --yes `
  --no-wait

# Purge Key Vault (if soft delete is enabled)
az keyvault purge `
  --name $keyVaultName `
  --no-wait
```

### Delete Specific Resources

```powershell
# Delete only Web App
az webapp delete `
  --name $webAppName `
  --resource-group $resourceGroup

# Delete only SQL Database (keep server)
az sql db delete `
  --name $database `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --yes
```

## Next Steps

1. Review [QUICK_REFERENCE.md](QUICK_REFERENCE.md) for common operations
2. Set up CI/CD pipeline for automated deployments
3. Configure monitoring and alerts
4. Implement backup strategy
5. Plan for scaling and high availability

## Additional Resources

- [Azure Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure CLI Reference](https://learn.microsoft.com/cli/azure/)
- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [Azure SQL Documentation](https://learn.microsoft.com/azure/azure-sql/)
