# Option 3 Implementation - Query Handler Enrichment

## Summary

Moved business context enrichment logic FROM TenantRequestService INTO GetTenantRequestByIdQueryHandler.

This is the **TRUE CQRS pattern** - queries can return enriched DTOs when needed.

## Changes Made

### 1. ? Enhanced Query
**File**: `Application/Queries/TenantRequests/GetTenantRequestById/GetTenantRequestByIdQuery.cs`

```csharp
public class GetTenantRequestByIdQuery : IQuery<TenantRequestDto>
{
    public Guid Id { get; set; }
    
    // NEW: Optional flag for enrichment
    public bool IncludeBusinessContext { get; set; }

    public GetTenantRequestByIdQuery(Guid id)
    {
     Id = id;
    }
}
```

### 2. ? Enhanced Query Handler
**File**: `Application/Queries/TenantRequests/GetTenantRequestById/GetTenantRequestByIdQueryHandler.cs`

**Added**:
- Inject `ICurrentUserService`, `RequestAuthorizationPolicy`, `TenantRequestStatusPolicy`
- New method: `EnrichWithBusinessContext()`
- Returns `TenantRequestDetailsDto` when `IncludeBusinessContext = true`

**Logic moved from TenantRequestService**:
- Get current user role from claims
- Call authorization policy for available actions
- Calculate `CanEdit`, `CanCancel`, `CanAssignWorker`
- Get next allowed status

### 3. ? Moved DTO Definition
**File**: `Application/DTOs/TenantRequestDto.cs`

```csharp
public class TenantRequestDetailsDto : TenantRequestDto
{
    public List<string> AvailableActions { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
    public bool CanAssignWorker { get; set; }
    public string? NextAllowedStatus { get; set; }
}
```

### 4. ? Updated WebUI - Details.cshtml.cs
**File**: `WebUI/Pages/TenantRequests/Details.cshtml.cs`

**Before** (used service):
```csharp
var userEmail = User.Identity?.Name ?? "anonymous";
TenantRequest = await _tenantRequestService.GetRequestDetailsWithContextAsync(id, userEmail);
```

**After** (uses query directly):
```csharp
var query = new GetTenantRequestByIdQuery(id) { IncludeBusinessContext = true };
var result = await _mediator.Send(query);
TenantRequest = result as TenantRequestDetailsDto;
```

### 5. ? Removed Service Layer
- ? Deleted: `Application/Services/TenantRequestService.cs`
- ? Deleted: `Application/Interfaces/ITenantRequestService.cs`
- ? Removed from DI: `services.AddScoped<ITenantRequestService, TenantRequestService>()`

## Still TODO

### Files That Still Reference ITenantRequestService:
1. **WebUI/Pages/TenantRequests/Complete.cshtml.cs** - Worker completion page
2. **WebUI/Pages/TenantRequests/Decline.cshtml.cs** - Decline request page
3. **Application.Tests/Services/Step10ApplicationServiceValidationTests.cs** - Test file

These need to be updated to use the query directly.

## Benefits

### 1. True CQRS ?
- WebUI calls queries directly via MediatR
- No unnecessary service wrapper
- Clear separation: queries for reads, commands for writes

### 2. Multi-Client Support ?
- Any client (WebUI, API, Mobile) can call the query
- Business logic stays in Application layer
- No client-specific code

### 3. Flexibility ?
```csharp
// Simple DTO (no business context)
var simple = await _mediator.Send(new GetTenantRequestByIdQuery(id));

// Enriched DTO (with business context)
var enriched = await _mediator.Send(new GetTenantRequestByIdQuery(id) { IncludeBusinessContext = true });
```

### 4. Performance ?
- Only enrich when needed
- Single database call
- No duplicate service layer overhead

### 5. Maintainability ?
- Logic in ONE place (query handler)
- Easy to test
- Clear dependencies

## Architecture Diagram

### Before (Anti-pattern):
```
WebUI ? ITenantRequestService ? MediatR ? Query Handler ? Database
       ? (duplicates MediatR)
      Calls domain services
```

### After (CQRS):
```
WebUI ? MediatR ? Query Handler ? Database
        ?
         Domain Services (when IncludeBusinessContext = true)
```

## Next Steps

1. Update Complete.cshtml.cs to use query
2. Update Decline.cshtml.cs to use query  
3. Update or remove test file
4. Document pattern for other queries

## Example Usage

### WebUI (Razor Pages):
```csharp
// With business context
var query = new GetTenantRequestByIdQuery(id) { IncludeBusinessContext = true };
var result = await _mediator.Send(query);
var enrichedDto = (TenantRequestDetailsDto)result;

// Check authorization
if (enrichedDto.CanEdit)
{
    // Show edit button
}
```

### Future API Controller:
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeContext = false)
{
    var query = new GetTenantRequestByIdQuery(id) { IncludeBusinessContext = includeContext };
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### Future Mobile App:
```csharp
// Get simple DTO for list view
var listQuery = new GetTenantRequestByIdQuery(id);
var listItem = await mediator.Send(listQuery);

// Get enriched DTO for detail view
var detailQuery = new GetTenantRequestByIdQuery(id) { IncludeBusinessContext = true };
var detailItem = await mediator.Send(detailQuery);
```

## Pattern for Other Queries

This pattern can be applied to other queries that need business context:

```csharp
public class GetPropertyByIdQuery : IQuery<PropertyDto>
{
 public Guid Id { get; set; }
    public bool IncludeBusinessContext { get; set; } // Add this

    public GetPropertyByIdQuery(Guid id) => Id = id;
}

// Handler enriches when needed
public class Handler : IRequestHandler<GetPropertyByIdQuery, PropertyDto>
{
    public async Task<PropertyDto> Handle(...)
    {
        var property = await _context.Properties...;
 
        if (request.IncludeBusinessContext)
        {
            return EnrichWithBusinessContext(property);
      }
    
     return property;
    }
}
```

## Summary

? **Removed unnecessary service layer**
? **True CQRS pattern implemented**  
? **Business logic stays in Application layer**  
? **Supports multiple client types**  
? **Better performance**  
? **Easier to maintain**  

**Status**: Partial - Need to update 2 more pages + 1 test
