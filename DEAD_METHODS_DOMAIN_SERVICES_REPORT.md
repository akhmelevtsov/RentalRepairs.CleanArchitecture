# Dead Methods Analysis Report: Domain Services
**Analysis Date:** January 2025  
**Scope:** Domain\Services layer  
**Analysis Type:** Production code usage verification

---

## Executive Summary

This report identifies domain service methods that exist but are not actively used in production code, tests, or by other services. All domain services were analyzed for actual usage patterns.

**Key Findings:**
- ? **8 Domain Services** are properly registered and actively used
- ?? **Multiple unused public methods** found across services
- ?? **Usage Pattern**: Most services have comprehensive method sets, but many methods are not called

---

## Services Analysis

### 1. ? **TenantRequestStatusPolicy** - ACTIVELY USED
**Location:** `Domain\Services\TenantRequestStatusPolicy.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used by multiple command handlers and application services

#### Used Methods:
- `GetStatusCssClass()` - Used in UI pages
- `IsValidStatusTransition()` - Used by TenantRequest entity and workflow managers
- `GetAllowedNextStatuses()` - Used in validation logic
- `IsCompletedStatus()` - Used in TenantRequest entity
- `GetStatusDisplayName()` - Used in UI pages

#### ?? Potentially Dead Methods:
```csharp
// These comprehensive methods exist but evidence of usage not found:
public bool CanEditInStatus(TenantRequestStatus status)
public bool CanCancelInStatus(TenantRequestStatus status)
public bool CanAssignWorkerInStatus(TenantRequestStatus status)
public bool CanScheduleWorkInStatus(TenantRequestStatus status)
public bool CanCompleteWorkInStatus(TenantRequestStatus status)
public bool CanDeclineInStatus(TenantRequestStatus status)
public bool CanCloseInStatus(TenantRequestStatus status)
public int GetStatusPriority(TenantRequestStatus status)
public bool IsActiveStatus(TenantRequestStatus status)
public bool IsFinalStatus(TenantRequestStatus status)
public TenantRequestStatus ParseStatus(string statusString, TenantRequestStatus defaultStatus = TenantRequestStatus.Draft)
public bool TryParseStatus(string statusString, out TenantRequestStatus status)
public bool RequiresAttention(TenantRequestStatus status)
public StatusCategory GetStatusCategory(TenantRequestStatus status)
public StatusTransitionValidationResult ValidateStatusTransition(TenantRequestStatus fromStatus, TenantRequestStatus toStatus)
```

**Reason:** The entity methods like `ValidateCanBeScheduled()` are used directly instead of these policy methods.

---

### 2. ?? **RequestAuthorizationPolicy** - PARTIALLY USED
**Location:** `Domain\Services\RequestAuthorizationPolicy.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used by TenantRequestPolicyService

#### Used Methods:
- `GetAvailableActionsForRole()` - Used by TenantRequestPolicyService
- `CanRolePerformAction()` - Used by TenantRequestPolicyService
- `CanRoleEditRequestInStatus()` - Used by TenantRequestPolicyService
- `CanRoleCancelRequestInStatus()` - Used by TenantRequestPolicyService

#### ?? Potentially Dead Methods:
```csharp
// Comprehensive authorization methods that may not be actively used:
public bool CanRoleTransitionStatus(string userRole, TenantRequestStatus fromStatus, TenantRequestStatus toStatus)
public int GetRolePriority(string userRole)
public bool HasHigherPrivileges(string userRole, string comparedToRole)
```

**Analysis:** These are used internally by `RequestWorkflowManager`, but their external usage is limited.

---

