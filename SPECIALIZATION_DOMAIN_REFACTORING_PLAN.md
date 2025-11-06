# Specialization Domain Refactoring - Implementation Plan

**Date**: 2024  
**Status**: ?? **PLANNING**  
**Scope**: Move specialization logic to Domain layer with proper enum-based implementation

---

## Agreement & Analysis

### ? Your Proposal is Correct

**1. Domain Service with Enum** ?
- Specialization determination is domain logic
- Enum is type-safe vs. magic strings
- Domain Service is proper pattern for this cross-entity logic

**2. Determine Specialization During Scheduling** ?
- Natural place to infer required specialization
- Tenant doesn't need to know about specializations
- Worker specialization is determined from request description

**No Objections** - This is the correct DDD approach!

---

## Current Problems Summary

### ? Problem 1: Magic Strings Everywhere
```csharp
// Domain
public string? Specialization { get; private set; }
Worker.DetermineRequiredSpecialization("title", "desc") // Returns string

// Application  
"Plumbing", "Electrical", "HVAC" // Hard-coded strings

// Configuration
{ "Specialization": "Plumber", "Keywords": [...] }
```

### ? Problem 2: Logic Duplication (3 copies!)
- `Worker.DetermineRequiredSpecialization()` - hard-coded keywords
- `WorkerService.DetermineRequiredSpecialization()` - config-based keywords
- `UnitSchedulingService.NormalizeSpecialization()` - hard-coded mapping

### ? Problem 3: Specialization Determination Location
Currently happens in:
- `WorkerService.GetAssignmentContextAsync()` - Application layer ?
- `Worker.DetermineRequiredSpecialization()` - Domain entity (static) ?

Should happen in:
- `ScheduleServiceWorkCommandHandler` - When scheduling ?

---

## Proposed Solution

### ? Phase 1: Create Specialization Enum (Domain)

**File**: `Domain/Enums/WorkerSpecialization.cs`

```csharp
namespace RentalRepairs.Domain.Enums;

/// <summary>
/// Worker specialization types for maintenance work.
/// Defines the types of work a worker can perform.
/// </summary>
public enum WorkerSpecialization
{
    /// <summary>
/// General maintenance - can handle any type of work
    /// </summary>
    GeneralMaintenance = 0,
    
    /// <summary>
    /// Plumbing work (leaks, pipes, drains, toilets)
    /// </summary>
    Plumbing = 1,
    
    /// <summary>
  /// Electrical work (outlets, wiring, lights, circuits)
    /// </summary>
    Electrical = 2,
    
    /// <summary>
    /// HVAC work (heating, cooling, ventilation)
    /// </summary>
    HVAC = 3,
    
    /// <summary>
    /// Carpentry work (wood, cabinets, doors, frames)
    /// </summary>
    Carpentry = 4,
  
    /// <summary>
    /// Painting work (walls, ceilings, trim)
    /// </summary>
    Painting = 5,
    
    /// <summary>
    /// Locksmith work (locks, keys, security)
    /// </summary>
    Locksmith = 6,
    
    /// <summary>
    /// Appliance repair (refrigerators, washers, dryers, ovens)
    /// </summary>
    ApplianceRepair = 7
}
```

---

### ? Phase 2: Create Domain Service

**File**: `Domain/Services/SpecializationDeterminationService.cs`

