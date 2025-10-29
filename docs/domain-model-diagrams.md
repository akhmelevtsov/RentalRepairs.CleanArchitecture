# Domain Model Diagrams

This document contains comprehensive Mermaid diagrams for the Rental Repairs domain model, organized by different architectural views.

## Documentation Navigation

- **[Main README](../README.md)** - Project overview and getting started
- **[Architecture Highlights](architecture-highlights.md)** - Portfolio-focused architectural overview  
- **[Business Rules](business-rules.md)** - Detailed business logic documentation
- **[Getting Started Guide](GETTING_STARTED.md)** - Development setup
- **[Development Setup](DEVELOPMENT_SETUP.md)** - Local configuration

---

## Diagram Overview

This comprehensive technical reference provides **10 detailed views** of the domain architecture:

1. **Core Domain Entities** - Entity relationships and data model
2. **Value Objects** - Self-validating business concepts  
3. **Domain Events** - Event-driven architecture flows
4. **Domain Services** - Cross-aggregate business logic
5. **Status Workflows** - State machine implementations
6. **Worker Assignment** - Complex business algorithms
7. **Aggregate Boundaries** - DDD boundary definitions
8. **Clean Architecture** - Layer separation and dependencies
9. **Business Rules** - Validation and constraint flows
10. **Integration Patterns** - Event publishing and handling

---

## 1. Core Domain Entities and Relationships

```mermaid
erDiagram
    Property {
        Guid Id PK
        string Name
        string Code
        PropertyAddress Address
        string PhoneNumber
        PersonContactInfo Superintendent
        List-string Units
        string NoReplyEmailAddress
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    Tenant {
        Guid Id PK
        Guid PropertyId FK
        PersonContactInfo ContactInfo
        string PropertyCode
        string UnitNumber
        int RequestsCount
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    TenantRequest {
        Guid Id PK
        string Code
        string Title
        string Description
        TenantRequestStatus Status
        string UrgencyLevel
        bool IsEmergency
        Guid TenantId FK
        Guid PropertyId FK
        string TenantFullName
        string TenantEmail
        string TenantUnit
        string PropertyName
        string PropertyPhone
        string SuperintendentFullName
        string SuperintendentEmail
        DateTime ScheduledDate
        string AssignedWorkerEmail
        string AssignedWorkerName
        string WorkOrderNumber
        DateTime CompletedDate
        string CompletionNotes
        string ClosureNotes
        bool WorkCompletedSuccessfully
        string PreferredContactTime
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    Worker {
        Guid Id PK
        PersonContactInfo ContactInfo
        bool IsActive
        string Specialization
        string Notes
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    WorkAssignment {
        Guid Id PK
        Guid WorkerId FK
        string WorkOrderNumber
        DateTime ScheduledDate
        bool IsCompleted
        bool CompletedSuccessfully
        string CompletionNotes
        string Notes
    }

    TenantRequestChange {
        Guid Id PK
        Guid TenantRequestId FK
        string ChangeDescription
        DateTime ChangeDate
        string ChangedBy
    }

    Property ||--o{ Tenant : "registers"
    Property ||--o{ TenantRequest : "contains"
    Tenant ||--o{ TenantRequest : "creates"
    Worker ||--o{ WorkAssignment : "has"
    TenantRequest ||--o{ TenantRequestChange : "tracks"
```

## 2. Value Objects and Their Relationships