### 3. ?? **RequestWorkflowManager** - USED BY POLICY SERVICE
**Location:** `Domain\Services\RequestWorkflowManager.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used exclusively by TenantRequestPolicyService

#### Used Methods:
- `ExecuteTransition()` - Used by TenantRequestPolicyService
- `GetRecommendedNextActions()` - Used by TenantRequestPolicyService
- `ValidateWorkflowIntegrity()` - Used by TenantRequestPolicyService
- `EvaluateEscalationNeed()` - Used by TenantRequestPolicyService
- `CalculateWorkflowMetrics()` - Used by TenantRequestPolicyService

#### ? Status: **All public methods are used** by TenantRequestPolicyService

**Note:** This is a good example of proper service composition - all methods have clear consumers.

---

### 4. ?? **TenantRequestPolicyService** - USED BUT SOME METHODS UNUSED
**Location:** `Domain\Services\TenantRequestPolicyService.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used by TenantRequestService (Application layer)

#### Used Methods:
- `ValidateWorkflowTransition()` - Used by TenantRequestService
- `ValidateUserAuthorization()` - Used by TenantRequestService
- `GenerateBusinessContext()` - Used by TenantRequestService
- `ValidateRequestSubmission()` - Used by TenantRequestService
- `GenerateFilteringStrategy()` - Used by TenantRequestService

#### ?? Potentially Dead Methods:
```csharp
// Analytical method that may not be actively used:
public RequestPerformanceAnalysis AnalyzeRequestPerformance(TenantRequest request)
```

**Analysis:** This comprehensive analytics method exists but no evidence of active usage found. Likely built for future reporting features.

---

### 5. ? **RequestCategorizationService** - POTENTIALLY DEAD SERVICE
**Location:** `Domain\Services\RequestCategorizationService.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** **NO EVIDENCE OF USAGE FOUND**

#### All Public Methods Potentially Unused:
```csharp
public async Task<List<TenantRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
public async Task<List<TenantRequest>> GetEmergencyRequestsAsync(CancellationToken cancellationToken = default)
public async Task<List<TenantRequest>> GetOverdueRequestsAsync(CancellationToken cancellationToken = default)
public async Task<List<TenantRequest>> GetRequestsRequiringAttentionAsync(CancellationToken cancellationToken = default)
public async Task<Dictionary<string, List<TenantRequest>>> GetRequestsByUrgencyAsync(CancellationToken cancellationToken = default)
public async Task<Dictionary<StatusCategory, List<TenantRequest>>> GetRequestsByStatusCategoryAsync(CancellationToken cancellationToken = default)
public async Task<List<TenantRequest>> GetAllActiveRequestsAsync(CancellationToken cancellationToken = default)
public List<TenantRequest> ApplyBusinessFilters(List<TenantRequest> requests, RequestFilterCriteria? criteria = null)
```

**?? CRITICAL FINDING:** This entire service has repository dependencies but no evidence of usage. This violates DDD principles:
- Has `ITenantRequestRepository` dependency (infrastructure concern in domain)
- All methods are async repository queries
- **Recommendation:** Move to Application layer or remove if truly unused

---

### 6. ?? **TenantRequestUrgencyPolicy** - PARTIALLY USED
**Location:** `Domain\Services\TenantRequestUrgencyPolicy.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used by RequestWorkflowManager and TenantRequest entity

#### Used Methods:
- `GetExpectedResolutionHours()` - Used by TenantRequest entity and RequestWorkflowManager
- `IsValidUrgencyLevel()` - Used in validation scenarios
- `IsEmergencyLevel()` - Used in business logic

#### ?? Potentially Dead Methods:
```csharp
// Comprehensive urgency management methods with no evidence of usage:
public string DetermineDefaultUrgencyLevel(Tenant tenant)
public List<UrgencyLevelOption> GetAvailableUrgencyLevels(Tenant tenant)
public bool IsUrgencyLevelAllowed(Tenant tenant, string urgencyLevel)
public int GetUrgencyPriority(string urgencyLevel)
public string GetUrgencyDisplayName(string urgencyLevel)
public string GetUrgencyDescription(string urgencyLevel)
public UrgencyUsageAnalysis AnalyzeUrgencyUsage(Tenant tenant, TimeSpan? period = null)
public List<string> GetAllValidUrgencyLevels()
```

**Analysis:** These appear to be designed for a tenant portal or urgency level selection UI, but are not currently integrated.

---