```csharp
namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for determining required worker specialization from work descriptions.
/// Encapsulates the business logic of mapping work descriptions to specializations.
/// </summary>
public class SpecializationDeterminationService
{
    // Keyword mappings for each specialization
    private static readonly Dictionary<WorkerSpecialization, string[]> _specializationKeywords = new()
    {
        [WorkerSpecialization.Plumbing] = new[]
        {
          "plumb", "leak", "water", "drain", "pipe", "faucet", "toilet", 
          "sink", "clog", "drip", "flush"
        },
        [WorkerSpecialization.Electrical] = new[]
        {
    "electric", "power", "outlet", "wiring", "light", "switch", 
   "breaker", "circuit", "lamp", "fixture", "voltage"
  },
    [WorkerSpecialization.HVAC] = new[]
        {
            "heat", "hvac", "air", "furnace", "thermostat", "cooling", 
        "ventilation", "ac", "temperature", "warm", "cold"
     },
      [WorkerSpecialization.Locksmith] = new[]
  {
            "lock", "key", "security", "deadbolt", "locked out", 
         "lockout", "unlock", "rekey"
     },
        [WorkerSpecialization.Painting] = new[]
        {
            "paint", "wall", "ceiling", "trim", "brush", "roller", 
    "repaint", "color"
        },
 [WorkerSpecialization.Carpentry] = new[]
        {
      "wood", "cabinet", "door", "frame", "carpenter", "build", 
      "shelf", "install"
    },
        [WorkerSpecialization.ApplianceRepair] = new[]
        {
  "appliance", "refrigerator", "washer", "dryer", "dishwasher", 
      "oven", "stove", "microwave", "freezer"
        }
    };

    /// <summary>
    /// Determines required specialization from work title and description.
    /// Uses keyword matching with priority ordering.
    /// </summary>
    /// <param name="title">Work request title</param>
    /// <param name="description">Work request description</param>
    /// <returns>Required worker specialization</returns>
    public WorkerSpecialization DetermineRequiredSpecialization(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
   return WorkerSpecialization.GeneralMaintenance;
        }

        string text = $"{title} {description}".ToLowerInvariant();

        // Check specializations in priority order
        // More specific keywords checked first (e.g., Locksmith before Carpentry for "door")
        var priorityOrder = new[]
 {
            WorkerSpecialization.Locksmith,      // Check first (lock is more specific than door)
  WorkerSpecialization.Plumbing,
      WorkerSpecialization.Electrical,
            WorkerSpecialization.HVAC,
            WorkerSpecialization.Painting,
      WorkerSpecialization.Carpentry,      // Check after Locksmith
          WorkerSpecialization.ApplianceRepair
        };

  foreach (var specialization in priorityOrder)
        {
            var keywords = _specializationKeywords[specialization];
        if (keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
 {
     return specialization;
            }
     }

   return WorkerSpecialization.GeneralMaintenance;
    }

    /// <summary>
    /// Checks if a worker's specialization can handle the required work.
    /// General Maintenance workers can handle any work type.
    /// </summary>
    public bool CanHandleWork(WorkerSpecialization workerSpecialization, WorkerSpecialization requiredSpecialization)
    {
    // Exact match
     if (workerSpecialization == requiredSpecialization)
        {
      return true;
     }

        // General Maintenance can handle anything
if (workerSpecialization == WorkerSpecialization.GeneralMaintenance)
        {
      return true;
        }

  return false;
    }

    /// <summary>
    /// Parses specialization from string (for backward compatibility and UI).
    /// Handles common variations and normalizes to enum.
    /// </summary>
    public WorkerSpecialization ParseSpecialization(string specializationText)
    {
        if (string.IsNullOrWhiteSpace(specializationText))
     {
      return WorkerSpecialization.GeneralMaintenance;
  }

        var normalized = specializationText.Trim().ToLowerInvariant();

        return normalized switch
        {
        "plumbing" or "plumber" => WorkerSpecialization.Plumbing,
  "electrical" or "electrician" => WorkerSpecialization.Electrical,
            "hvac" or "hvac technician" or "heating" or "cooling" => WorkerSpecialization.HVAC,
        "carpentry" or "carpenter" => WorkerSpecialization.Carpentry,
         "painting" or "painter" => WorkerSpecialization.Painting,
            "locksmith" => WorkerSpecialization.Locksmith,
            "appliance repair" or "appliance technician" => WorkerSpecialization.ApplianceRepair,
 "general maintenance" or "maintenance" or "general" => WorkerSpecialization.GeneralMaintenance,
            _ => Enum.TryParse<WorkerSpecialization>(specializationText, true, out var result)
? result
           : WorkerSpecialization.GeneralMaintenance
      };
    }

    /// <summary>
    /// Gets display name for specialization.
    /// </summary>
    public string GetDisplayName(WorkerSpecialization specialization)
    {
        return specialization switch
        {
         WorkerSpecialization.GeneralMaintenance => "General Maintenance",
     WorkerSpecialization.Plumbing => "Plumbing",
            WorkerSpecialization.Electrical => "Electrical",
       WorkerSpecialization.HVAC => "HVAC",
        WorkerSpecialization.Carpentry => "Carpentry",
         WorkerSpecialization.Painting => "Painting",
            WorkerSpecialization.Locksmith => "Locksmith",
         WorkerSpecialization.ApplianceRepair => "Appliance Repair",
            _ => "General Maintenance"
 };
    }
}
```

---

### ? Phase 3: Update Worker Entity

**File**: `Domain/Entities/Worker.cs`

```csharp
public class Worker : BaseEntity, IAggregateRoot
{
    // Change from string to enum
    public WorkerSpecialization Specialization { get; private set; } = WorkerSpecialization.GeneralMaintenance;
    
    public void SetSpecialization(WorkerSpecialization specialization)
    {
  var oldSpecialization = Specialization;
        Specialization = specialization;

        if (oldSpecialization != specialization)
        {
          AddDomainEvent(new WorkerSpecializationChangedEvent(
       this, 
        oldSpecialization, 
    specialization));
        }
    }

    /// <summary>
    /// Checks if this worker can handle the required specialization.
    /// Uses domain service for business logic.
    /// </summary>
    public bool HasSpecializedSkills(WorkerSpecialization requiredSpecialization, 
        SpecializationDeterminationService specializationService)
    {
        return specializationService.CanHandleWork(Specialization, requiredSpecialization);
    }

    public void ValidateCanBeAssignedToRequest(
        DateTime scheduledDate, 
string workOrderNumber, 
    WorkerSpecialization requiredSpecialization,
        SpecializationDeterminationService specializationService)
    {
      // ... existing validation ...

        // Business rule: Worker must have required specialization
        if (!specializationService.CanHandleWork(Specialization, requiredSpecialization))
      {
            throw new WorkerNotAvailableException(
    ContactInfo.EmailAddress,
     $"Worker does not have required specialization: {requiredSpecialization}");
     }
    }

    // ? DELETE: Remove static method - logic moves to domain service
    // public static string DetermineRequiredSpecialization(string title, string description) { ... }
    
    // ? DELETE: Remove normalization - handled by domain service
    // private static string NormalizeSpecialization(string specialization) { ... }
}
```

