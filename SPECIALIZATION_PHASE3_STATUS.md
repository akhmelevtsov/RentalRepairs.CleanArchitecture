# Specialization Domain Refactoring - Phase 3 Status

**Date**: 2024  
**Status**: ?? **IN PROGRESS** - Application Layer Updates  
**Build**: ? **Failed** (Expected - Worker entity not yet updated)

---

## Phase 3: Application Layer - 60% Complete ??

### What Was Completed ?

**1. Command Handler Updated** ?
- `ScheduleServiceWorkCommandHandler.cs`
- Injects `SpecializationDeterminationService`
- **KEY CHANGE**: Determines specialization DURING SCHEDULING
- Uses domain service instead of static Worker method
- Logs determined specialization

**2. WorkerService Updated** ?
- `WorkerService.cs`
- Injects `SpecializationDeterminationService`
- Uses domain service to determine specialization
- Method signatures updated to use `WorkerSpecialization` enum
- Removed `DetermineRequiredSpecialization` private method (now in domain)

**3. IWorkerService Interface Updated** ?
- Changed `string requiredSpecialization` ? `WorkerSpecialization requiredSpecialization`
- Added `RequiredSpecialization` property to `WorkerAssignmentContextDto`
- Updated `WorkerOptionDto.Specialization` to enum

**4. Query Updated** ?
- `GetAvailableWorkersQuery.cs`
- Changed `RequiredSpecialization` from `string?` ? `WorkerSpecialization?`
- Updated `WorkerAssignmentDto.Specialization` to enum

**5. Query Handler Updated** ?
- `GetAvailableWorkersQueryHandler.cs`
- Enum-based filtering logic
- Compares `WorkerSpecialization` enums instead of strings

---

### Current Compilation Errors (Expected) ?

```
1. ScheduleServiceWorkCommandHandler.cs:80
   - worker.Specialization is still string
   - Needs Worker entity update (Phase 2)

2. GetAvailableWorkersQueryHandler.cs:49-50
   - w.Specialization == requiredSpec (string vs enum)
   - Needs Worker entity update (Phase 2)

3. GetAvailableWorkersQueryHandler.cs:100
   - s.Specialization = s.Specialization (string ? enum)
   - Needs WorkerAvailabilitySummary update
```

**Root Cause**: Worker entity still uses `string? Specialization`

---

### Remaining Work ??

#### Phase 2 Tasks (Should Have Been Done First!)

We need to go back and complete Phase 2:

**1. Update Worker Entity** ? NOT DONE
```csharp
// Current
public string? Specialization { get; private set; }

// Should be
public WorkerSpecialization Specialization { get; private set; } = WorkerSpecialization.GeneralMaintenance;
```

**2. Update Domain Methods** ? NOT DONE
- Delete `Worker.DetermineRequiredSpecialization()` static method
- Delete `Worker.NormalizeSpecialization()` private method
- Update `Worker.SetSpecialization()` to accept enum
- Update `Worker.HasSpecializedSkills()` to use domain service

**3. Update Value Objects** ? NOT DONE
- `WorkerAvailabilitySummary.Specialization` from string ? enum

**4. Update Domain Events** ? NOT DONE
- `WorkerSpecializationChangedEvent` to use enum

**5. Database Migration** ? NOT DONE
- Create migration to change column from nvarchar ? int
- Migrate existing data

**6. EF Core Configuration** ? NOT DONE
- Update `WorkerConfiguration` to use enum converter

---

## Correct Implementation Order