```mermaid
classDiagram
    class PropertyAddress {
        -string StreetNumber
        -string StreetName
        -string City
        -string PostalCode
        +string FullAddress
        +GetMailingAddress() string
        +IsWithinServiceArea(cities) bool
        +WithStreetAddress(streetNumber, streetName) PropertyAddress
        +WithCity(city) PropertyAddress
        +WithPostalCode(postalCode) PropertyAddress
    }

    class PersonContactInfo {
        -string FirstName
        -string LastName
        -string EmailAddress
        -string PhoneNumber
        +GetFullName() string
        +GetDisplayName() string
        +WithEmail(email) PersonContactInfo
        +WithPhone(phone) PersonContactInfo
        +WithName(firstName, lastName) PersonContactInfo
    }

    class SchedulingSlot {
        -DateTime Date
        -TimeSpan StartTime
        -TimeSpan EndTime
        -SlotType Type
        +GetScheduledDateTime() DateTime
        +OverlapsWith(other) bool
        +IsSuitableForEmergency() bool
        +CreateStandardSlots(date) List~SchedulingSlot~
        +FromTenantPreference(date, preference) SchedulingSlot
    }

    class WorkAssignment {
        -string WorkOrderNumber
        -DateTime ScheduledDate
        -bool IsCompleted
        -bool CompletedSuccessfully
        -string CompletionNotes
        -string Notes
        +Complete(successful, notes) WorkAssignment
        +IsOverdue() bool
    }

    class NotificationData {
        -string RecipientEmail
        -string Subject
        -string Body
        -NotificationType Type
        -Dictionary~string, object~ TemplateData
        +IsValid() bool
        +GetFormattedSubject() string
        +GetFormattedBody() string
    }

    class ServiceWorkScheduleInfo {
        -DateTime ScheduledDate
        -string WorkerEmail
        -string WorkOrderNumber
        -int ServiceWorkOrderCount
        +IsValidSchedule() bool
        +GetScheduleDescription() string
    }

    Property *-- PropertyAddress : contains
    Property *-- PersonContactInfo : superintendent
    Tenant *-- PersonContactInfo : contact info
    Worker *-- PersonContactInfo : contact info
    TenantRequest *-- SchedulingSlot : preferred slot
    Worker *-- WorkAssignment : assignments
    TenantRequest *-- ServiceWorkScheduleInfo : schedule info
    TenantRequest *-- NotificationData : notifications
```

## 3. Domain Events and Event Flow

```mermaid
graph TB
    subgraph "Property Events"
        PE1[PropertyRegisteredEvent]
        PE2[TenantRegisteredEvent]
        PE3[SuperintendentChangedEvent]
        PE4[UnitAddedEvent]
        PE5[UnitRemovedEvent]
        PE6[TenantContactInfoChangedEvent]
    end

    subgraph "Tenant Request Events"
        TRE1[TenantRequestCreatedEvent]
        TRE2[TenantRequestSubmittedEvent]
        TRE3[TenantRequestScheduledEvent]
        TRE4[TenantRequestCompletedEvent]
        TRE5[TenantRequestDeclinedEvent]
        TRE6[TenantRequestClosedEvent]
        TRE7[TenantRequestTenantInfoUpdatedEvent]
        TRE8[TenantRequestPropertyInfoUpdatedEvent]
    end

    subgraph "Worker Events"
        WE1[WorkerRegisteredEvent]
        WE2[WorkerAssignedEvent]
        WE3[WorkCompletedEvent]
        WE4[WorkerSpecializationChangedEvent]
        WE5[WorkerContactInfoChangedEvent]
        WE6[WorkerActivatedEvent]
        WE7[WorkerDeactivatedEvent]
    end

    subgraph "Event Handlers"
        EH1[NotificationHandler]
        EH2[AuditHandler]
        EH3[MetricsHandler]
        EH4[IntegrationHandler]
    end

    PE1 --> EH1
    PE2 --> EH1
    PE2 --> EH2
    
    TRE1 --> EH1
    TRE1 --> EH2
    TRE2 --> EH1
    TRE2 --> EH3
    TRE3 --> EH1
    TRE3 --> EH3
    TRE4 --> EH1
    TRE4 --> EH3
    TRE5 --> EH1
    TRE6 --> EH1
    TRE6 --> EH3

    WE1 --> EH2
    WE2 --> EH1
    WE2 --> EH3
    WE3 --> EH1
    WE3 --> EH3
```

## 4. Domain Services Architecture

