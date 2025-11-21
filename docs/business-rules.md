# Business Rules Documentation

This document outlines the **sophisticated business rules** implemented in the RentalRepairs domain model, demonstrating complex business logic encapsulation and validation.

## Overview

The RentalRepairs system implements **enterprise-grade business rules** across multiple domains:
- **Tenant Request Management** with rate limiting and validation
- **Worker Assignment Logic** with skill matching and availability
- **Property Management** with occupancy and maintenance rules
- **Workflow State Management** with transition validation

## Property Management Rules

### Unit Management
```mermaid
flowchart TD
    A[Add Unit Request] --> B{Valid Unit Number?}
    B -->|No| E1[Throw: Invalid Format]
    B -->|Yes| C{Unit Already Exists?}
    C -->|Yes| E2[Throw: Duplicate Unit]
    C -->|No| D{Property Has Space?}
    D -->|No| E3[Throw: Property Full]
    D -->|Yes| F[Add Unit Successfully]
    
    G[Remove Unit Request] --> H{Unit Exists?}
    H -->|No| E4[Throw: Unit Not Found]
    H -->|Yes| I{Unit Occupied?}
    I -->|Yes| E5[Throw: Unit Occupied]
    I -->|No| J[Remove Unit Successfully]
```

### Business Rules Implementation
```csharp
// Property aggregate encapsulates unit management rules
public void AddUnit(string unitNumber)
{
    if (!IsValidUnitNumber(unitNumber))
        throw new PropertyDomainException($"Unit number '{unitNumber}' has invalid format");
    
    if (Units.Contains(unitNumber))
        throw new PropertyDomainException($"Unit {unitNumber} already exists");
    
    Units.Add(unitNumber);
    AddDomainEvent(new UnitAddedEvent(this, unitNumber));
}

private static bool IsValidUnitNumber(string unitNumber)
{
    return !string.IsNullOrWhiteSpace(unitNumber) && 
           unitNumber.Length <= 10 && 
           Regex.IsMatch(unitNumber, @"^[A-Za-z0-9\-\s]+$");
}
```

## Tenant Request Submission Rules

### Rate Limiting Logic
```mermaid
graph TB
    subgraph "Submission Validation"
        A[Tenant Submits Request] --> B{Check Pending Requests}
        B -->|≥ 5 pending| E1[Reject: Too Many Pending]
        B -->|< 5 pending| C{Check Rate Limit}

        C -->|< 1 hour since last| E4[Reject: Too Soon]
        C -->|≥ 1 hour since last| D{Emergency Request?}

        D -->|Yes| E{Check 30-day Emergency Limit}
        D -->|No| F[Validate Request Content]

        E -->|≥ 3 emergency| E2[Reject: Too Many Emergency]
        E -->|< 3 emergency| F

        F --> G{Similar Active Request?}
        G -->|Duplicate Found| E3[Reject: Duplicate Request]
        G -->|No Duplicates| H[Allow Submission]
    end

    subgraph "Business Rules (Configurable)"
        BC1[Max 5 pending requests]
        BC2[Min 1 hour between submissions]
        BC3[Max 3 emergency per 30 days]
        BC4[No duplicate active requests]
    end
```

### Implementation Details

**Configurable Business Rules via appsettings.json**

The system uses a **configurable policy approach** allowing business rules to be adjusted without code changes:

```csharp
// Configuration model - values loaded from appsettings.json
public class TenantRequestPolicyConfiguration
{
    public int MaxPendingRequests { get; set; } = 5;
    public int MinimumHoursBetweenSubmissions { get; set; } = 1;
    public int MaxEmergencyRequestsPerMonth { get; set; } = 3;
    public int EmergencyRequestLookbackDays { get; set; } = 30;

    public bool IsRateLimitingEnabled => MinimumHoursBetweenSubmissions > 0;
    public bool IsEmergencyLimitingEnabled => MaxEmergencyRequestsPerMonth > 0;
    public TimeSpan RateLimitTimeSpan => TimeSpan.FromHours(MinimumHoursBetweenSubmissions);
    public TimeSpan EmergencyLookbackTimeSpan => TimeSpan.FromDays(EmergencyRequestLookbackDays);
}

// Policy implementation using configuration
public class TenantRequestSubmissionPolicy : ITenantRequestSubmissionPolicy
{
    private readonly TenantRequestPolicyConfiguration _configuration;

    public TenantRequestSubmissionPolicy(TenantRequestPolicyConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void ValidateCanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency)
    {
        // Business Rule 1: Maximum pending requests
        ValidateMaxPendingRequests(tenant);

        // Business Rule 2: Rate limiting between submissions (if enabled)
        if (_configuration.IsRateLimitingEnabled)
        {
            ValidateRateLimit(tenant);
        }

        // Business Rule 3: Emergency request limitations (if enabled)
        if (_configuration.IsEmergencyLimitingEnabled && urgency == TenantRequestUrgency.Emergency)
        {
            ValidateEmergencyRequestLimit(tenant);
        }
    }

    private void ValidateMaxPendingRequests(Tenant tenant)
    {
        int activeRequestsCount = tenant.Requests.Count(r =>
            r.Status is TenantRequestStatus.Submitted or TenantRequestStatus.Scheduled);

        if (activeRequestsCount >= _configuration.MaxPendingRequests)
        {
            throw new MaxPendingRequestsExceededException(
                _configuration.MaxPendingRequests, activeRequestsCount);
        }
    }

    private void ValidateRateLimit(Tenant tenant)
    {
        TenantRequest? lastSubmission = tenant.Requests
            .Where(r => r.Status != TenantRequestStatus.Draft)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (lastSubmission != null)
        {
            TimeSpan timeSinceLastSubmission = DateTime.UtcNow - lastSubmission.CreatedAt;
            TimeSpan minimumWaitTime = _configuration.RateLimitTimeSpan;

            if (timeSinceLastSubmission < minimumWaitTime)
            {
                TimeSpan waitTime = minimumWaitTime - timeSinceLastSubmission;
                throw new SubmissionRateLimitExceededException(waitTime);
            }
        }
    }

    private void ValidateEmergencyRequestLimit(Tenant tenant)
    {
        int emergencyRequestsInPeriod = tenant.Requests
            .Count(r => r.UrgencyLevel == "Emergency" &&
                        r.CreatedAt > DateTime.UtcNow.Subtract(_configuration.EmergencyLookbackTimeSpan));

        if (emergencyRequestsInPeriod >= _configuration.MaxEmergencyRequestsPerMonth)
        {
            throw new EmergencyRequestLimitExceededException(
                _configuration.MaxEmergencyRequestsPerMonth, emergencyRequestsInPeriod);
        }
    }
}
```

**Configuration Example (appsettings.json):**
```json
{
  "TenantRequestSubmission": {
    "MaxPendingRequests": 5,
    "MinimumHoursBetweenSubmissions": 1,
    "MaxEmergencyRequestsPerMonth": 3,
    "EmergencyRequestLookbackDays": 30
  }
}
```

**Benefits of Configurable Approach:**
- ✅ Business rules can be adjusted without code deployment
- ✅ Different rules for different environments (dev, staging, production)
- ✅ Easy A/B testing of business rule variations
- ✅ Quick response to changing business requirements
- ✅ Rules can be disabled by setting values to 0

## Emergency Request Handling

### Priority Classification
```mermaid
graph LR
    subgraph "Urgency Levels"
        L1[Low<br/>168h SLA]
        L2[Normal<br/>72h SLA]  
        L3[High<br/>24h SLA]
        L4[Critical<br/>4h SLA]
        L5[Emergency<br/>2h SLA]
    end
    
    subgraph "Emergency Triggers"
        T1[Gas Leak]
        T2[Electrical Hazard]
        T3[Water Damage]
        T4[Security Breach]
        T5[Heating Failure]
    end
    
    T1 --> L5
    T2 --> L5
    T3 --> L4
    T4 --> L4
    T5 --> L3
    
    subgraph "Response Actions"
        A1[Immediate Assignment]
        A2[Emergency Worker Pool]
        A3[Superintendent Notification]
        A4[Escalation Protocol]
    end
    
    L5 --> A1
    L5 --> A2
    L4 --> A1
    L4 --> A3
    L3 --> A4
```

### Business Logic
```csharp
public bool RequiresImmediateAttention()
{
    // Complex business rule for immediate attention
    return IsEmergency || 
           (Status == TenantRequestStatus.Submitted && 
            CreatedAt <= DateTime.UtcNow.AddDays(-2));
}

public int GetExpectedResolutionHours()
{
    return UrgencyLevel switch
    {
        "Emergency" => 2,    // Critical response time
        "Critical" => 4,     // High priority
        "High" => 24,        // Next business day
        "Normal" => 72,      // Standard SLA
        "Low" => 168,        // Weekly resolution
        _ => 72              // Default fallback
    };
}
```

## Worker Assignment Algorithm

### Skill Matching Logic
```mermaid
flowchart TD
    A[Request Analysis] --> B{Analyze Title & Description}
    
    B --> C1{Plumbing Keywords?}
    B --> C2{Electrical Keywords?}
    B --> C3{HVAC Keywords?}
    B --> C4{Locksmith Keywords?}
    B --> C5{Other Specialized?}
    
    C1 -->|Yes| S1[Plumbing Specialization]
    C2 -->|Yes| S2[Electrical Specialization]
    C3 -->|Yes| S3[HVAC Specialization]
    C4 -->|Yes| S4[Locksmith Specialization]
    C5 -->|Yes| S5[Other Specialization]
    
    C1 -->|No| GM[General Maintenance]
    C2 -->|No| GM
    C3 -->|No| GM
    C4 -->|No| GM
    C5 -->|No| GM
    
    S1 --> F[Find Matching Workers]
    S2 --> F
    S3 --> F
    S4 --> F
    S5 --> F
    GM --> F
    
    F --> G{Workers Available?}
    G -->|Yes| H[Calculate Scores]
    G -->|No| I[Queue Request]
    
    H --> J[Assign Best Match]
```