### ? What We Did (Wrong Order)
1. ? Phase 1: Domain Service (correct)
2. ? **Phase 3**: Application Layer (too early!)
3. ?? **Phase 2**: Worker Entity (should be #2)

### ? What We Should Do (Correct Order)
1. ? **Phase 1**: Domain Service + Enum ? DONE
2. ?? **Phase 2**: Worker Entity + Infrastructure (DO NEXT)
3. ?? **Phase 3**: Application Layer (resume after Phase 2)
4. ?? **Phase 4**: Cleanup
5. ?? **Phase 5**: UI & Tests

---

## Decision Point

### Option 1: Complete Phase 2 First ? RECOMMENDED

**Advantages**:
- Proper bottom-up dependency order
- Worker entity is foundation for everything
- Database migration done early
- Application layer can use real enums

**Steps**:
1. Update Worker entity to use enum
2. Update Worker domain methods
3. Update value objects
4. Create database migration
5. Update EF Core configuration
6. Fix compilation errors from Phase 3

### Option 2: Revert Phase 3, Do Phase 2 ? NOT RECOMMENDED

**Disadvantages**:
- Lose all Phase 3 work
- Have to redo it later
- More complex

---

## Recommended Next Steps

### Step 1: Update Worker Entity

**File**: `Domain/Entities/Worker.cs`

```csharp
using RentalRepairs.Domain.Enums;

public class Worker : BaseEntity, IAggregateRoot
{
    // Change property type
    public WorkerSpecialization Specialization { get; private set; } = WorkerSpecialization.GeneralMaintenance;
    
    // Update method signature
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
    
    // Use domain service for skills check
    public bool HasSpecializedSkills(
        WorkerSpecialization requiredSpecialization,
      SpecializationDeterminationService service)
  {
  return service.CanHandleWork(Specialization, requiredSpecialization);
    }
    
    // DELETE these methods (now in SpecializationDeterminationService)
    // public static string DetermineRequiredSpecialization(...) DELETE
    // private static string NormalizeSpecialization(...) DELETE
}
```

### Step 2: Update WorkerAvailabilitySummary

**File**: `Domain/ValueObjects/WorkerAvailabilitySummary.cs`

```csharp
using RentalRepairs.Domain.Enums;

public sealed class WorkerAvailabilitySummary : ValueObject
{
    public WorkerSpecialization Specialization { get; init; } // Changed from string
    
    public static WorkerAvailabilitySummary CreateFromWorker(...)
    {
        return new WorkerAvailabilitySummary
  {
            // ...existing code...
     Specialization = worker.Specialization, // Now enum
        };
    }
}
```

### Step 3: Update Domain Events

**File**: `Domain/Events/Workers/WorkerSpecializationChangedEvent.cs`

```csharp
using RentalRepairs.Domain.Enums;

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

### Step 4: Create Database Migration

```bash
dotnet ef migrations add UpdateWorkerSpecializationToEnum --project Infrastructure --startup-project WebUI
```

### Step 5: Update EF Core Configuration

**File**: `Infrastructure/Persistence/Configurations/WorkerConfiguration.cs`

```csharp
builder.Property(w => w.Specialization)
    .HasConversion<int>() // Store as int in database
    .IsRequired();
```

---

## Files Modified So Far

### Phase 1 (Complete) ?
1. ? `Domain/Enums/WorkerSpecialization.cs` (created)
2. ? `Domain/Services/SpecializationDeterminationService.cs` (created)
3. ? `Domain/DependencyInjection.cs` (updated)
4. ? `Domain.Tests/Services/SpecializationDeterminationServiceTests.cs` (created)

### Phase 3 (Incomplete - needs Phase 2) ??
5. ?? `Application/Commands/.../ScheduleServiceWorkCommandHandler.cs` (updated)
6. ?? `Application/Services/WorkerService.cs` (updated)
7. ?? `Application/Interfaces/IWorkerService.cs` (updated)
8. ?? `Application/Queries/.../GetAvailableWorkersQuery.cs` (updated)
9. ?? `Application/Queries/.../GetAvailableWorkersQueryHandler.cs` (updated)

### Phase 2 (Not Started) ?
- ? `Domain/Entities/Worker.cs` (needs update)
- ? `Domain/ValueObjects/WorkerAvailabilitySummary.cs` (needs update)
- ? `Domain/Events/Workers/WorkerSpecializationChangedEvent.cs` (needs update)
- ? `Infrastructure/Persistence/Configurations/WorkerConfiguration.cs` (needs update)
- ? Database migration (needs creation)

---

## Summary

**Current Status**:
- ? Phase 1: 100% Complete
- ? Phase 2: 0% Complete (should be next)
- ?? Phase 3: 60% Complete (needs Phase 2 first)

**Build Status**: ? Failed (4 compilation errors - all related to Worker.Specialization still being string)

**Recommendation**: Complete Phase 2 (Worker Entity + Infrastructure) before finishing Phase 3

**Next Action**: Update Worker entity to use `WorkerSpecialization` enum

---

**Progress**: 35% Complete (Phase 1 done, Phase 3 started, Phase 2 pending)