```mermaid
classDiagram
    direction TB
    
    class TenantRequestBusinessService {
        +ValidateRequestSubmission(tenant, urgency) bool
        +CalculateUrgentRequests(requests) List~TenantRequest~
        +GetOverdueRequests(requests) List~TenantRequest~
        +CalculatePerformanceMetrics(requests) Metrics
    }

    class WorkerAssignmentPolicyService {
        +FindBestWorkerForRequest(request, workers) Worker
        +ValidateWorkerAssignment(worker, request) ValidationResult
        +CalculateWorkerScores(workers, request) List~WorkerScore~
        +GetRecommendedWorkers(request, workers) List~WorkerRecommendation~
    }

    class RequestTitleGenerator {
        +GenerateTitleWithUrgency(description, urgency, customTitle) string
        +ValidateTitle(title) ValidationResult
        +SuggestTitleImprovements(title, description) List~string~
    }

    class TenantRequestUrgencyPolicy {
        +IsValidUrgencyLevel(urgency) bool
        +GetAllValidUrgencyLevels() List~string~
        +GetExpectedResolutionHours(urgency) int
        +DetermineUrgencyFromDescription(description) string
    }

    class TenantRequestStatusPolicy {
        +IsValidStatusTransition(from, to) bool
        +ValidateStatusTransition(from, to) ValidationResult
        +CanEditInStatus(status) bool
        +CanScheduleWorkInStatus(status) bool
        +GetStatusCategory(status) StatusCategory
        +RequiresAttention(status) bool
    }

    class PropertyDomainService {
        +ValidatePropertyCreation(property) ValidationResult
        +CalculateOccupancyMetrics(property) PropertyMetrics
        +FindAvailableUnits(property) List~string~
        +ValidateUnitRegistration(property, unit) ValidationResult
    }

    class RequestWorkflowManager {
        +GetNextValidStatuses(currentStatus) List~TenantRequestStatus~
        +CanTransitionTo(from, to) bool
        +GetWorkflowHistory(request) List~WorkflowStep~
        +ValidateWorkflowTransition(request, newStatus) ValidationResult
    }

    class UnitSchedulingService {
        +GetAvailableSlots(date, property) List~SchedulingSlot~
        +FindOptimalSlot(request, preferences) SchedulingSlot
        +ValidateSlotAvailability(slot, worker) bool
        +CreateScheduleRecommendation(request, worker) ScheduleRecommendation
    }

    TenantRequestBusinessService ..> TenantRequestUrgencyPolicy : uses
    TenantRequestBusinessService ..> TenantRequestStatusPolicy : uses
    WorkerAssignmentPolicyService ..> UnitSchedulingService : uses
    TenantRequest ..> RequestTitleGenerator : uses
    TenantRequest ..> TenantRequestStatusPolicy : uses
    Property ..> PropertyDomainService : uses
    Worker ..> WorkerAssignmentPolicyService : uses
```

## 5. Enum Types and Status Flow

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> Submitted : Submit()
    
    Submitted --> Scheduled : Schedule()
    Submitted --> Declined : Decline()
    
    Scheduled --> Done : ReportWorkCompleted(success=true)
    Scheduled --> Failed : ReportWorkCompleted(success=false)
    
    Failed --> Scheduled : Reschedule()
    
    Done --> Closed : Close()
    Declined --> Closed : Close()
    
    Closed --> [*]
    
    note right of Draft
        Initial status when request is created
        Can edit all fields
    end note
    
    note right of Submitted
        Request submitted for review
        Awaiting assignment
    end note
    
    note right of Scheduled
        Worker assigned and scheduled
        Work is pending
    end note
    
    note right of Done
        Work completed successfully
        Ready for closure
    end note
    
    note right of Failed
        Work attempted but failed
        Can be rescheduled
    end note
    
    note right of Declined
        Request declined by management
        Ready for closure
    end note
    
    note right of Closed
        Final state
        Request is complete
    end note
