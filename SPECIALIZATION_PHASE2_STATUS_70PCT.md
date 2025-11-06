# Specialization Phase 2 - Status Report

**Date**: 2024  
**Status**: ?? **70% COMPLETE** - Domain entities updated, tests/queries need fixes  
**Build**: ? **Failed** (45 compilation errors - expected during migration)

---

## Phase 2: Domain Entity + Infrastructure - Progress

### ? Completed (70%)

**1. Domain Layer Updates** ?
- ? `Worker.cs` - Updated to use `WorkerSpecialization` enum
- ? `WorkerAvailabilitySummary.cs` - Updated to use enum
- ? `WorkerSpecializationChangedEvent.cs` - Updated to use enum
- ? `WorkerBySpecializationSpecification.cs` - Updated to use enum
- ? Removed old methods: `DetermineRequiredSpecialization()`, `NormalizeSpecialization()`, `HasSpecializedSkills()`

**2. Application Layer Integration** ?
- ? `NotifyPartiesService.cs` - Injects SpecializationDeterminationService
- ? Uses domain service to get display names from enum

**3. Domain Tests Updated** ? (Partial)
- ? `WorkerTests.cs` - Updated SetSpecialization test to use enum
- ? Added using for WorkerSpecialization enum
- ? Removed obsolete tests for deleted methods

---

### ?? Remaining Work (30%)

#### Category 1: Query Handlers (5 files)
1. ? `GetWorkersQueryHandler.cs` - Update Specialization filter + DTO mapping
2. ? `GetWorkerByEmailQueryHandler.cs` - Update DTO mapping
3. ? `GetWorkerByIdQueryHandler.cs` - Update DTO mapping
4. ? `WorkerRepository.cs` - Update query filter

**Fix Pattern**:
```csharp
// Before
query = query.Where(w => w.Specialization == request.Specialization); // string

// After - Option 1: Parse string to enum
var specializationEnum = _specializationService.ParseSpecialization(request.Specialization);
query = query.Where(w => w.Specialization == specializationEnum);

// After - Option 2: Update query to accept enum
public string? Specialization { get; set; } // Change to WorkerSpecialization?
```

#### Category 2: Command Handlers (2 files)
1. ? `RegisterWorkerCommandHandler.cs` - Parse string to enum before SetSpecialization
2. ? `UpdateWorkerSpecializationCommandHandler.cs` - Parse string to enum

**Fix Pattern**:
```csharp
// Before
worker.SetSpecialization(request.Specialization); // string

// After
var specializationEnum = _specializationService.ParseSpecialization(request.Specialization);
worker.SetSpecialization(specializationEnum);
```

#### Category 3: Event Handlers (2 files)
1. ? `WorkerRegisteredEventHandler.cs` - Use GetDisplayName() for logging
2. ? `WorkerSpecializationChangedEventHandler.cs` - Use GetDisplayName() for notifications

**Fix Pattern**:
```csharp
// Before
worker.Specialization ?? "Not specified" // Can't use ?? with enum

// After
_specializationService.GetDisplayName(worker.Specialization)
```

#### Category 4: Domain Tests (6 files)
1. ? `WorkerTests.cs` - Remove/update obsolete tests (HasSpecializedSkills, DetermineRequiredSpecialization)
2. ? `WorkerAssignmentServiceTests.cs` - Use SpecializationDeterminationService instead of Worker static methods
3. ? `WorkerSpecializationFilteringTests.cs` - Update to use enum + domain service
4. ? `WorkerBookingAvailabilityTests.cs` - Update SetSpecialization calls
5. ? `WorkerAvailabilitySummaryTests.cs` - Update assertions to use enum
6. ? `WorkerTestDataBuilder.cs` - Update fluent API to use enum

**Fix Pattern**:
```csharp
// Before
worker.SetSpecialization("Plumbing"); // string

// After
worker.SetSpecialization(WorkerSpecialization.Plumbing); // enum
```