---

### ? Phase 4: Update Domain Events

**File**: `Domain/Events/Workers/WorkerSpecializationChangedEvent.cs`

```csharp
public class WorkerSpecializationChangedEvent : DomainEvent
{
    public Worker Worker { get; }
    public WorkerSpecialization OldSpecialization { get; }
    public WorkerSpecialization NewSpecialization { get; }

    public WorkerSpecializationChangedEvent(
        Worker worker,
        WorkerSpecialization oldSpecialization,
        WorkerSpecialization newSpecialization)
    {
        Worker = worker;
        OldSpecialization = oldSpecialization;
  NewSpecialization = newSpecialization;
    }
}
```

---

### ? Phase 5: Update Command Handler (Key Change!)

**File**: `Application/Commands/TenantRequests/ScheduleServiceWork/ScheduleServiceWorkCommandHandler.cs`

```csharp
public class ScheduleServiceWorkCommandHandler : IRequestHandler<ScheduleServiceWorkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly UnitSchedulingService _unitSchedulingService;
    private readonly SpecializationDeterminationService _specializationService; // NEW
    private readonly ILogger<ScheduleServiceWorkCommandHandler> _logger;

    public ScheduleServiceWorkCommandHandler(
 IApplicationDbContext context,
    UnitSchedulingService unitSchedulingService,
        SpecializationDeterminationService specializationService, // NEW
        ILogger<ScheduleServiceWorkCommandHandler> logger)
    {
        _context = context;
        _unitSchedulingService = unitSchedulingService;
        _specializationService = specializationService; // NEW
        _logger = logger;
    }

    public async Task Handle(ScheduleServiceWorkCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
     "Processing schedule service work command for request {RequestId} with worker {WorkerEmail}",
     request.TenantRequestId, request.WorkerEmail);

        // Get tenant request
    var tenantRequest = await _context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRequestId, cancellationToken);

        if (tenantRequest == null)
            throw new InvalidOperationException($"Tenant request with ID {request.TenantRequestId} not found");

     // Get worker with assignments
   var worker = await _context.Workers
            .Include(w => w.Assignments)
      .FirstOrDefaultAsync(w => w.ContactInfo.EmailAddress == request.WorkerEmail, cancellationToken);

        if (worker == null)
       throw new InvalidOperationException($"Worker with email {request.WorkerEmail} not found");

        // ? NEW: Determine required specialization from request description
 var requiredSpecialization = _specializationService.DetermineRequiredSpecialization(
            tenantRequest.Title,
            tenantRequest.Description);

        _logger.LogInformation(
            "Determined required specialization: {Specialization} for request {RequestId}",
       requiredSpecialization, request.TenantRequestId);

        // Get property for validation
        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == tenantRequest.PropertyId, cancellationToken);

        // Get existing assignments for validation
        var existingAssignments = await GetExistingAssignmentsAsync(request.ScheduledDate, cancellationToken);

        // ? UPDATED: Use enum instead of string
        var validationResult = _unitSchedulingService.ValidateWorkerAssignment(
   request.TenantRequestId,
   property?.Code ?? "Unknown",
          tenantRequest.TenantUnit,
     request.ScheduledDate,
            request.WorkerEmail,
            worker.Specialization, // Now enum
            requiredSpecialization, // Now enum
    tenantRequest.IsEmergency,
     existingAssignments);

        if (!validationResult.IsValid)
    {
       throw new InvalidOperationException(
    $"Worker assignment validation failed: {validationResult.ErrorMessage}");
  }

 // Handle emergency overrides if needed
        if (tenantRequest.IsEmergency && validationResult.AssignmentsToCancelForEmergency.Any())
        {
  await ProcessEmergencyOverrides(validationResult.AssignmentsToCancelForEmergency, cancellationToken);
        }

        // ? UPDATED: Use domain service for validation
  worker.ValidateCanBeAssignedToRequest(
            request.ScheduledDate,
   request.WorkOrderNumber,
      requiredSpecialization,
            _specializationService);

        // Assign work
        worker.AssignToWork(request.WorkOrderNumber, request.ScheduledDate);

        // Schedule request
        var workerName = worker.ContactInfo.GetFullName();
    tenantRequest.Schedule(
       request.ScheduledDate,
         worker.ContactInfo.EmailAddress,
          request.WorkOrderNumber,
   workerName);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
  "Successfully scheduled work for request {RequestId} with worker {WorkerEmail} (specialization: {Specialization})",
            request.TenantRequestId, request.WorkerEmail, requiredSpecialization);
    }

    // ... helper methods ...
}
```

---

### ? Phase 6: Update WorkerService (Application Layer)