### 7. ? **RequestTitleGenerator** - POTENTIALLY DEAD SERVICE
**Location:** `Domain\Services\RequestTitleGenerator.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** **NO EVIDENCE OF USAGE FOUND**

#### All Public Methods Potentially Unused:
```csharp
public string GenerateTitle(string problemDescription, string? existingTitle = null)
public string GenerateTitleWithUrgency(string problemDescription, string urgencyLevel, string? existingTitle = null)
public TitleValidationResult ValidateTitle(string title)
public List<string> SuggestTitleImprovements(string currentTitle, string description)
```

**Analysis:** This is a sophisticated title generation service with pattern matching and AI-like logic, but no evidence of integration. Likely built for auto-title generation feature that was never completed.

---

### 8. ? **UnitSchedulingService** - ACTIVELY USED
**Location:** `Domain\Services\UnitSchedulingService.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used by ScheduleServiceWorkCommandHandler

#### Used Methods:
- `ValidateWorkerAssignment()` - Core scheduling validation logic
- `ProcessEmergencyOverride()` - Emergency scheduling logic

#### ? Status: **All public methods are actively used**

**Test Coverage:** ? Comprehensive tests exist in `UnitSchedulingServiceTests.cs`

---

### 9. ?? **UserRoleDomainService** - PARTIALLY USED
**Location:** `Domain\Services\UserRoleDomainService.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used by UserRoleService (Application layer)

#### Used Methods:
- `GetPermissionsForRole()` - Used by UserRoleService
- `IsValidRole()` - Used by UserRoleService
- `CanRoleDeclineRequests()` - Used by UserRoleService
- `RoleHasPermission()` - Used by UserRoleService

#### ?? Potentially Dead Methods:
```csharp
// Role hierarchy and escalation methods with no evidence of usage:
public bool CanRoleAssignWork(string assignerRole, string assigneeRole)
public int GetRolePriority(string role)
public bool CanEscalateToRole(string fromRole, string toRole)
public List<string> GetAllValidRoles()
```

**Analysis:** These are well-designed business rules for role-based operations, but may not be actively used in current workflows.

---

### 10. ?? **PropertyPolicyService** - MINIMAL USAGE
**Location:** `Domain\Services\PropertyPolicyService.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** **LIMITED EVIDENCE OF USAGE**

#### ?? All Methods Potentially Unused:
```csharp
public void ValidatePropertyCreation(...)
public void ValidateTenantRegistration(...)
public UnitAssignmentStrategy DetermineOptimalUnitAssignment(...)
public PropertyPerformanceMetrics CalculatePropertyPerformance(...)
```

**Analysis:** This service has comprehensive tests (`PropertyPolicyServiceTests.cs` and `PropertyDomainServiceTests.cs`) but minimal evidence of production usage. Property operations may be handled directly by entities instead.

---

### 11. ?? **AuthorizationDomainService** - USED BY INFRASTRUCTURE
**Location:** `Domain\Services\AuthorizationDomainService.cs`  
**Registration:** ? Registered in DI  
**Primary Usage:** Used by Infrastructure.Authentication.AuthorizationService

#### Used Methods:
- `CanUserAccessProperty()` - Used by AuthorizationService
- `CanUserManageWorkers()` - Used by AuthorizationService

#### ? Status: **All public methods are actively used**

**Note:** Proper separation - pure domain logic used by infrastructure orchestration.

---

### 12. ?? **TenantRequestSubmissionPolicy** - INTERFACE NOT WIDELY USED
**Location:** `Domain\Services\TenantRequestSubmissionPolicy.cs`  
**Registration:** ? Registered as ITenantRequestSubmissionPolicy  
**Primary Usage:** **LIMITED EVIDENCE OF USAGE**

#### Public Methods:
```csharp
void ValidateCanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency)
bool CanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency)
DateTime? GetNextAllowedSubmissionTime(Tenant tenant)
int GetRemainingEmergencyRequests(Tenant tenant)
```

**Analysis:** Interface exists with configuration support, but limited evidence of integration into submission workflows.

---

## Architectural Issues Identified