#### Category 5: Infrastructure Tests (2 files)
1. ? `DatabaseAuditingTests.cs` - Update SetSpecialization call
2. ? `DomainEventPublishingTests.cs` - Update SetSpecialization + assertions

#### Category 6: DTOs (3 files)
1. ? `WorkerDto` in `TenantDto.cs` - Update Specialization property type
2. ? Query DTOs - Update to use enum or string with notes

**Decision needed**: Should DTOs use enum or string?
- **Option A**: Use enum (type-safe, requires JSON converter)
- **Option B**: Use string (backward compatible, convert at boundaries)

---

## Compilation Errors Breakdown

### By Category:
- **Query Handlers**: 4 errors (comparison + mapping)
- **Command Handlers**: 3 errors (SetSpecialization calls)
- **Event Handlers**: 7 errors (?? operator + method signatures)
- **Domain Tests**: 26 errors (obsolete method calls + SetSpecialization)
- **Infrastructure Tests**: 3 errors (SetSpecialization + assertions)
- **Test Builders**: 1 error (SetSpecialization)
- **Specification**: 1 error ? FIXED

**Total**: 45 errors (down from 52)

### By Error Type:
- `Cannot convert from 'string' to 'WorkerSpecialization'`: 22 errors
- `Worker does not contain definition for 'X'`: 15 errors (deleted methods)
- `Cannot implicitly convert WorkerSpecialization to string`: 4 errors
- `Operator '??' cannot be applied`: 2 errors
- `Operator '==' cannot be applied`: 1 error
- `Missing parameter 'specializationService'`: 2 errors

---

## Quick Win Fixes (Can Be Done Now)

### 1. Update WorkerTestDataBuilder (1 file)
```csharp
private WorkerSpecialization? _specialization = null; // Change from string

public WorkerTestDataBuilder WithSpecialization(WorkerSpecialization specialization)
{
    _specialization = specialization;
    return this;
}

public WorkerTestDataBuilder AsPlumber() => WithSpecialization(WorkerSpecialization.Plumbing);
// etc...
```

### 2. Update Event Handlers (2 files)
Inject `SpecializationDeterminationService` and use `GetDisplayName()`:

```csharp
// WorkerRegisteredEventHandler
_logger.LogInformation(
 "Processing WorkerRegisteredEvent for worker {WorkerEmail} with specialization {Specialization}",
    worker.ContactInfo.EmailAddress,
    _specializationService.GetDisplayName(worker.Specialization));
```

### 3. Update Command Handlers (2 files)
Inject `SpecializationDeterminationService` and parse strings:

```csharp
// RegisterWorkerCommandHandler
if (!string.IsNullOrEmpty(request.Specialization))
{
    var specializationEnum = _specializationService.ParseSpecialization(request.Specialization);
    worker.SetSpecialization(specializationEnum);
}
```

### 4. Delete Obsolete Tests (3 files)
Remove tests for deleted Worker methods - they're now tested in SpecializationDeterminationServiceTests

---

## Database Migration (Not Started) ?

**Required Steps**:
1. Create EF Core migration
2. Data migration strategy
3. Update `WorkerConfiguration.cs`

**File**: `Infrastructure/Persistence/Configurations/WorkerConfiguration.cs`
```csharp
builder.Property(w => w.Specialization)
    .HasConversion<int>() // Store as int
  .IsRequired();
```

**Migration SQL** (approximate):
```sql
-- Step 1: Add new column
ALTER TABLE Workers ADD SpecializationEnum INT NOT NULL DEFAULT 0;

-- Step 2: Migrate data
UPDATE Workers SET SpecializationEnum =
    CASE Specialization
        WHEN 'Plumbing' THEN 1
        WHEN 'Electrical' THEN 2
        -- etc...
        ELSE 0
    END;

-- Step 3: Drop old column
ALTER TABLE Workers DROP COLUMN Specialization;

-- Step 4: Rename
EXEC sp_rename 'Workers.SpecializationEnum', 'Specialization', 'COLUMN';
```