**File**: `Application/Services/WorkerService.cs`

```csharp
public class WorkerService : IWorkerService
{
 private readonly IMediator _mediator;
    private readonly ILogger<WorkerService> _logger;
    private readonly WorkerServiceSettings _settings;
    private readonly SpecializationDeterminationService _specializationService; // NEW

    public WorkerService(
     IMediator mediator,
  ILogger<WorkerService> logger,
        IOptions<WorkerServiceSettings> settings,
        SpecializationDeterminationService specializationService) // NEW
    {
        _mediator = mediator;
      _logger = logger;
        _settings = settings.Value;
     _specializationService = specializationService; // NEW
    }

    public async Task<WorkerAssignmentContextDto> GetAssignmentContextAsync(
     Guid requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting assignment context for request {RequestId}", requestId);

        var request = await _mediator.Send(
      new GetTenantRequestByIdQuery(requestId),
     cancellationToken);

        if (request == null)
            throw new NotFoundException($"Tenant request with ID {requestId} not found");

        // ? NEW: Use domain service instead of configuration
        var requiredSpecialization = _specializationService.DetermineRequiredSpecialization(
            request.Title,
  request.Description);

        _logger.LogInformation(
  "Determined specialization: {Specialization} for request {RequestId}",
            requiredSpecialization, requestId);

 var suggestedDates = GenerateSuggestedDates();

        var isEmergencyRequest = request.UrgencyLevel.Contains("Emergency", StringComparison.OrdinalIgnoreCase) ||
    request.UrgencyLevel.Contains("Critical", StringComparison.OrdinalIgnoreCase);

   // ? UPDATED: Pass enum instead of string
        var availableWorkers = await GetAvailableWorkersForRequestAsync(
     requestId,
            requiredSpecialization,
          suggestedDates.First(),
       isEmergencyRequest,
      cancellationToken);

        return new WorkerAssignmentContextDto
 {
   Request = request,
   AvailableWorkers = availableWorkers,
     SuggestedDates = suggestedDates,
        IsEmergencyRequest = isEmergencyRequest,
        RequiredSpecialization = requiredSpecialization // NEW: Expose enum
        };
    }

    public async Task<List<WorkerOptionDto>> GetAvailableWorkersForRequestAsync(
        Guid requestId,
        WorkerSpecialization requiredSpecialization, // ? CHANGED: string ? enum
        DateTime preferredDate,
        bool isEmergencyRequest = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
   "Getting available workers for request {RequestId}: specialization={Specialization}, date={Date}, emergency={Emergency}",
requestId, requiredSpecialization, preferredDate, isEmergencyRequest);

     // ? Query with enum
 var query = new GetAvailableWorkersQuery(preferredDate)
    {
       RequiredSpecialization = requiredSpecialization,
            IsEmergencyRequest = isEmergencyRequest,
            MaxWorkers = _settings.MaxAvailableWorkers,
            LookAheadDays = _settings.BookingLookAheadDays
        };

        var workerAssignments = await _mediator.Send(query, cancellationToken);
        var attemptCounts = new { primary = workerAssignments.Count, fallback = 0, any = 0 };

        // Fallback 1: Try General Maintenance
   if (workerAssignments.Count == 0 && requiredSpecialization != WorkerSpecialization.GeneralMaintenance)
      {
  _logger.LogDebug("No specialized workers, trying General Maintenance fallback");

            var fallbackQuery = new GetAvailableWorkersQuery(preferredDate)
    {
       RequiredSpecialization = WorkerSpecialization.GeneralMaintenance,
             IsEmergencyRequest = isEmergencyRequest,
   MaxWorkers = _settings.MaxAvailableWorkers,
 LookAheadDays = _settings.BookingLookAheadDays
       };

            workerAssignments = await _mediator.Send(fallbackQuery, cancellationToken);
    attemptCounts = attemptCounts with { fallback = workerAssignments.Count };
        }

        // Fallback 2: Try any available
        if (workerAssignments.Count == 0)
        {
      _logger.LogDebug("No General Maintenance workers, trying any available");

 var anyWorkersQuery = new GetAvailableWorkersQuery(preferredDate)
            {
    RequiredSpecialization = null, // Any specialization
    IsEmergencyRequest = isEmergencyRequest,
        MaxWorkers = _settings.MaxAvailableWorkers,
        LookAheadDays = _settings.BookingLookAheadDays
   };

      workerAssignments = await _mediator.Send(anyWorkersQuery, cancellationToken);
     attemptCounts = attemptCounts with { any = workerAssignments.Count };
  }

      // Map to DTO
        var result = workerAssignments.Select(w => new WorkerOptionDto
        {
            Id = w.WorkerId,
          Email = w.WorkerEmail,
 FullName = w.WorkerName,
            Specialization = w.Specialization, // Enum
   IsAvailable = w.IsAvailable,
            NextAvailableDate = w.NextAvailableDate ?? preferredDate,
      ActiveAssignmentsCount = w.CurrentWorkload,
        BookedDates = w.BookedDates,
            PartiallyBookedDates = w.PartiallyBookedDates,
            AvailabilityScore = w.AvailabilityScore
        }).ToList();

        _logger.LogInformation(
            "Found {Count} workers (primary={Primary}, fallback={Fallback}, any={Any})",
            result.Count, attemptCounts.primary, attemptCounts.fallback, attemptCounts.any);

 return result;
    }

    // ? DELETE: Remove DetermineRequiredSpecialization() - now in domain service
}
```