### Scoring Algorithm
```csharp
public int CalculateScoreForRequest(TenantRequest request)
{
    var score = 0;
    
    // Base score for active workers
    if (!IsActive) return 0;
    score += 100;
    
    // Specialization matching (highest weight)
    var requiredSpec = DetermineRequiredSpecialization(request.Title, request.Description);
    if (HasSpecializedSkills(requiredSpec))
    {
        if (Specialization?.Equals(requiredSpec, StringComparison.OrdinalIgnoreCase) == true)
        {
            score += 200; // Exact match gets highest score
        }
        else
        {
            score += 100; // General maintenance capability
        }
    }
    
    // Availability bonus
    if (IsAvailableForWork(DateTime.Today.AddDays(1)))
    {
        score += 50;
    }
    
    // Workload consideration (lower is better)
    var workload = GetUpcomingWorkloadCount(DateTime.UtcNow);
    score += Math.Max(0, (10 - workload) * 10);
    
    // Emergency handling bonus
    if (request.IsEmergency && IsEmergencyResponseCapable())
    {
        score += 30;
    }
    
    return score;
}
```

## Status Transition Rules

### State Machine Implementation
```mermaid
stateDiagram-v2
    [*] --> Draft : Create Request
    
    Draft --> Submitted : Submit()
    
    Submitted --> Scheduled : AssignWorker()
    Submitted --> Declined : Decline()
    
    Scheduled --> Done : CompleteWork(success=true)
    Scheduled --> Failed : CompleteWork(success=false)
    
    Failed --> Scheduled : Reschedule()
    
    Done --> Closed : Close()
    Declined --> Closed : Close()
    
    Closed --> [*]
    
    note right of Draft
        Business Rules:
        • Can edit all fields
        • Must have title & description
        • Urgency validation required
    end note
    
    note right of Submitted
        Business Rules:
        • Rate limiting applied
        • No duplicate active requests
        • Emergency validation
    end note
    
    note right of Scheduled
        Business Rules:
        • Worker must be available
        • Specialization match required
        • Future date validation
    end note
```

### Validation Logic
```csharp
public void ValidateCanBeScheduled(DateTime scheduledDate, string workerEmail, string workOrderNumber)
{
    // Business rule: Only submitted and failed requests can be scheduled
    if (Status != TenantRequestStatus.Submitted && Status != TenantRequestStatus.Failed)
    {
        throw new TenantRequestDomainException(
            $"Request can only be scheduled from Submitted or Failed status. Current: {Status}");
    }
    
    // Business rule: Future date requirement
    if (scheduledDate <= DateTime.UtcNow)
    {
        throw new TenantRequestDomainException("Scheduled date must be in the future");
    }
    
    // Business rule: Worker assignment validation
    if (string.IsNullOrWhiteSpace(workerEmail))
    {
        throw new TenantRequestDomainException("Worker email is required for scheduling");
    }
}
```


## Data Integrity Rules

### Validation Hierarchy
```mermaid
graph TD
    subgraph "Validation Layers"
        V1[Value Object Validation<br/>• Format validation<br/>• Range checking<br/>• Business constraints]
        V2[Entity Validation<br/>• State consistency<br/>• Business rules<br/>• Relationship integrity]
        V3[Aggregate Validation<br/>• Cross-entity rules<br/>• Transaction boundaries<br/>• Event consistency]
        V4[Domain Service Validation<br/>• Cross-aggregate rules<br/>• Complex algorithms<br/>• Policy enforcement]
    end
    
    V1 --> V2
    V2 --> V3
    V3 --> V4
    
    subgraph "Examples"
        E1[Email Format<br/>PostalCode Pattern]
        E2[Status Transitions<br/>Required Fields]
        E3[Worker Assignment<br/>Tenant Rate Limits]
        E4[Emergency Protocol<br/>Skill Matching]
    end
    
    V1 -.-> E1
    V2 -.-> E2
    V3 -.-> E3
    V4 -.-> E4
```

## Business Rule Benefits

### Encapsulation Advantages
- **Single Source of Truth**: Business rules defined once in domain
- **Consistency**: Same rules applied across all entry points
- **Testability**: Business logic isolated and easily tested
- **Maintainability**: Changes made in one place
- **Documentation**: Code serves as living documentation

### Quality Assurance
- **Domain Expert Validation**: Rules match business requirements
- **Edge Case Handling**: Comprehensive error scenarios covered
- **Performance Optimization**: Efficient rule evaluation
- **Audit Trail**: Business rule execution tracked

---

**This comprehensive business rules implementation demonstrates enterprise-grade domain modeling and validation strategies suitable for complex business applications.**
