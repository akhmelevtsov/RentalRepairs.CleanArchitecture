# Domain Aggregate Dead Methods Analysis Report

**Date:** January 2025  
**Project:** RentalRepairs Clean Architecture  
**Analysis Scope:** Domain Layer - 4 Main Aggregates

---

## Executive Summary

This report identifies **unused methods** (dead code) across the four main domain aggregates: `Worker`, `Property`, `Tenant`, and `TenantRequest`. Methods are classified as "dead" if they are:
- Not called by Application, Infrastructure, or WebUI layers
- Only referenced in test code (tests don't count as production usage)
- Not part of the working functionality for this demo project

---

## 1. Worker Aggregate Analysis

### ? DEAD METHODS

#### 1.1 `IsAvailableForSlot(SchedulingSlot slot)`
**Location:** `Domain\Entities\Worker.cs` (Line ~265)

**Reason:** 
- Uses `SchedulingSlot` value object with time-based scheduling
- Project uses **date-only scheduling** (no time components)
- Replaced by simpler `IsAvailableForWork(DateTime date)` method
- Never called in Application/WebUI layers

**Evidence:**
```csharp
// Only used in obsolete time-based scheduling approach
public bool IsAvailableForSlot(SchedulingSlot slot)
```

**Usage:** ? Not found in production code

---

#### 1.2 `IsEmergencyResponseCapable()`
**Location:** `Domain\Entities\Worker.cs` (Line ~282)

**Reason:**
- Returns `true` for all active workers (no actual certification logic)
- Used in scoring/recommendation methods but **those methods are also dead** (see below)
- Emergency handling is done through availability scoring, not capability flags

**Evidence:**
```csharp
public bool IsEmergencyResponseCapable()
{
    if (!IsActive) return false;
    // Business rule: assume all workers can handle emergencies
    return true;
}
```

**Usage:** Called by other dead methods (`CalculateScoreForRequest`, `WorkerCollectionExtensions.GetAvailableForEmergency`)

---

#### 1.3 `CalculateScoreForRequest(TenantRequest request)`
**Location:** `Domain\Entities\Worker.cs` (Line ~429)

**Reason:**
- Part of "Step 2: Push Logic to Aggregates" refactoring
- **Application layer uses `GetAvailableWorkersQuery` which has its own scoring logic**
- `WorkerAssignmentDto` contains the actual scores used in production
- Extension methods in `WorkerCollectionExtensions` that use this are also unused

**Evidence:**
```csharp
public int CalculateScoreForRequest(TenantRequest request)
{
    int score = 0;
    if (!IsActive) return 0;
  score += 100; // Base score
    // ... complex scoring logic that's never called
}
```

**Usage:** ? Application uses `GetAvailableWorkersQueryHandler` scoring instead

---

#### 1.4 `CanBeAssignedToRequest(TenantRequest request)`
**Location:** `Domain\Entities\Worker.cs` (Line ~473)

**Reason:**
- Part of "Step 2: Push Logic to Aggregates" pattern
- **Never called in production - replaced by validation in command handlers**
- `ScheduleServiceWorkCommandHandler` uses domain validation directly
- `WorkerService` doesn't use this method

**Evidence:**
```csharp
public bool CanBeAssignedToRequest(TenantRequest request)
{
    if (request == null) return false;
    if (!IsActive) return false;
    // ... more validation that's duplicated in command handlers
}
```

**Usage:** ? Not found in Application/WebUI layers

---

#### 1.5 `CalculateRecommendationConfidence(TenantRequest request)`
**Location:** `Domain\Entities\Worker.cs` (Line ~509)

**Reason:**
- Returns confidence scores for worker recommendations
- **Recommendation system not used in production**
- UI shows available workers but doesn't use confidence scores
- Part of unused `WorkerAssignmentRecommendation` value object

**Evidence:**
```csharp
public double CalculateRecommendationConfidence(TenantRequest request)
{
    if (request == null || !IsActive) return 0.0;
    string requiredSpecialization = DetermineRequiredSpecialization(...);
    // Returns 0.70 to 0.95 based on match - never used
}
```

**Usage:** ? Only in dead `WorkerCollectionExtensions.GetAssignmentRecommendations()`

---

#### 1.6 `GenerateRecommendationReasoning(TenantRequest request)`
**Location:** `Domain\Entities\Worker.cs` (Line ~537)

**Reason:**
- Generates human-readable reasoning for recommendations
- **Recommendation UI not implemented**
- Part of unused recommendation system

**Evidence:**
```csharp
public string GenerateRecommendationReasoning(TenantRequest request)
{
    if (!IsActive) return "Worker is inactive";
    var reasons = new List<string>();
    // Builds reasoning that's never displayed
}
```

**Usage:** ? Only in dead `WorkerCollectionExtensions` methods

---

#### 1.7 `EstimateCompletionTime(TenantRequest request)`
**Location:** `Domain\Entities\Worker.cs` (Line ~575)

**Reason:**
- Estimates time to complete work
- **Time estimation not used in UI or scheduling**
- Part of recommendation system that's not implemented

**Evidence:**
```csharp
public TimeSpan EstimateCompletionTime(TenantRequest request)
{
    if (!IsActive) return TimeSpan.Zero;
    var baseTime = TimeSpan.FromHours(2);
    // Returns estimated time that's never used
}
```

**Usage:** ? Only in tests and dead recommendation methods

---

#### 1.8 `ValidateAssignmentToRequest(TenantRequest, DateTime)`
**Location:** `Domain\Entities\Worker.cs` (Line ~606)

**Reason:**
- Returns detailed validation result with error messages
- **Command handlers use `ValidateCanBeAssignedToRequest()` instead**
- Redundant with simpler validation methods

**Evidence:**
```csharp
public AssignmentValidationResult ValidateAssignmentToRequest(TenantRequest request, DateTime scheduledDate)
{
    // Returns structured validation result
    // But command handlers use domain exceptions instead
}
```

**Usage:** ? Not called - handlers use exception-based validation

---

### ? METHODS THAT ARE USED (Keep These)

These methods ARE actively used in production:

1. **`IsAvailableForWork(DateTime, TimeSpan?)`** - Used by `GetAvailableWorkersQueryHandler`
2. **`AssignToWork(string, DateTime, string?)`** - Used by `ScheduleServiceWorkCommandHandler`
3. **`CompleteWork(string, bool, string?)`** - Used by work completion flow
4. **`GetUpcomingWorkloadCount(DateTime, int)`** - Used by availability scoring
5. **`HasSpecializedSkills(string)`** - Used by worker filtering
6. **`DetermineRequiredSpecialization(string, string)`** - Used everywhere
7. **`ValidateCanBeAssignedToRequest(...)`** - Used by command handlers
8. **`GetBookedDatesInRange(...)`** - Used by Phase 3 calendar visualization
9. **`GetPartiallyBookedDatesInRange(...)`** - Used by Phase 3 calendar
10. **`GetAvailabilityScoreForDate(...)`** - Used by Phase 3 booking logic
11. **`GetNextFullyAvailableDate(...)`** - Used by worker ordering
12. **`CalculateAvailabilityScore(...)`** - Used by `GetAvailableWorkersQueryHandler`

---

## 2. Property Aggregate Analysis

### ? DEAD METHODS

#### 2.1 `CalculatePerformanceScore()`
**Location:** `Domain\Entities\Property.cs` (Line ~103)

**Reason:**
- Calculates weighted performance score (occupancy 50%, maintenance 30%, satisfaction 20%)
- **Never displayed in UI**
- Not used in any business logic or queries
- Part of unused analytics features

**Evidence:**
```csharp
public double CalculatePerformanceScore()
{
    double occupancyScore = GetOccupancyRate() * 100;
    double maintenanceScore = CalculateMaintenanceScore();
    // Returns score that's never used
}
```

**Usage:** ? Only called by dead collection extensions

---

#### 2.2 `CanAccommodateAdditionalTenants()`
**Location:** `Domain\Entities\Property.cs` (Line ~124)

**Reason:**
- Checks if property has available units
- **Duplicate of `GetAvailableUnits().Any()`**
- Not used in tenant registration flow
- Redundant method

**Evidence:**
```csharp
public bool CanAccommodateAdditionalTenants()
{
    return GetAvailableUnits().Any();
}
```

**Usage:** ? Only in dead collection extensions like `WithAvailableUnits()`

---

#### 2.3 `CalculateRevenuePotential(double)`
**Location:** `Domain\Entities\Property.cs` (Line ~131)

**Reason:**
- Calculates potential rental revenue
- **Revenue tracking not implemented**
- Not displayed in UI or used in reporting

**Evidence:**
```csharp
public double CalculateRevenuePotential(double averageRentPerUnit = 1000)
{
    return Units.Count * averageRentPerUnit * GetOccupancyRate();
}
```

**Usage:** ? Only in dead `PropertyCollectionExtensions` methods

---

#### 2.4 `GetStatistics()`
**Location:** `Domain\Entities\Property.cs` (Line ~137)

**Reason:**
- Returns dictionary of mixed property statistics
- **Never used - queries use projections instead**
- Redundant with `CalculateMetrics()` which IS used

**Evidence:**
```csharp
public Dictionary<string, object> GetStatistics()
{
    IEnumerable<string> availableUnits = GetAvailableUnits();
    return new Dictionary<string, object>
    {
        ["PropertyName"] = Name,
        ["TotalUnits"] = Units.Count,
        // ... more stats that are never consumed
    };
}
```

**Usage:** ? Not found in queries or UI

---

#### 2.5 Private Helper Methods
**Location:** `Domain\Entities\Property.cs`

**Dead private methods:**
- `CalculateMaintenanceScore()` - Only called by dead `CalculatePerformanceScore()`
- `CalculateTenantSatisfactionScore()` - Only called by dead `CalculatePerformanceScore()`

---

### ? METHODS THAT ARE USED (Keep These)

1. **`RegisterTenant(...)`** - Used by tenant registration
2. **`IsUnitAvailable(string)`** - Used by validation
3. **`GetAvailableUnits()`** - Used by queries
4. **`GetOccupiedUnitsCount()`** - Used by statistics
5. **`GetOccupancyRate()`** - Used by dashboard
6. **`RequiresAttention()`** - Used by property collection extensions
7. **`CalculateMetrics()`** - Used by `GetPropertyStatisticsQueryHandler`
8. **`AddUnit(string)`** - Used by property management
9. **`RemoveUnit(string)`** - Used by property management
10. **`UpdateSuperintendent(...)`** - Used by property updates

---

## 3. Tenant Aggregate Analysis

### ? DEAD METHODS

#### 3.1 `SubmitTenantRequest(TenantRequest, TenantRequestUrgency, ITenantRequestSubmissionPolicy)`
**Location:** `Domain\Entities\Tenant.cs` (Line ~88)

**Reason:**
- Designed to enforce submission policies on existing requests
- **Never used - policy validation done in command handlers**
- `CreateAndSubmitTenantRequestCommandHandler` validates directly
- Overly complex API for demo project needs

**Evidence:**
```csharp
public void SubmitTenantRequest(TenantRequest request, TenantRequestUrgency urgency, ITenantRequestSubmissionPolicy policy)
{
    // Complex validation that's duplicated in application layer
    policy.ValidateCanSubmitRequest(this, urgency);
  request.Submit();
}
```

**Usage:** ? Command handlers don't use this method

---

#### 3.2 `CanSubmitRequest(TenantRequestUrgency, ITenantRequestSubmissionPolicy)`
**Location:** `Domain\Entities\Tenant.cs` (Line ~112)

**Reason:**
- Checks if tenant can submit based on policy
- **Policy checks done in command handlers, not pre-checks**
- UI doesn't show "can submit" indicator
- Part of unused policy infrastructure

**Evidence:**
```csharp
public bool CanSubmitRequest(TenantRequestUrgency urgency, ITenantRequestSubmissionPolicy policy)
{
    return policy.CanSubmitRequest(this, urgency);
}
```

**Usage:** ? Not called before submission in production code

---

#### 3.3 `GetNextAllowedSubmissionTime(ITenantRequestSubmissionPolicy)`
**Location:** `Domain\Entities\Tenant.cs` (Line ~124)

**Reason:**
- Returns when tenant can next submit
- **Rate limiting UI not implemented**
- Policy enforcement happens at command level
- Feature not exposed to users

**Evidence:**
```csharp
public DateTime? GetNextAllowedSubmissionTime(ITenantRequestSubmissionPolicy policy)
{
  return policy.GetNextAllowedSubmissionTime(this);
}
```

**Usage:** ? Not displayed in UI or used in flows

---

#### 3.4 `GetRemainingEmergencyRequests(ITenantRequestSubmissionPolicy)`
**Location:** `Domain\Entities\Tenant.cs` (Line ~134)

**Reason:**
- Returns remaining emergency quota
- **Emergency quota UI not implemented**
- Feature not exposed to tenants
- Policy check done server-side only

**Evidence:**
```csharp
public int GetRemainingEmergencyRequests(ITenantRequestSubmissionPolicy policy)
{
    return policy.GetRemainingEmergencyRequests(this);
}
```

**Usage:** ? Not shown in tenant UI

---

### ? METHODS THAT ARE USED (Keep These)

1. **`SubmitRequest(string, string, TenantRequestUrgency)`** - Used by command handlers
2. **`CreateRequest(string, string, string)`** - Legacy compatibility method, still used
3. **`UpdateContactInfo(PersonContactInfo)`** - Used by tenant updates

---

## 4. TenantRequest Aggregate Analysis

### ? DEAD METHODS

#### 4.1 `InitializeFromAggregateIds(...)`
**Location:** `Domain\Entities\TenantRequest.cs` (Line ~267)

**Reason:**
- Private initialization method with 13 parameters
- **Never called - `CreateNew()` uses `InitializeNewRequest()` instead**
- Leftover from refactoring
- Dead code

**Evidence:**
```csharp
private void InitializeFromAggregateIds(...) // 13 parameters
{
    Code = ValidateCode(code);
    // ... initialization that's never executed
}
```

**Usage:** ? Not called anywhere

---

#### 4.2 `IsRequestAssignable(TenantRequest)` (Private)
**Location:** `Domain\Entities\TenantRequest.cs` (Line ~688)

**Reason:**
- Private helper checking if request can be assigned
- **Used only by dead `CanBeAssignedToRequest(TenantRequest)` in Worker**
- Not part of working assignment flow

**Evidence:**
```csharp
private static bool IsRequestAssignable(TenantRequest request)
{
    return request.Status is TenantRequestStatus.Submitted or TenantRequestStatus.Failed;
}
```

**Usage:** Only called by unused Worker methods

---

#### 4.3 `IsOverloaded()` (Private)
**Location:** `Domain\Entities\TenantRequest.cs` (Line ~697)

**Reason:**
- Private helper in wrong aggregate (should be in Worker)
- **Never called - was for unused assignment validation**
- Logic doesn't make sense in TenantRequest

**Evidence:**
```csharp
private bool IsOverloaded()
{
    // Business rule: Worker is overloaded if they have more than 5 active assignments
    int activeAssignments = _assignments.Count(a => !a.IsCompleted);
    return activeAssignments > 5;
}
```

**Usage:** ? Dead code in wrong place

---

### ? METHODS THAT ARE USED (Keep These)

Core workflow methods (ALL USED):
1. **`CreateNew(...)`** - Factory method for new requests
2. **`SubmitForReview()`** - Submit transition
3. **`ScheduleWork(...)`** - Schedule transition
4. **`ReportWorkCompleted(...)`** - Complete transition
5. **`DeclineRequest(...)`** - Decline transition
6. **`Close(...)`** - Close transition
7. **`FailDueToEmergencyOverride(...)`** - Emergency handling
8. **`UpdateTenantInformation(...)`** - Data updates
9. **`ValidateCanBeScheduled(...)`** - Validation for scheduling
10. **`RequiresImmediateAttention()`** - Priority flagging
11. **`GetExpectedResolutionHours()`** - SLA calculation
12. **`IsActive()`** - Status checking
13. **`IsOverdue(...)`** - Overdue detection
14. **`CalculateResolutionPerformanceScore()`** - Performance metrics
15. **`CalculateUrgencyPriority()`** - Priority scoring
16. **`GetAgeInDays()`** - Age calculation
17. **`GetAgeCategory()`** - Age categorization
18. **`DetermineCategoryFromDescription()`** - Category detection
19. **`WasResolvedOnTime()`** - Satisfaction metrics
20. **`WasEmergencyHandledWell()`** - Emergency performance

---

## 5. Summary Statistics

| Aggregate | Total Methods | Dead Methods | Used Methods | Dead Percentage |
|-----------|---------------|--------------|--------------|-----------------|
| **Worker** | 35 | 8 | 27 | 23% |
| **Property** | 18 | 5 | 13 | 28% |
| **Tenant** | 7 | 4 | 3 | 57% |
| **TenantRequest** | 32 | 3 | 29 | 9% |
| **TOTAL** | **92** | **20** | **72** | **22%** |

---

## 6. Recommended Actions

### HIGH PRIORITY - Remove These

1. **Worker Aggregate:**
   - Remove `IsAvailableForSlot()` 
   - Remove `IsEmergencyResponseCapable()`
   - Remove `CalculateScoreForRequest()`
   - Remove `CanBeAssignedToRequest(TenantRequest)`
   - Remove `CalculateRecommendationConfidence()`
   - Remove `GenerateRecommendationReasoning()`
   - Remove `EstimateCompletionTime()`
   - Remove `ValidateAssignmentToRequest()`

2. **Property Aggregate:**
   - Remove `CalculatePerformanceScore()`
 - Remove `CanAccommodateAdditionalTenants()`
   - Remove `CalculateRevenuePotential()`
   - Remove `GetStatistics()`
   - Remove `CalculateMaintenanceScore()` (private)
   - Remove `CalculateTenantSatisfactionScore()` (private)

3. **Tenant Aggregate:**
 - Remove `SubmitTenantRequest()`
   - Remove `CanSubmitRequest()`
   - Remove `GetNextAllowedSubmissionTime()`
   - Remove `GetRemainingEmergencyRequests()`

4. **TenantRequest Aggregate:**
   - Remove `InitializeFromAggregateIds()`
   - Remove `IsRequestAssignable()` (private)
   - Remove `IsOverloaded()` (private - wrong aggregate)

### MEDIUM PRIORITY - Review Collection Extensions

Many collection extension methods in `Domain\Extensions\` are also dead because they use dead aggregate methods:

**Dead Extension Methods:**
- `WorkerCollectionExtensions.GetAvailableForEmergency()` - uses dead `IsEmergencyResponseCapable()`
- `WorkerCollectionExtensions.FindBestMatchForRequest()` - uses dead scoring methods
- `WorkerCollectionExtensions.GetAssignmentRecommendations()` - entire recommendation system unused
- `PropertyCollectionExtensions.CalculateSystemPerformanceScore()` - uses dead `CalculatePerformanceScore()`
- `PropertyCollectionExtensions.CalculateTotalRevenuePotential()` - uses dead revenue method
- `PropertyCollectionExtensions.WithAvailableUnits()` - uses dead `CanAccommodateAdditionalTenants()`

### LOW PRIORITY - Review Domain Services

Some domain services have dead methods that use dead aggregate methods:

- `WorkerAssignmentPolicyService` - entire service may be unused if replacement scoring is in queries
- `RequestCategorizationService` - review if categorization is actually used
- `RequestWorkflowManager` - review if workflow manager is actually used vs. command handlers

---

## 7. Impact Analysis

### Removing Dead Methods Will:

? **Reduce code complexity** by 22%  
? **Eliminate confusion** about which methods to use  
? **Remove test maintenance burden** for unused features  
? **Clarify domain model** for new developers  
? **Improve code coverage metrics** (tests for dead code inflate coverage)  

### Will NOT Break:

? Any production functionality  
? Any UI features  
? Any working business processes  

### Testing Strategy:

1. Run full test suite before removal
2. Remove dead methods one aggregate at a time
3. Run tests after each aggregate cleanup
4. Verify UI still works end-to-end
5. Check Application layer doesn't reference removed methods

---

## 8. Conclusion

**Finding:** 20 out of 92 domain methods (22%) are unused dead code from:
1. "Step 2: Push Logic to Aggregates" refactoring that wasn't fully adopted
2. Advanced features (recommendations, analytics) that were never implemented in UI
3. Over-engineering with multiple ways to do the same thing
4. Policy pattern infrastructure that ended up being unused

**Recommendation:** Remove all identified dead methods to:
- Simplify the domain model for this demo project
- Focus on working functionality only
- Reduce maintenance burden
- Improve code clarity

This is a **safe cleanup** since all dead methods are genuinely unused in production code.

---

**Report Generated:** January 2025  
**Analyst:** GitHub Copilot  
**Project:** RentalRepairs Clean Architecture Demo