---

## Recommended Next Steps

### Step 1: Fix Test Builders (Low Hanging Fruit)
- Update `WorkerTestDataBuilder.cs` to use enum
- **Time**: 5 minutes
- **Impact**: Fixes ~10 test errors

### Step 2: Update Event Handlers
- Inject SpecializationDeterminationService
- Use `GetDisplayName()` for logging/notifications
- **Time**: 10 minutes
- **Impact**: Fixes 7 errors

### Step 3: Update Command Handlers
- Parse strings to enum before SetSpecialization
- **Time**: 10 minutes
- **Impact**: Fixes 3 errors

### Step 4: Clean Up Tests
- Remove obsolete test methods
- Update remaining tests to use enum
- **Time**: 30 minutes
- **Impact**: Fixes 26 errors

### Step 5: Update Query Handlers
- Parse filter strings to enum
- Update DTO mappings
- **Time**: 20 minutes
- **Impact**: Fixes 4 errors

### Step 6: Database Migration
- Create migration
- Test on dev database
- **Time**: 20 minutes
- **Impact**: Enables full integration

**Total Estimated Time**: ~2 hours to complete Phase 2

---

## Files Modified So Far (Phase 2)

### Domain Layer ?
1. ? `Domain/Entities/Worker.cs`
2. ? `Domain/ValueObjects/WorkerAvailabilitySummary.cs`
3. ? `Domain/Events/Workers/WorkerSpecializationChangedEvent.cs`
4. ? `Domain/Specifications/Workers/WorkerBySpecializationSpecification.cs`

### Application Layer ?
5. ? `Application/Services/NotifyPartiesService.cs`
6. ? `Application/Commands/.../ScheduleServiceWorkCommandHandler.cs` (temp workaround)

### Tests ??
7. ?? `Domain.Tests/Entities/WorkerTests.cs` (partial)

### Phase 3 Files (Already Done) ?
- `Application/Services/WorkerService.cs`
- `Application/Interfaces/IWorkerService.cs`
- `Application/Queries/.../GetAvailableWorkersQuery.cs`
- `Application/Queries/.../GetAvailableWorkersQueryHandler.cs`
- `Application/Commands/.../ScheduleServiceWorkCommandHandler.cs`

---

## Current Architecture State

### ? What Works:
1. Enum defined in Domain
2. Domain service for determination/validation
3. Worker entity uses enum internally
4. Phase 3 application layer ready for enums
5. Comprehensive domain service tests passing (84/84)

### ? What Needs Fixing:
1. Tests reference old Worker methods (deleted)
2. Command handlers pass strings to SetSpecialization
3. Query handlers map enum ? string for DTOs
4. Event handlers use ?? with enum
5. Database still stores string (migration needed)

---

## Summary

### Progress: 70% Complete

**Phase 1**: ? 100% Complete (Domain Service + Enum)  
**Phase 2**: ?? 70% Complete (Entity updated, tests/queries need fixes)  
**Phase 3**: ? 95% Complete (Application layer ready, awaiting Phase 2)  

**Build**: ? 45 errors (all fixable, clear patterns)  
**Risk**: Low (systematic refactoring)  
**Time to Complete**: ~2 hours  

---

## Next Session Action Plan

1. **Quick Wins** (30 min)
   - Fix test builders
   - Update event handlers
   - Update command handlers

2. **Test Cleanup** (30 min)
   - Delete obsolete tests
   - Update remaining tests

3. **Query Updates** (20 min)
   - Update query handlers
   - Fix DTO mappings

4. **Database** (20 min)
   - Create migration
   - Test migration

5. **Verification** (20 min)
   - Run all tests
   - Verify build
   - Integration testing

**Total**: ~2 hours to 100% completion

---

**Status**: Phase 2 is 70% done. Clear path forward with systematic fixes remaining.