---

### ? Phase 7: Update DTOs

**File**: `Application/Interfaces/IWorkerService.cs`

```csharp
public interface IWorkerService
{
    Task<List<WorkerOptionDto>> GetAvailableWorkersForRequestAsync(
  Guid requestId,
        WorkerSpecialization requiredSpecialization, // ? CHANGED: string ? enum
        DateTime preferredDate,
  bool isEmergencyRequest = false,
 CancellationToken cancellationToken = default);

    Task<WorkerAssignmentContextDto> GetAssignmentContextAsync(
        Guid requestId,
   CancellationToken cancellationToken = default);
}

public class WorkerOptionDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public WorkerSpecialization Specialization { get; set; } // ? CHANGED: string ? enum
    public bool IsAvailable { get; set; }
    public DateTime? NextAvailableDate { get; set; }
    public int ActiveAssignmentsCount { get; set; }
    public List<DateTime> BookedDates { get; set; } = new();
    public List<DateTime> PartiallyBookedDates { get; set; } = new();
    public int AvailabilityScore { get; set; }
}

public class WorkerAssignmentContextDto
{
    public TenantRequestDto Request { get; set; } = new();
    public List<WorkerOptionDto> AvailableWorkers { get; set; } = new();
    public List<DateTime> SuggestedDates { get; set; } = new();
    public bool IsEmergencyRequest { get; set; }
    public WorkerSpecialization RequiredSpecialization { get; set; } // ? NEW
}
```

---

### ? Phase 8: Update Queries

**File**: `Application/Queries/Workers/GetAvailableWorkers/GetAvailableWorkersQuery.cs`

```csharp
public class GetAvailableWorkersQuery : IQuery<List<WorkerAssignmentDto>>
{
    public DateTime ServiceDate { get; }
    public WorkerSpecialization? RequiredSpecialization { get; set; } // ? CHANGED: string ? enum
    public bool IsEmergencyRequest { get; set; }
    public int MaxWorkers { get; set; } = 10;
    public int LookAheadDays { get; set; } = 30;

    public GetAvailableWorkersQuery(DateTime serviceDate)
    {
 ServiceDate = serviceDate;
    }
}
```

---

### ? Phase 9: Update Query Handler

**File**: `Application/Queries/Workers/GetAvailableWorkers/GetAvailableWorkersQueryHandler.cs`

```csharp
public class GetAvailableWorkersQueryHandler : IQueryHandler<GetAvailableWorkersQuery, List<WorkerAssignmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly SpecializationDeterminationService _specializationService; // NEW
    private readonly ILogger<GetAvailableWorkersQueryHandler> _logger;

    public GetAvailableWorkersQueryHandler(
        IApplicationDbContext context,
        SpecializationDeterminationService specializationService, // NEW
        ILogger<GetAvailableWorkersQueryHandler> logger)
    {
        _context = context;
_specializationService = specializationService; // NEW
        _logger = logger;
    }

    public async Task<List<WorkerAssignmentDto>> Handle(
     GetAvailableWorkersQuery request,
   CancellationToken cancellationToken)
    {
        var targetDate = request.ServiceDate;

        _logger.LogInformation(
     "Getting available workers for {Date}, specialization={Specialization}, emergency={Emergency}",
            targetDate, request.RequiredSpecialization, request.IsEmergencyRequest);

// Load workers with assignments
    var workersQuery = _context.Workers
        .Include(w => w.Assignments)
      .Where(w => w.IsActive);

        // ? UPDATED: Filter by enum specialization
   if (request.RequiredSpecialization.HasValue)
  {
   var requiredSpec = request.RequiredSpecialization.Value;
     
        workersQuery = workersQuery.Where(w =>
       w.Specialization == requiredSpec ||
          w.Specialization == WorkerSpecialization.GeneralMaintenance);
        }

  var workers = await workersQuery.ToListAsync(cancellationToken);

   _logger.LogInformation("Loaded {Count} active workers", workers.Count);

        if (!workers.Any())
        {
  _logger.LogWarning(
      "No active workers found for specialization {Specialization}",
                request.RequiredSpecialization);
            return new List<WorkerAssignmentDto>();
        }

  // Calculate availability
        var startDate = DateTime.Today;
     var endDate = DateTime.Today.AddDays(request.LookAheadDays);

        var workerSummaries = workers
            .Select(w => WorkerAvailabilitySummary.CreateFromWorker(
   w,
  startDate,
    endDate,
       targetDate,
      request.IsEmergencyRequest))
        .ToList();

        // Order by availability and take top N
        var orderedWorkers = workerSummaries
            .OrderBy(s => s.AvailabilityScore)
            .ThenBy(s => s.CurrentWorkload)
       .ThenBy(s => s.WorkerName)
            .Take(request.MaxWorkers)
   .ToList();

        // Map to DTO
        var result = orderedWorkers.Select(s => new WorkerAssignmentDto
        {
   WorkerId = s.WorkerId,
            WorkerName = s.WorkerName,
      WorkerEmail = s.WorkerEmail,
            Specialization = s.Specialization, // Enum
       IsAvailable = s.IsActive,
      CurrentWorkload = s.CurrentWorkload,
            NextAvailableDate = s.NextFullyAvailableDate,
         BookedDates = s.BookedDates.ToList(),
    PartiallyBookedDates = s.PartiallyBookedDates.ToList(),
   AvailabilityScore = s.AvailabilityScore
        }).ToList();

     _logger.LogInformation("Returning {Count} worker assignments", result.Count);

        return result;
    }
}
```

