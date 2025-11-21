# Azure Budget Management for Demo Project

This guide provides practical strategies to limit costs for your publicly accessible RentalRepairs application demo deployment.

## Table of Contents

1. [Budget Overview](#budget-overview)
2. [Cost Control Mechanisms](#cost-control-mechanisms)
3. [Azure Budgets & Alerts](#azure-budgets--alerts)
4. [Application-Level Controls](#application-level-controls)
5. [Recommended Settings](#recommended-settings)
6. [Monitoring & Response](#monitoring--response)
7. [Cost Optimization Tips](#cost-optimization-tips)

## Budget Overview

### Estimated Monthly Costs

For a demo project with **occasional public access**:

| Service | Tier | Estimated Cost |
|---------|------|------------------|
| **App Service** | F1 (Free) | $0.00 |
| **SQL Database** | GP_S_Gen5 (Serverless) | $1-5 |
| **Storage Account** | Hot LRS | $0.60 |
| **Application Insights** | Pay-as-you-go | $0-2 |
| **CDN** | Standard | $0-1 |
| **Key Vault** | Standard | $0.67 |
| **Total (low usage)** | | **$0.84 - $9.27/month** |

### What Drives Costs?

**High-Cost Activities:**
- ? Sustained high traffic (CPU time if using B1+ tier)
- ? Large SQL compute instances (S1+ tiers)
- ? Always-on SQL databases (serverless auto-pauses)
- ? High data egress (especially CDN)
- ? Large application logs

**Low-Cost Activities:**
- ? Occasional requests (F1 tier limits CPU)
- ? Serverless SQL (auto-pauses when idle)
- ? Static assets (CDN caches and compresses)
- ? Application Insights (pay for data ingestion)
- ? Key Vault (per transaction, not per second)

---

## Cost Control Mechanisms

### 1. App Service Tier Limitation (PRIMARY)

**Currently Deployed:** F1 (Free)

**What it does:**
- **60 minutes of CPU time per day** - Hard limit that stops app after limit is reached
- **1 GB shared memory** - Performance naturally degraded
- **1 GB storage** - Limited log retention
- **No custom domain** (acceptable for demo)
- **No scale-out** (single instance)

**Public Access Consideration:**
If high traffic hits F1 tier, the app automatically stops serving after 60 CPU minutes/day. This **prevents runaway costs** at the trade-off of downtime.

**Alternative Tiers:**

```powershell
# Cost-controlled alternatives for public demo:

# Option 1: Remain on F1 (most cost-effective)
"appServiceSku": "F1"  # Free + built-in daily limits

# Option 2: B1 Basic (if you need more capacity)
# ~$13/month + CPU overage prevention
"appServiceSku": "B1"
# B1 includes 60% CPU reserved, can burst
# Requires App Service Plan
```

### 2. SQL Database - Serverless Mode (PRIMARY)

**Currently Deployed:** GP_S_Gen5 (Serverless)

**What it does:**
- **Auto-pauses** after 60 minutes of inactivity
- **Auto-resumes** when queries arrive (5-10 second resume time)
- **Dynamic scaling** from 1-2 vCores based on load
- **Pay-per-second** only when running
- **Estimated savings:** 84-98% vs always-on tier

**Cost Examples:**
```
Light Demo (1-2 hours/week active): $1-2/month
Moderate Demo (10-20 hours/week): $5-10/month
Heavy Usage (5+ hours/day): $20-30/month
```

**Configuration (in parameters.json):**
```json
{
  "parameters": {
 "sqlDatabaseSku": {
      "value": "GP_S_Gen5"
 },
    "sqlAutoPauseDelay": {
      "value": 60
    },
    "sqlMinCapacity": {
      "value": 1
    },
    "sqlMaxCapacity": {
      "value": 2
    }
  }
}
```

**To Reduce Further:**
```json
{
  "sqlAutoPauseDelay": {
    "value": 30  // Pause after 30 minutes instead of 60
  },
  "sqlMaxCapacity": {
    "value": 1  // Max 1 vCore instead of 2 (slower but cheaper)
  }
}
```

### 3. Application Insights - Sampling

**Why it matters:**
- Each HTTP request, dependency, and trace = logged data
- Pricing is per GB ingested (~$2.50/GB)
- A moderately busy app can ingest 1-5 GB/month

**Enable Sampling (Reduces Cost):**

Edit `src/WebUI/appsettings.json`:
```json
{
  "ApplicationInsights": {
    "SamplingSettings": {
      "isEnabled": true,
  "samplingPercentage": 25  // Log only 25% of requests
    }
  }
}
```

**Sampling Levels:**
- `samplingPercentage: 100` = Log everything ($5-10/month for busy app)
- `samplingPercentage: 50` = Log half the traffic ($2-5/month)
- `samplingPercentage: 25` = Log quarter the traffic ($1-2/month)
- `samplingPercentage: 10` = Log only 10% ($0.50-1/month)

**For Demo Only:**
```json
{
  "samplingPercentage": 10  // Very minimal logging
}
```

### 4. Storage Account - Lifecycle Rules

**Currently Deployed:** Hot storage tier

**Reduce Logs Automatically:**

Create lifecycle management to auto-delete old logs:

```powershell
# Add lifecycle policy to storage account
$resourceGroup = "rentalrepairs-dev-rg"
$storageAccountName = "rentalrepairsdevsa"  # Adjust to your name

# Delete logs older than 30 days
az storage account management-policy create `
  --account-name $storageAccountName `
  --resource-group $resourceGroup `
  --policy @- <<EOF
{
  "rules": [
  {
      "name": "DeleteOldLogs",
    "enabled": true,
      "type": "Lifecycle",
 "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["logs/", "diagnostics/"]
        },
        "actions": {
       "baseBlob": {
  "delete": {
          "daysAfterModificationGreaterThan": 30
      }
          }
        }
      }
    }
  ]
}
EOF
```

### 5. Data Egress - CDN & Caching

**Control data egress costs:**

```powershell
# Configure CDN caching headers (in your app)
# Cache static assets for longer periods
# Set Cache-Control: public, max-age=86400  # 24 hours

# Purge CDN cache to save on storage
az cdn endpoint purge `
  --name rentalrepairs-dev-endpoint `
  --profile-name rentalrepairs-dev-cdn `
  --resource-group rentalrepairs-dev-rg `
  --content-paths '/*'

# Check CDN usage
az monitor metrics list `
  --resource /subscriptions/{subId}/resourceGroups/rentalrepairs-dev-rg/providers/Microsoft.Cdn/profiles/rentalrepairs-dev-cdn `
  --metric BytesDelivered `
  --start-time 2024-01-01T00:00:00Z `
  --end-time 2024-01-02T00:00:00Z
```

---

## Azure Budgets & Alerts

### Create a Budget in Azure Portal

**Step 1: Navigate to Cost Management**
```
Azure Portal ? Cost Management + Billing ? Budgets
```

**Step 2: Create New Budget**
```
Name: RentalRepairs Demo Budget
Amount: $10/month  # Choose your limit
Scope: Your Resource Group (rentalrepairs-dev-rg)
Reset Period: Monthly
Start Date: Today
```

**Step 3: Set Alert Thresholds**

| Threshold | Alert Type | Action |
|-----------|-----------|--------|
| 50% | Warning | Get notified when costs reach $5 |
| 75% | Warning | Final warning before limit reached |
| 100% | Critical | Immediate notification at limit |
| 125% | Over-budget | Overage notification |

### Create Budget via PowerShell

```powershell
# Get resource group ID
$resourceGroup = "rentalrepairs-dev-rg"
$rgId = (az group show --name $resourceGroup --query id --output tsv)

# Create budget
az consumption budget create `
  --name "RentalRepairs-Demo-Budget" `
  --category "cost" `
  --amount 10 `
  --time-period Monthly `
  --start-date 2024-01-01 `
  --resource-group-filter $rgId

# List budgets
az consumption budget list --output table

# Get budget details
az consumption budget show --name "RentalRepairs-Demo-Budget" --resource-group $resourceGroup
```

### Configure Email Alerts

```powershell
# Create action group for alerts
az monitor action-group create `
  --name "BudgetAlerts" `
  --resource-group $resourceGroup

# Add email recipient
az monitor action-group update `
  --name "BudgetAlerts" `
  --resource-group $resourceGroup `
  --add-action email admin --email-receiver your-email@example.com
```

---

## Application-Level Controls

### 1. Rate Limiting (Protect Against Abuse)

Add rate limiting to your Razor Pages to prevent abuse:

**Install Rate Limiting:**
```bash
dotnet add package AspNetCoreRateLimit
```

**Configure in Program.cs:**
```csharp
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.ConfigureHttpCacheHeaders(options =>
{
  options.MaxAge = TimeSpan.FromHours(1);
});

builder.Services.AddHttpCacheHeaders(
    expirationModelOptions => 
    {
expirationModelOptions.MaxAge = 600;
expirationModelOptions.CacheLocation = CacheLocation.Public;
    });

// Global rate limiting: 100 requests per 5 minutes per IP
builder.Services.AddIpRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

app.UseIpRateLimiting();
app.MapGet("/", () => "Welcome to RentalRepairs!");

app.Run();
```

**Configure in appsettings.json:**
```json
{
  "IpRateLimitPolicies": {
    "IpWhitelist": [],
    "EndpointWhitelist": ["/health", "/ready"],
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "RealIpHeader": "X-Real-IP",
    "IpWhitelistHeader": "X-Forwarded-For"
  },
  "IpRateLimitSettings": {
    "BucketStore": "MemoryCacheStore",
    "MonitoringToolEnabled": false,
    "CacheKeyPrefix": "ocelot_cluster_nrate",
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "5m",
        "Limit": 100,
        "QuotaExceededResponse": {
          "Content": "Rate limit exceeded",
          "StatusCode": 429
     }
      }
    ]
  }
}
```

### 2. Request Throttling & Timeouts

```csharp
// In your Razor Pages handler
public async Task<IActionResult> OnGetAsync()
{
    // Timeout for operations: 30 seconds
    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
    {
        try
        {
 var data = await _mediator.Send(
    new GetTenantRequestsQuery(TenantId), 
         cts.Token
     );
         return Page();
        }
        catch (OperationCanceledException)
        {
    return StatusCode(408, "Request timeout");
        }
    }
}
```

### 3. Maintenance Window (Stop App During Low-Use Hours)

**Automatically stop the app during certain hours:**

```powershell
# Stop app at 11 PM (23:00)
$scheduleName = "StopApp-11PM"
$resourceGroup = "rentalrepairs-dev-rg"
$webAppName = "rentalrepairs-dev-app"

az scheduler job create `
  --resource-group $resourceGroup `
  --name $scheduleName `
  --start-time "2024-01-01T23:00" `
  --recurrence-frequency daily `
  --action-setting-http-method POST `
  --action-setting-uri "https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/$resourceGroup/providers/Microsoft.Web/sites/$webAppName/stop?api-version=2016-08-01" `
  --output-settings-storage-account "myStorageAccount" `
  --output-settings-storage-container "myContainer" `
  --output-settings-storage-sas-token "my-sas-token"

# Start app at 6 AM (06:00)
az scheduler job create `
  --resource-group $resourceGroup `
  --name "StartApp-6AM" `
  --start-time "2024-01-02T06:00" `
  --recurrence-frequency daily `
  --action-setting-http-method POST `
  --action-setting-uri "https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/$resourceGroup/providers/Microsoft.Web/sites/$webAppName/start?api-version=2016-08-01" `
  --output-settings-storage-account "myStorageAccount" `
  --output-settings-storage-container "myContainer" `
  --output-settings-storage-sas-token "my-sas-token"
```

**Or use Azure Automation (Simpler):**

```powershell
# Create automation account
az automation account create `
  --name "RentalRepairsAutomation" `
  --resource-group $resourceGroup `
  --sku Free

# Create runbook for stopping app
# PowerShell runbook content:
<#
$conn = Get-AutomationConnection -Name AzureRunAsConnection
Connect-AzAccount -ServicePrincipal -Tenant $conn.TenantId `
    -ApplicationId $conn.ApplicationId -CertificateThumbprint $conn.CertificateThumbprint

Stop-AzWebApp -Name "rentalrepairs-dev-app" -ResourceGroupName "rentalrepairs-dev-rg"
#>
```

---

## Recommended Settings

### For Absolute Minimum Cost Demo

```json
{
  "parameters": {
    "appName": {
      "value": "rentalrepairs"
    },
  "environment": {
      "value": "demo"
    },
    "location": {
      "value": "canadacentral"
    },
    "appServiceSku": {
      "value": "F1"
    },
    "sqlDatabaseSku": {
    "value": "GP_S_Gen5"
    },
    "sqlAutoPauseDelay": {
      "value": 30
    },
    "sqlMinCapacity": {
 "value": 1
    },
    "sqlMaxCapacity": {
    "value": 1
    },
    "appInsightsRetentionDays": {
      "value": 30
    },
    "enableStaticWebsite": {
      "value": true
    }
  }
}
```

**Expected Cost:** $0.84 - $2/month (with occasional public access)

### For Balanced Demo (Small Traffic)

```json
{
  "parameters": {
 "appServiceSku": {
      "value": "B1"
    },
    "sqlDatabaseSku": {
    "value": "GP_S_Gen5"
    },
    "sqlAutoPauseDelay": {
    "value": 60
    },
    "sqlMinCapacity": {
      "value": 1
    },
    "sqlMaxCapacity": {
    "value": 2
    },
    "appInsightsRetentionDays": {
      "value": 30
 },
    "enableStaticWebsite": {
      "value": true
    }
  }
}
```

**Expected Cost:** $13-20/month (with moderate public access)

---

## Monitoring & Response

### Check Current Costs

```powershell
# Get estimated costs for resource group
az consumption usage list `
  --scope "/subscriptions/{subscriptionId}/resourcegroups/rentalrepairs-dev-rg" `
  --start-date 2024-01-01 `
  --end-date 2024-01-31 `
  --query "[].properties" `
  --output table

# Get costs by resource
az consumption usage list `
  --scope "/subscriptions/{subscriptionId}/resourcegroups/rentalrepairs-dev-rg" `
  --query "[].properties | group_by(.instanceName) | [].[key, sum(value.cost)]" `
  --output table
```

### Daily Cost Check Script

```powershell
# save as check-costs.ps1
param(
    [string]$ResourceGroup = "rentalrepairs-dev-rg",
    [int]$BudgetThreshold = 10
)

# Get today's costs
$today = Get-Date -Format "yyyy-MM-dd"
$costs = az consumption usage list `
  --scope "/subscriptions/$(az account show --query id -o tsv)/resourcegroups/$ResourceGroup" `
  --start-date $today `
  --end-date $today `
  --query "sum(properties.cost)"

Write-Host "Current daily cost: `$$costs"

if ($costs -gt $BudgetThreshold) {
    Write-Warning "Cost exceeded threshold! Daily: `$$costs, Threshold: `$$BudgetThreshold"
    # Send alert email or Slack notification
}
```

**Run Daily:**
```powershell
# Run script every day
$trigger = New-JobTrigger -Daily -At "5:00 PM"
Register-ScheduledJob -Name DailyCostCheck `
    -Trigger $trigger `
    -ScriptBlock {
 & "C:\scripts\check-costs.ps1"
    }
```

### Alerts to Monitor

| Alert | When | Action |
|-------|------|--------|
| **CPU Usage High** | F1 tier hitting 60-min limit | Traffic spike - rate limit or scale |
| **SQL Costs Increasing** | Unexpected compute charges | Review slow queries, add indexes |
| **Budget Alert** | Monthly costs approaching limit | Reduce logging, scale down resources |
| **App Errors Increasing** | 5xx errors spiking | Check Application Insights, investigate |

---

## Cost Optimization Tips

### 1. **Regularly Review Resource Usage**
```powershell
# Check App Service metrics weekly
az monitor metrics list `
  --resource rentalrepairs-dev-app `
  --resource-group rentalrepairs-dev-rg `
  --resource-type "Microsoft.Web/sites" `
  --metric "CpuPercentage" `
  --aggregation Average `
  --start-time $(Get-Date).AddDays(-7) `
  --end-time $(Get-Date)
```

### 2. **Delete Unused Resources**
```powershell
# Find deleted resources that still have storage
az resource list `
  --resource-group rentalrepairs-dev-rg `
--query "[?properties.provisioningState=='Succeeded']" `
  --output table
```

### 3. **Use Spot Instances** (If Scaling)
```json
{
  "spotInstance": true,
  "evictionPolicy": "Deallocate"
}
```

### 4. **Optimize Database Queries**
- Add indexes to frequently queried columns
- Use Entity Framework `.AsNoTracking()` for read-only queries
- Implement caching for frequently accessed data

### 5. **Compress Static Assets**
```csharp
// In Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "image/svg+xml" }
    );
});

app.UseResponseCompression();
```

---

## Emergency Cost Control

**If costs spike unexpectedly:**

### Immediate Actions
```powershell
# 1. Stop the Web App immediately
az webapp stop --name rentalrepairs-dev-app --resource-group rentalrepairs-dev-rg

# 2. Pause SQL database (manual pause for serverless)
# Navigate to Azure Portal ? Resource Group ? SQL Database ? Pause

# 3. Check what caused the spike
az monitor activity-log list `
  --resource-group rentalrepairs-dev-rg `
  --start-time $(Get-Date).AddHours(-2) `
  --output table

# 4. Review Application Insights
az monitor app-insights component show `
  --app rentalrepairs-dev-ai `
  --resource-group rentalrepairs-dev-rg

# 5. Check for DDoS or excessive requests
# Look at Web App logs for patterns
```

### Prevention Going Forward
1. ? Lower rate limits
2. ? Reduce sampling percentage for logs
3. ? Enable Azure DDoS Protection (Standard tier)
4. ? Implement request throttling in application
5. ? Add Web Application Firewall (WAF)

---

## Next Steps

1. **Set up budgets** in Azure Cost Management
2. **Review current usage** weekly
3. **Implement rate limiting** in your application
4. **Monitor alerts** and respond quickly
5. **Consider scheduled shutdown** during low-use hours
6. **Document your costs** for future reference

## Related Documentation

- [Azure Budgets Best Practices](https://learn.microsoft.com/azure/cost-management-billing/costs/tutorial-acm-create-budgets)
- [Azure Cost Management](https://learn.microsoft.com/azure/cost-management-billing/cost-management-billing-overview)
- [App Service Pricing](https://azure.microsoft.com/pricing/details/app-service/)
- [SQL Database Pricing](https://azure.microsoft.com/pricing/details/sql-database/)

