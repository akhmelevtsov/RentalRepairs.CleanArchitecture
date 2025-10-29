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
        A[Tenant Submits Request] --> B{Check 24h Rate Limit}
        B -->|> 5 requests| E1[Reject: Too Many Requests]
        B -->|≤ 5 requests| C{Emergency Request?}
        
        C -->|Yes| D{Check 7-day Emergency Limit}
        C -->|No| F[Validate Request Content]
        
        D -->|> 2 emergency| E2[Reject: Too Many Emergency]
        D -->|≤ 2 emergency| F
        
        F --> G{Active Request Exists?}
        G -->|Similar Active| E3[Reject: Duplicate Request]
        G -->|No Duplicates| H[Allow Submission]
    end
    
    subgraph "Business Constants"
        BC1[Max 5 requests per 24h]
        BC2[Max 2 emergency per 7 days]
        BC3[No duplicate active requests]
    end
```

### Implementation Details
```csharp
public class TenantRequestSubmissionPolicy : ITenantRequestSubmissionPolicy
{
    private const int MaxRequestsPer24Hours = 5;
    private const int MaxEmergencyRequestsPer7Days = 2;
    
    public void ValidateCanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency)
    {
        ValidateRateLimit(tenant);
        
        if (urgency.IsEmergency())
        {
            ValidateEmergencyLimit(tenant);
        }
        
        ValidateNoDuplicateActiveRequests(tenant);
    }
    
    private void ValidateRateLimit(Tenant tenant)
    {
        var recentRequests = tenant.Requests.Count(r => 
            r.WasSubmittedWithinHours(24));
            
        if (recentRequests >= MaxRequestsPer24Hours)
        {
            throw new TenantRequestSubmissionPolicyException(
                $"Tenant has submitted {recentRequests} requests in the last 24 hours. Maximum allowed: {MaxRequestsPer24Hours}");
        }
    }
}
```

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

## Performance and Analytics Rules  

### Metrics Calculation
```mermaid
graph TB
    subgraph "Performance Metrics"
        PM1[Resolution Time<br/>Actual vs Expected]
        PM2[First Call Resolution<br/>Success Rate]
        PM3[Worker Efficiency<br/>Completion Rate]
        PM4[Tenant Satisfaction<br/>Based on SLA]
    end
    
    subgraph "Business Rules"
        BR1[On-time = Within SLA]
        BR2[Failed work = Poor performance]
        BR3[Emergency < 2h = Excellent]
        BR4[Overdue = Attention required]
    end
    
    PM1 --> BR1
    PM2 --> BR2
    PM3 --> BR3
    PM4 --> BR4
    
    subgraph "Actions"
        A1[Performance Bonus]
        A2[Worker Training]
        A3[Priority Assignment]
        A4[Escalation Protocol]
    end
    
    BR1 --> A1
    BR2 --> A2
    BR3 --> A3
    BR4 --> A4
```

### Implementation Examples
```csharp
// Performance scoring business logic
public double CalculateResolutionPerformanceScore()
{
    if (!CompletedDate.HasValue) return 0;
    
    var actualHours = (CompletedDate.Value - CreatedAt).TotalHours;
    var expectedHours = GetExpectedResolutionHours();
    
    if (actualHours <= expectedHours)
        return 100; // On time = perfect score
    
    // Calculate penalty for being late
    var latePenalty = Math.Min(50, (actualHours - expectedHours) / expectedHours * 50);
    return Math.Max(0, 100 - latePenalty);
}

// Property attention requirement
public bool RequiresAttention()
{
    const double AttentionThreshold = 0.8; // Business rule constant
    return GetOccupancyRate() < AttentionThreshold;
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