---

### ? Phase 10: Update Domain Service (UnitSchedulingService)

**File**: `Domain/Services/UnitSchedulingService.cs`

```csharp
public class UnitSchedulingService
{
    private readonly SpecializationDeterminationService _specializationService; // NEW

    public UnitSchedulingService(SpecializationDeterminationService specializationService)
    {
        _specializationService = specializationService;
    }

    public UnitSchedulingValidationResult ValidateWorkerAssignment(
    Guid requestId,
        string propertyCode,
        string unitNumber,
      DateTime scheduledDate,
        string workerEmail,
 WorkerSpecialization workerSpecialization, // ? CHANGED: string ? enum
        WorkerSpecialization requiredSpecialization, // ? CHANGED: string ? enum
        bool isEmergency,
        IEnumerable<ExistingAssignment> existingAssignments)
    {
  var result = new UnitSchedulingValidationResult();

        // Rule 1: Check specialization match
        if (!_specializationService.CanHandleWork(workerSpecialization, requiredSpecialization))
        {
    result.IsValid = false;
            result.ErrorMessage = $"Worker specialized in {workerSpecialization} cannot handle {requiredSpecialization} work";
            result.ConflictType = SchedulingConflictType.SpecializationMismatch;
 return result;
  }

     // ... rest of validation logic ...
    }

    // ? DELETE: Remove DoesSpecializationMatch() - now uses domain service
    // ? DELETE: Remove NormalizeSpecialization() - now uses domain service
}

public class ExistingAssignment
{
    public Guid TenantRequestId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
 public string UnitNumber { get; set; } = string.Empty;
    public string WorkerEmail { get; set; } = string.Empty;
    public WorkerSpecialization WorkerSpecialization { get; set; } // ? CHANGED: string ? enum
    public string WorkOrderNumber { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsEmergency { get; set; } = false;
}
```

---

### ? Phase 11: Database Migration

**File**: `Infrastructure/Migrations/[Timestamp]_UpdateWorkerSpecializationToEnum.cs`

```csharp
public partial class UpdateWorkerSpecializationToEnum : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Option 1: If starting fresh or can recreate
    migrationBuilder.AlterColumn<int>(
            name: "Specialization",
    table: "Workers",
        type: "int",
       nullable: false,
  oldClrType: typeof(string),
       oldType: "nvarchar(100)",
       oldNullable: true);

        // Option 2: If need to preserve data
        // Step 1: Add new column
        migrationBuilder.AddColumn<int>(
            name: "SpecializationEnum",
     table: "Workers",
       type: "int",
          nullable: false,
            defaultValue: 0); // GeneralMaintenance

   // Step 2: Migrate data
        migrationBuilder.Sql(@"
            UPDATE Workers
     SET SpecializationEnum = 
      CASE Specialization
           WHEN 'Plumbing' THEN 1
  WHEN 'Electrical' THEN 2
    WHEN 'HVAC' THEN 3
       WHEN 'Carpentry' THEN 4
        WHEN 'Painting' THEN 5
       WHEN 'Locksmith' THEN 6
              WHEN 'Appliance Repair' THEN 7
   ELSE 0 -- General Maintenance
    END
        ");

        // Step 3: Drop old column
        migrationBuilder.DropColumn(
            name: "Specialization",
          table: "Workers");

        // Step 4: Rename new column
        migrationBuilder.RenameColumn(
            name: "SpecializationEnum",
            table: "Workers",
            newName: "Specialization");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse migration
        migrationBuilder.AlterColumn<string>(
   name: "Specialization",
     table: "Workers",
     type: "nvarchar(100)",
nullable: true,
            oldClrType: typeof(int),
 oldType: "int");
    }
}
```

---

### ? Phase 12: Update EF Core Configuration

**File**: `Infrastructure/Persistence/Configurations/WorkerConfiguration.cs`