### ?? **Critical: Domain Services with Repository Dependencies**

**Problem:** `RequestCategorizationService` has repository dependencies, violating DDD principles.

```csharp
// ? ANTI-PATTERN: Repository in Domain Service
public class RequestCategorizationService
{
    private readonly ITenantRequestRepository _tenantRequestRepository;
    
    public async Task<List<TenantRequest>> GetPendingRequestsAsync(...)
    {
        var spec = new TenantRequestByMultipleStatusSpecification(...);
        return (await _tenantRequestRepository.GetBySpecificationAsync(spec, ...)).ToList();
    }
}
```

**Recommendation:**
1. Move to Application layer as an Application Service
2. OR refactor to pure domain logic that operates on collections passed in
3. OR remove if genuinely unused

---

## Summary Statistics

| Service | Status | Active Methods | Unused Methods | Tests Exist |
|---------|--------|----------------|----------------|-------------|
| TenantRequestStatusPolicy | ?? Partial | 5 | 15 | ? No |
| RequestAuthorizationPolicy | ?? Partial | 4 | 3 | ? No |
| RequestWorkflowManager | ? Active | 5 | 0 | ? No |
| TenantRequestPolicyService | ?? Partial | 5 | 1 | ? No |
| RequestCategorizationService | ? Dead | 0 | 8 | ? No |
| TenantRequestUrgencyPolicy | ?? Partial | 3 | 8 | ? No |
| RequestTitleGenerator | ? Dead | 0 | 4 | ? No |
| UnitSchedulingService | ? Active | 2 | 0 | ? Yes |
| UserRoleDomainService | ?? Partial | 4 | 4 | ? No |
| PropertyPolicyService | ?? Minimal | 0 | 4 | ? Yes |
| AuthorizationDomainService | ? Active | 2 | 0 | ? No |
| TenantRequestSubmissionPolicy | ?? Limited | 2 | 2 | ? No |

---

## Recommendations

### Immediate Actions:

1. **? Remove Dead Services:**
   - `RequestCategorizationService` - Move to Application layer or delete
   - `RequestTitleGenerator` - Delete if not planned for near-term use

2. **?? Document Unused But Valuable Methods:**
   - `TenantRequestStatusPolicy` - Many methods for future authorization features
   - `TenantRequestUrgencyPolicy` - Tenant portal methods not yet integrated
   - `UserRoleDomainService` - Role hierarchy methods for future escalation features

3. **? Services to Keep As-Is:**
   - `UnitSchedulingService` - Fully used with excellent tests
   - `AuthorizationDomainService` - Core authorization logic
   - `RequestWorkflowManager` - Fully integrated workflow orchestration

### Long-Term Strategy:

1. **Audit Unused Methods:**
   - Review each ?? service's unused methods
   - Delete methods with no near-term roadmap usage
   - Document methods being kept for future features

2. **Add Tests for Active Services:**
   - `TenantRequestStatusPolicy` needs tests
   - `RequestAuthorizationPolicy` needs tests
   - `TenantRequestPolicyService` needs tests

3. **Fix Architectural Violations:**
 - Remove repository dependencies from `RequestCategorizationService`
   - Ensure all Domain Services contain only pure business logic

---

## Conclusion

The Domain Services layer has a mix of:
- ? **Well-designed, actively used services** (UnitSchedulingService, AuthorizationDomainService)
- ?? **Over-engineered services** with many unused methods (TenantRequestStatusPolicy, TenantRequestUrgencyPolicy)
- ? **Unused or architecturally problematic services** (RequestCategorizationService, RequestTitleGenerator)

**Primary Issue:** Many comprehensive methods were built anticipating future features (tenant portals, advanced reporting) that haven't been implemented yet. These should either be removed or clearly documented as "future use" methods.

**Best Practice Violation:** `RequestCategorizationService` violates DDD by having repository dependencies in the Domain layer.

---

**Report Generated:** 2025-01-15  
**Analysis Tool:** Manual code review with search verification  
**Confidence Level:** High (based on codebase search and usage patterns)
