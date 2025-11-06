# Domain Services Quick Reference
**Last Updated:** January 2025  
**Status After Cleanup:** 8 Active Services

---

## ?? Active Domain Services

### ? **Fully Used Services** (Keep as-is)

| Service | Purpose | Usage | Tests |
|---------|---------|-------|-------|
| **UnitSchedulingService** | Worker scheduling validation & business rules | ? Used by ScheduleServiceWorkCommandHandler | ? Yes |
| **AuthorizationDomainService** | Pure domain authorization logic | ? Used by Infrastructure AuthorizationService | ? No |
| **RequestWorkflowManager** | Workflow state transitions & orchestration | ? Used by TenantRequestPolicyService | ? No |

### ?? **Partially Used Services** (Many methods unused)

| Service | Active Methods | Unused Methods | Status |
|---------|---------------|----------------|--------|
| **TenantRequestPolicyService** | 5 | 1 | ?? Monitor |
| **TenantRequestStatusPolicy** | 5 | 15 | ?? Keep for UI |
| **TenantRequestUrgencyPolicy** | 3 | 8 | ?? Future: Tenant Portal |
| **RequestAuthorizationPolicy** | 4 | 3 | ? Used internally |
| **UserRoleDomainService** | 4 | 4 | ?? Future: Escalation |
| **PropertyPolicyService** | 0 | 4 | ?? **Monitor closely** |
| **TenantRequestSubmissionPolicy** | 2 | 2 | ?? Rate limiting |

---

## ? Recently Removed Services

| Service | Reason | Date Removed |
|---------|--------|--------------|
| **RequestCategorizationService** | DDD Violation (had repository dependencies) | Jan 2025 |
| **RequestTitleGenerator** | Never integrated, over-engineered | Jan 2025 |

---

## ?? Service Usage Examples

### UnitSchedulingService ?
```csharp
// In ScheduleServiceWorkCommandHandler
var validationResult = _unitSchedulingService.ValidateWorkerAssignment(
    requestId, propertyCode, unitNumber, scheduledDate, 
  workerEmail, workerSpecialization, requiredSpecialization,
    isEmergency, existingAssignments);

if (!validationResult.IsValid)
{
    return Result.Failure(validationResult.ErrorMessage);
}
```

### TenantRequestPolicyService ?
```csharp
// In TenantRequestService (Application layer)
var result = _policyService.ValidateWorkflowTransition(
 request, toStatusEnum, "SystemUser");

if (!result.IsSuccess)
{
    return false;
}
```

### TenantRequestStatusPolicy ?
```csharp
// In UI Pages for status display
var cssClass = _statusPolicy.GetStatusCssClass(request.Status);
var displayName = _statusPolicy.GetStatusDisplayName(request.Status);
```

---

## ?? Anti-Patterns to Avoid

### ? DON'T: Repository Dependencies in Domain

```csharp
// ? WRONG: Domain service with repository dependency
public class SomeDomainService
{
    private readonly ISomeRepository _repository; // ? INFRASTRUCTURE CONCERN!
    
    public async Task<List<Entity>> GetFilteredEntitiesAsync()
    {
   return await _repository.GetBySpecificationAsync(...); // ? DATA ACCESS!
    }
}
```

### ? DO: Pure Business Logic Only

```csharp
// ? CORRECT: Domain service with pure business logic
public class SomeDomainService
{
    public ValidationResult ValidateBusinessRule(Entity entity, BusinessContext context)
    {
        // Pure business logic - no data access
      if (!entity.MeetsRequirement(context))
     {
            return ValidationResult.Failure("Business rule violated");
        }
    return ValidationResult.Success();
    }
}
```

---

## ?? Architecture Guidelines

### Domain Services Should:
- ? Contain pure business logic
- ? Coordinate multiple entities
- ? Implement complex business rules
- ? Be stateless (no mutable state)
- ? Have no infrastructure dependencies

### Domain Services Should NOT:
- ? Access databases (use repositories)
- ? Call external APIs
- ? Have UI concerns
- ? Handle infrastructure (logging, caching, etc.)
- ? Be created for simple entity methods

---

## ?? When to Create a Domain Service

### ? Create When:
1. Business logic spans multiple aggregates
2. Complex rules require coordination
3. Workflow/state machine management needed
4. Business rule validation across entities

### ? Don't Create When:
1. Logic belongs to single entity
2. It's just data access (use repository)
3. It's speculative/"future use"
4. Simple validation (use entity methods)

---

## ?? Service Health Checklist

Use this checklist during quarterly reviews:

- [ ] Service has clear, single responsibility
- [ ] Service is actively used (not just tested)
- [ ] Service has no infrastructure dependencies
- [ ] Service follows DDD principles
- [ ] Service methods are not duplicated elsewhere
- [ ] Service has documentation
- [ ] "Future use" methods have roadmap dates

---

## ?? Quarterly Review Process

1. **Identify Unused Services:**
   ```bash
   # Search for service usage
   grep -r "ServiceName" src/Application/
   grep -r "ServiceName" src/WebUI/
   ```

2. **Check DDD Compliance:**
   - Look for repository dependencies
   - Look for infrastructure concerns
   - Validate pure business logic only

3. **Remove or Document:**
   - Remove services unused for 3+ months
   - Document "future use" services with dates
   - Update this reference guide

---

## ?? Service Ownership

| Service | Owner/Team | Purpose |
|---------|-----------|---------|
| UnitSchedulingService | Scheduling Team | Worker assignment validation |
| TenantRequestPolicyService | Requests Team | Business policy orchestration |
| RequestWorkflowManager | Workflow Team | State transition management |
| AuthorizationDomainService | Security Team | Authorization business rules |
| Others | Core Domain Team | General domain logic |

---

## ?? Learning Resources

### DDD Domain Services:
- **Book:** "Domain-Driven Design" by Eric Evans (Chapter on Domain Services)
- **Guideline:** Use when logic doesn't naturally fit in an entity
- **Rule of Thumb:** If you're hesitating, it probably belongs in the entity

### Clean Architecture:
- **Principle:** Domain layer has NO dependencies on outer layers
- **Rule:** No repositories, no infrastructure, no UI in Domain
- **Check:** Can you test the Domain layer without any infrastructure?

---

## ?? Quick Decision Tree

```
Need to implement business logic?
?
?? Does it naturally belong to a single entity?
?  ?? YES ? Add method to entity ?
?  ?? NO  ? Continue...
?
?? Does it require data access?
?  ?? YES ? Create Application Service (not Domain Service) ?
?  ?? NO  ? Continue...
?
?? Does it coordinate multiple entities/aggregates?
?  ?? YES ? Create Domain Service ?
?  ?? NO  ? Reconsider - might be entity method
?
?? Is it for a "future" feature?
   ?? YES ? DON'T CREATE IT ? (wait until actually needed)
   ?? NO  ? Create Domain Service ?
```

---

## ?? Related Documentation

- `DEAD_METHODS_DOMAIN_SERVICES_REPORT.md` - Full analysis report
- `DOMAIN_SERVICES_CLEANUP_COMPLETE.md` - Cleanup summary
- `DOMAIN_SERVICES_CLEANUP_FINAL_REPORT.md` - Comprehensive final report
- `Domain\DependencyInjection.cs` - Service registration

---

**Maintained By:** Development Team  
**Review Frequency:** Quarterly  
**Last Cleanup:** January 2025  
**Next Review:** April 2025