```csharp
public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("Workers");

        builder.HasKey(w => w.Id);

        // ? UPDATED: Configure enum
        builder.Property(w => w.Specialization)
  .HasConversion<int>() // Store as int in database
         .IsRequired();

        builder.OwnsOne(w => w.ContactInfo, contactInfo =>
        {
   contactInfo.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
       contactInfo.Property(c => c.LastName).HasMaxLength(100).IsRequired();
          contactInfo.Property(c => c.EmailAddress).HasMaxLength(254).IsRequired();
    contactInfo.Property(c => c.PhoneNumber).HasMaxLength(20);
        });

 builder.Property(w => w.IsActive)
     .IsRequired();

        builder.Property(w => w.Notes)
   .HasMaxLength(2000);

        builder.HasMany(w => w.Assignments)
            .WithOne()
         .HasForeignKey("WorkerId")
   .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### ? Phase 13: Update UI (Register Page)

**File**: `WebUI/Pages/Account/Register.cshtml`

```razor
<div id="workerFields" class="role-specific-fields" style="display: none;">
    <div class="mb-3">
        <label asp-for="Register.WorkerSpecialization" class="form-label">Specialization</label>
  <select asp-for="Register.WorkerSpecialization" class="form-select">
            <option value="">Select specialization...</option>
            <option value="@((int)WorkerSpecialization.Plumbing)">Plumbing</option>
            <option value="@((int)WorkerSpecialization.Electrical)">Electrical</option>
   <option value="@((int)WorkerSpecialization.HVAC)">HVAC</option>
            <option value="@((int)WorkerSpecialization.GeneralMaintenance)">General Maintenance</option>
  <option value="@((int)WorkerSpecialization.Carpentry)">Carpentry</option>
<option value="@((int)WorkerSpecialization.Painting)">Painting</option>
            <option value="@((int)WorkerSpecialization.Locksmith)">Locksmith</option>
          <option value="@((int)WorkerSpecialization.ApplianceRepair)">Appliance Repair</option>
        </select>
    </div>
</div>
```

---

### ? Phase 14: Update DI Registration

**File**: `Domain/DependencyInjection.cs`

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Pure Domain Services
        services.AddScoped<PropertyPolicyService>();
 services.AddScoped<UserRoleDomainService>();
        services.AddScoped<SpecializationDeterminationService>(); // ? NEW

        // Domain Services with abstractions
        services.AddScoped<TenantRequestStatusPolicy>();
        services.AddScoped<TenantRequestUrgencyPolicy>();
 services.AddScoped<RequestAuthorizationPolicy>();
        services.AddScoped<ITenantRequestSubmissionPolicy, TenantRequestSubmissionPolicy>();
 services.AddScoped<UnitSchedulingService>();
    services.AddScoped<AuthorizationDomainService>();

 return services;
    }
}
```

---

### ? Phase 15: Delete Old Code

**Files to Delete/Update**:

1. ? Delete `Application/Common/Configuration/SpecializationSettings.cs`
2. ? Delete `Application/Common/Configuration/SpecializationMapping.cs`
3. ? Delete `WebUI/appsettings.WorkerService.json` ? Remove Specialization section
4. ? Delete `Worker.DetermineRequiredSpecialization()` static method
5. ? Delete `Worker.NormalizeSpecialization()` private method
6. ? Delete `WorkerService.DetermineRequiredSpecialization()` private method
7. ? Delete `UnitSchedulingService.NormalizeSpecialization()` private method
8. ? Delete `UnitSchedulingService.DoesSpecializationMatch()` private method

---

## Implementation Order

### Priority 1: Domain Layer (Core) ?
1. Create `WorkerSpecialization` enum
2. Create `SpecializationDeterminationService`
3. Update `Worker` entity
4. Update domain events
5. Register domain service in DI
6. Update domain tests

### Priority 2: Infrastructure (Data) ?
7. Create database migration
8. Update EF Core configuration
9. Test migration on dev database

### Priority 3: Application Layer ?
10. Update `ScheduleServiceWorkCommandHandler` (KEY CHANGE)
11. Update `WorkerService`
12. Update `GetAvailableWorkersQuery` and handler
13. Update DTOs
14. Update `UnitSchedulingService`

### Priority 4: Cleanup ?
15. Delete configuration classes
16. Delete old methods from Worker entity
17. Delete old methods from WorkerService
18. Delete old methods from UnitSchedulingService
19. Update appsettings.json

### Priority 5: UI & Tests ?
20. Update Register page
21. Update all tests
22. Manual testing

---

## Testing Strategy

### Unit Tests

**Domain Service Tests**:
```csharp
[Theory]
[InlineData("Leaking faucet", "Water dripping", WorkerSpecialization.Plumbing)]
[InlineData("Outlet sparking", "Power issue", WorkerSpecialization.Electrical)]
[InlineData("Heater broken", "HVAC failure", WorkerSpecialization.HVAC)]
public void DetermineRequiredSpecialization_ShouldReturnCorrectEnum(
    string title, string description, WorkerSpecialization expected)
{
    // Arrange
    var service = new SpecializationDeterminationService();
    
    // Act
    var result = service.DetermineRequiredSpecialization(title, description);
    
    // Assert
    result.Should().Be(expected);
}

[Fact]
public void CanHandleWork_GeneralMaintenance_ShouldHandleAnything()
{
    // Arrange
    var service = new SpecializationDeterminationService();
    
    // Act & Assert
    service.CanHandleWork(
        WorkerSpecialization.GeneralMaintenance,
        WorkerSpecialization.Plumbing).Should().BeTrue();
    
    service.CanHandleWork(
    WorkerSpecialization.GeneralMaintenance,
        WorkerSpecialization.Electrical).Should().BeTrue();
}

[Fact]
public void CanHandleWork_Plumber_ShouldNotHandleElectrical()
{
    // Arrange
    var service = new SpecializationDeterminationService();
    
    // Act & Assert
    service.CanHandleWork(
        WorkerSpecialization.Plumbing,
        WorkerSpecialization.Electrical).Should().BeFalse();
}
```

