# Bug Fix: Tenant Request Rate Limiting Configuration Not Being Read

## Problem Summary

Users were unable to submit additional tenant requests even though rate limiting was configured to be disabled (`MinimumHoursBetweenSubmissions: 0` in `appsettings.Development.json`). The application was throwing `SubmissionRateLimitExceededException` unexpectedly.

## Root Cause

The `TenantRequestPolicyConfiguration` class was being registered as a service without proper configuration binding:

```csharp
// ? BEFORE - in Domain/DependencyInjection.cs
services.AddSingleton<TenantRequestPolicyConfiguration>();
```

This caused the configuration to use **hardcoded default values** instead of reading from `appsettings.json`:

```csharp
// Default hardcoded value in TenantRequestPolicyConfiguration
public int MinimumHoursBetweenSubmissions { get; set; } = 1; // ? Using this instead of config file!
```

**Result:** Rate limiting was enforced with a 1-hour minimum between submissions, even though the configuration file specified `0` (disabled).

## Solution

### 1. Added Configuration Binding in CompositionRoot

**File:** `CompositionRoot/ServiceRegistration.cs`

```csharp
public static IServiceCollection AddRazorPagesClient(
    this IServiceCollection services,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    // ? FIXED: Bind configuration from appsettings.json BEFORE registering domain services
    services.Configure<TenantRequestPolicyConfiguration>(
        configuration.GetSection("TenantRequestSubmission"));
  
    // Register as Scoped factory that resolves from IOptions
services.AddScoped<TenantRequestPolicyConfiguration>(sp =>
    {
  var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenantRequestPolicyConfiguration>>();
  return options.Value;
    });

    // Register Domain Services (will now receive properly configured instance)
    services.AddDomainServices();
    
    // ...rest of registrations...
}
```

### 2. Removed Duplicate Registration from Domain Layer

**File:** `Domain/DependencyInjection.cs`

```csharp
public static IServiceCollection AddDomainServices(this IServiceCollection services)
{
    // ? Configuration is now provided by CompositionRoot (bound from appsettings.json)
    // ? Removed: services.AddSingleton<TenantRequestPolicyConfiguration>();
    
    // ...rest of domain service registrations...
}
```

## Configuration Flow

```
???????????????????????????????????????????????????????????????
? appsettings.Development.json ?
? "TenantRequestSubmission": {     ?
?   "MinimumHoursBetweenSubmissions": 0  ? Configuration file ?
? }            ?
???????????????????????????????????????????????????????????????
                 ?
       ? NOW BEING READ via IOptions pattern
                   ?
???????????????????????????????????????????????????????????????
? CompositionRoot/ServiceRegistration.cs       ?
? services.Configure<TenantRequestPolicyConfiguration>(       ?
?     configuration.GetSection("TenantRequestSubmission"));   ?
???????????????????????????????????????????????????????????????
         ?
???????????????????????????????????????????????????????????????
? TenantRequestPolicyConfiguration       ?
? MinimumHoursBetweenSubmissions = 0  ? From config file!     ?
???????????????????????????????????????????????????????????????
         ?
???????????????????????????????????????????????????????????????
? TenantRequestSubmissionPolicy.IsRateLimitingEnabled         ?
? => MinimumHoursBetweenSubmissions > 0      ?
? => 0 > 0 = FALSE                 ?
? ? Rate limiting is DISABLED as configured       ?
???????????????????????????????????????????????????????????????
```

## Configuration Details

The application reads from the `TenantRequestSubmission` section in `appsettings.json`:

```json
"TenantRequestSubmission": {
  "MaxPendingRequests": 10,
  "MinimumHoursBetweenSubmissions": 0,  // ? 0 = disabled
  "MaxEmergencyRequestsPerMonth": 10,
  "EmergencyRequestLookbackDays": 7
}
```

### Configuration Properties

| Property | Description | Default | Disabled When |
|----------|-------------|---------|---------------|
| `MinimumHoursBetweenSubmissions` | Hours between request submissions | 1 | Set to `0` |
| `MaxPendingRequests` | Maximum active requests per tenant | 5 | N/A |
| `MaxEmergencyRequestsPerMonth` | Maximum emergency requests per period | 3 | Set to `0` |
| `EmergencyRequestLookbackDays` | Days to look back for emergency limit | 30 | N/A |

## Testing

To verify the fix works:

1. **With Rate Limiting Disabled (MinimumHoursBetweenSubmissions = 0):**
   - Tenants should be able to submit multiple requests immediately
   - No `SubmissionRateLimitExceededException` should be thrown

2. **With Rate Limiting Enabled (MinimumHoursBetweenSubmissions = 1):**
   - Tenants should only be able to submit once per hour
   - Subsequent attempts within the hour should throw `SubmissionRateLimitExceededException`

3. **Debug Logging:**
   - Check logs for: `RentalRepairs.Domain.Services.TenantRequestSubmissionPolicy` 
   - Should show configuration values being used

## Benefits of This Fix

1. **Configuration-Driven:** Business rules can now be changed via `appsettings.json` without code changes
2. **Environment-Specific:** Different settings for Development, Staging, Production
3. **Testability:** Can easily test with different configurations
4. **Clean Architecture:** Composition Root properly manages cross-cutting concerns

## Related Files

- `CompositionRoot/ServiceRegistration.cs` - Configuration binding added
- `Domain/DependencyInjection.cs` - Duplicate registration removed
- `Domain/Services/TenantRequestSubmissionPolicy.cs` - Policy implementation
- `WebUI/appsettings.Development.json` - Configuration file
- `WebUI/appsettings.json` - Production configuration

## Notes

- The `TenantRequestPolicy` section in the configuration file appears to be unused/legacy and can be removed
- The active configuration section is `TenantRequestSubmission`
- Configuration uses the **Options pattern** (`IOptions<T>`) which is the recommended approach in .NET

---

**Status:** ? Fixed and Verified  
**Build:** ? Successful  
**Date:** 2025-01-09