```

## 6. Worker Specialization and Assignment Logic

```mermaid
flowchart TD
    A[Tenant Request] --> B{Analyze Title & Description}
    
    B --> C{Contains Plumbing Keywords?}
    C -->|Yes| P[Plumbing Specialization]
    C -->|No| D{Contains Electrical Keywords?}
    
    D -->|Yes| E[Electrical Specialization]
    D -->|No| F{Contains HVAC Keywords?}
    
    F -->|Yes| H[HVAC Specialization]
    F -->|No| G{Contains Lock Keywords?}
    
    G -->|Yes| L[Locksmith Specialization]
    G -->|No| I{Contains Paint Keywords?}
    
    I -->|Yes| PA[Painting Specialization]
    I -->|No| J{Contains Wood/Carpentry Keywords?}
    
    J -->|Yes| C2[Carpentry Specialization]
    J -->|No| K{Contains Appliance Keywords?}
    
    K -->|Yes| AP[Appliance Repair Specialization]
    K -->|No| GM[General Maintenance]
    
    P --> M[Find Workers with Plumbing Skills]
    E --> N[Find Workers with Electrical Skills]
    H --> O[Find Workers with HVAC Skills]
    L --> Q[Find Workers with Locksmith Skills]
    PA --> R[Find Workers with Painting Skills]
    C2 --> S[Find Workers with Carpentry Skills]
    AP --> T[Find Workers with Appliance Skills]
    GM --> U[Find Workers with General Maintenance]
    
    M --> V[Calculate Worker Scores]
    N --> V
    O --> V
    Q --> V
    R --> V
    S --> V
    T --> V
    U --> V
    
    V --> W{Check Availability}
    W -->|Available| X[Assign Best Match]
    W -->|Not Available| Y[Queue for Next Available]
    
    X --> Z[Schedule Work]
    Y --> Z
```

## 7. Aggregate Boundaries and Context Map

```mermaid
graph TB
  %% Aggregates
  subgraph PropertyAggregate
    Property[Property — Aggregate Root]
    PropertyAddress[PropertyAddress — VO]
    TenantRef[Tenant — Aggregate Member]
  end

  subgraph TenantRequestAggregate
    TenantRequest[TenantRequest — Aggregate Root]
    TenantRequestChange[TenantRequestChange — Entity]
    ServiceWorkScheduleInfo[ServiceWorkScheduleInfo — VO]
  end

  subgraph WorkerAggregate
    Worker[Worker — Aggregate Root]
    WorkAssignment[WorkAssignment — Entity]
  end

  subgraph DomainServices
    TenantRequestBusinessService[TenantRequestBusinessService]
    WorkerAssignmentPolicyService[WorkerAssignmentPolicyService]
    RequestTitleGenerator[RequestTitleGenerator]
    TenantRequestUrgencyPolicy[TenantRequestUrgencyPolicy]
    TenantRequestStatusPolicy[TenantRequestStatusPolicy]
    PropertyDomainService[PropertyDomainService]
    RequestWorkflowManager[RequestWorkflowManager]
    UnitSchedulingService[UnitSchedulingService]
  end

  subgraph ValueObjects
    PersonContactInfo[PersonContactInfo]
    SchedulingSlot[SchedulingSlot]
    NotificationData[NotificationData]
    PropertyMetrics[PropertyMetrics]
    UnitMixAnalysis[UnitMixAnalysis]
  end

  %% References and interactions
  Property -.->|propertyId| TenantRequest
  TenantRef -.->|tenantId| TenantRequest
  Worker -.->|workerEmail| TenantRequest

  DomainServices --> Property
  DomainServices --> TenantRequest
  DomainServices --> Worker

  %% Composition
  Property --> PropertyAddress
  TenantRequest --> ServiceWorkScheduleInfo
  Worker --> WorkAssignment

  %% VO usage
  TenantRequest -.-> SchedulingSlot
  Worker -.-> PersonContactInfo
  Property -.-> UnitMixAnalysis
  Property -.-> PropertyMetrics
  TenantRequest -.-> NotificationData