**Worker Entity Tests**:
```csharp
[Fact]
public void SetSpecialization_ShouldUpdateToEnum()
{
    // Arrange
    var worker = CreateTestWorker();
  
    // Act
    worker.SetSpecialization(WorkerSpecialization.Plumbing);
    
    // Assert
  worker.Specialization.Should().Be(WorkerSpecialization.Plumbing);
}

[Fact]
public void HasSpecializedSkills_ShouldUseService()
{
    // Arrange
    var worker = CreateTestWorker();
    worker.SetSpecialization(WorkerSpecialization.Plumbing);
    var service = new SpecializationDeterminationService();
    
    // Act & Assert
    worker.HasSpecializedSkills(
   WorkerSpecialization.Plumbing,
        service).Should().BeTrue();
    
    worker.HasSpecializedSkills(
      WorkerSpecialization.Electrical,
        service).Should().BeFalse();
}
```

---

## Benefits

### ? Type Safety
```csharp
// Before (stringly-typed)
worker.SetSpecialization("Plumber"); // Typo? Runtime error!

// After (type-safe)
worker.SetSpecialization(WorkerSpecialization.Plumbing); // Compile-time safe!
```

### ? Single Source of Truth
- All specialization logic in Domain layer
- One enum definition
- One determination service
- No duplication

### ? Proper Timing
```csharp
// ? Specialization determined WHEN SCHEDULING (correct)
ScheduleServiceWorkCommandHandler.Handle()
    ? SpecializationDeterminationService.DetermineRequiredSpecialization()
    ? Worker.ValidateCanBeAssignedToRequest()

// ? NOT when tenant creates request (tenant doesn't know specializations)
// ? NOT when viewing assignment context (too late)
```

### ? Domain-Driven Design
- Business logic in domain layer
- Domain service for cross-entity logic
- Rich domain model (enum > string)
- Domain events with type safety

---

## Migration Path

### Step 1: Add enum alongside string (compatibility)
```csharp
public string? SpecializationLegacy { get; private set; }
public WorkerSpecialization Specialization { get; private set; }
```

### Step 2: Migrate data
```sql
UPDATE Workers
SET Specialization = 
    CASE SpecializationLegacy
     WHEN 'Plumbing' THEN 1
        WHEN 'Electrical' THEN 2
        -- etc.
    END
```

### Step 3: Remove legacy property
```csharp
// Delete SpecializationLegacy
```

---

## Rollback Plan

If issues arise:
1. Keep migration reversible (Down method)
2. Database backup before migration
3. Feature flag for new logic
4. Gradual rollout (dev ? staging ? production)

---

## Summary

### Changes Overview

| Component | Before | After |
|-----------|--------|-------|
| **Specialization Type** | `string` | `WorkerSpecialization enum` |
| **Determination Logic** | 3 places | 1 Domain Service |
| **Normalization** | 3 copies | Domain Service |
| **When Determined** | Assignment Context | Scheduling Command |
| **Configuration** | appsettings.json | Domain Code |
| **Type Safety** | ? None | ? Compile-time |
| **Duplication** | ? 3× | ? None |

### Key Decisions

? **Enum** - Type-safe vs. magic strings  
? **Domain Service** - Proper DDD pattern  
? **Scheduling Time** - Correct business timing  
? **Delete Configuration** - Business logic in domain  

---

## Action Items

### Immediate (Priority 1)
- [ ] Create `WorkerSpecialization` enum
- [ ] Create `SpecializationDeterminationService`
- [ ] Update `Worker` entity
- [ ] Update domain events
- [ ] Write domain tests

### Next (Priority 2)
- [ ] Create database migration
- [ ] Update EF Core configuration
- [ ] Test migration

### Then (Priority 3)
- [ ] Update `ScheduleServiceWorkCommandHandler`
- [ ] Update `WorkerService`
- [ ] Update queries and DTOs
- [ ] Update `UnitSchedulingService`

### Finally (Priority 4)
- [ ] Delete old configuration
- [ ] Delete duplicate methods
- [ ] Update UI
- [ ] Full integration testing

---

**Estimated Effort**: 6-8 hours  
**Risk**: Medium (database migration, but reversible)  
**Benefit**: High (type safety, clean architecture, maintainability)  

**Recommendation**: ? **PROCEED WITH IMPLEMENTATION**