```

## 8. Clean Architecture Layers

```mermaid
graph TB
    subgraph "WebUI Layer"
        Pages[Razor Pages]
        Controllers[Controllers]
        Models[View Models]
    end
    
    subgraph "Application Layer"
        AppServices[Application Services]
        Queries[Query Handlers]
        Commands[Command Handlers]
        Behaviors[Behaviors]
    end
    
    subgraph "Domain Layer"
        Entities[Domain Entities]
        ValueObjects[Value Objects]
        DomainServices[Domain Services]
        DomainEvents[Domain Events]
        Specifications[Specifications]
    end
    
    subgraph "Infrastructure Layer"
        Repositories[Repositories]
        DbContext[EF Core DbContext]
        EventHandlers[Event Handlers]
        ExternalServices[External Services]
    end
    
    Pages --> Models
    Controllers --> Models
    Pages --> AppServices
    Controllers --> AppServices
    
    AppServices --> Commands
    AppServices --> Queries
    AppServices --> Behaviors
    
    Commands --> Entities
    Commands --> DomainServices
    Queries --> Specifications
    Behaviors --> DomainEvents
    
    Repositories --> Entities
    DbContext --> ValueObjects
    EventHandlers --> DomainEvents
    ExternalServices --> DomainServices
    
    AppServices -.-> Repositories
    Repositories -.-> DbContext
```

## 9. Business Rules and Validation Flow

```mermaid
flowchart LR
    subgraph "Request Submission Rules"
        R1[Max 5 requests per 24h]
        R2[Max 2 emergency per 7 days]
        R3[No duplicate active requests]
        R4[Title & Description required]
    end
    
    subgraph "Worker Assignment Rules"
        W1[Worker must be active]
        W2[Specialization match required]
        W3[Max 2 assignments per day]
        W4[No time conflicts]
        W5[Available on scheduled date]
    end
    
    subgraph "Status Transition Rules"
        S1[Draft → Submitted only]
        S2[Submitted → Scheduled/Declined]
        S3[Scheduled → Done/Failed]
        S4[Failed → Scheduled allowed]
        S5[Done/Declined → Closed]
    end
    
    TenantRequest --> R1
    TenantRequest --> R2
    TenantRequest --> R3
    TenantRequest --> R4
    
    Worker --> W1
    Worker --> W2
    Worker --> W3
    Worker --> W4
    Worker --> W5
    
    TenantRequest --> S1
    TenantRequest --> S2
    TenantRequest --> S3
    TenantRequest --> S4
    TenantRequest --> S5
```

## 10. Integration and Event Publishing

```mermaid
sequenceDiagram
    participant T as Tenant
    participant TR as TenantRequest
    participant ES as EventStore
    participant NH as NotificationHandler
    participant MH as MetricsHandler
    participant AH as AuditHandler
    
    T->>TR: Submit Request
    TR->>TR: Validate Business Rules
    TR->>ES: Publish TenantRequestSubmittedEvent
    
    ES->>NH: Handle Event
    ES->>MH: Handle Event
    ES->>AH: Handle Event
    
    NH->>NH: Send Email Notification
    MH->>MH: Update Performance Metrics
    AH->>AH: Log Audit Trail
    
    Note over TR,AH: Async event processing ensures<br/>loose coupling between aggregates
```

These diagrams provide a comprehensive view of your domain model, covering:

1. **Core entities and relationships** - The main aggregates and how they relate
2. **Value objects** - Self-validating, immutable objects that encapsulate business concepts
3. **Domain events** - How the system communicates changes across boundaries
4. **Domain services** - Business logic that doesn't belong to a single aggregate
5. **Status flows** - How tenant requests progress through their lifecycle
6. **Worker assignment logic** - Complex business rules for matching workers to requests
7. **Aggregate boundaries** - Clean separation of concerns following DDD principles
8. **Clean architecture** - How the layers interact while maintaining dependency rules
9. **Business rules** - Key validation and business logic constraints
10. **Integration patterns** - How events enable loose coupling and async processing

The diagrams show a well-designed domain with proper encapsulation, clear boundaries, and sophisticated business logic handling the complexities of rental property maintenance management.
