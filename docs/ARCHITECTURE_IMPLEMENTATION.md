# Architecture Implementation Guide

## Overview

This document details the architectural decisions and implementation patterns used in the RentalRepairs Clean Architecture portfolio project.

## Clean Architecture Implementation

### Layer Dependencies

```
Presentation Layer (WebUI)
    ↓ depends on
Application Layer 
    ↓ depends on
Domain Layer (Core)
    ↑ implements
Infrastructure Layer
```

### Dependency Inversion Principle

- **Domain Layer**: Contains no external dependencies, only business logic
- **Application Layer**: Depends only on Domain abstractions
- **Infrastructure Layer**: Implements Domain and Application interfaces
- **Presentation Layer**: Depends on Application layer through abstractions

## Domain-Driven Design Patterns

### Aggregates

#### Property Aggregate
- **Root**: `Property` entity
- **Entities**: Property management data
- **Value Objects**: `PropertyAddress`, `PersonContactInfo`
- **Invariants**: Property code uniqueness, unit availability

#### Tenant Aggregate  
- **Root**: `Tenant` entity
- **Entities**: Tenant registration data
- **Business Rules**: Unit assignment validation, request submission policies

#### TenantRequest Aggregate
- **Root**: `TenantRequest` entity
- **Entities**: Request lifecycle management
- **Value Objects**: `SchedulingSlot`, `WorkAssignment`
- **Invariants**: Status transition rules, worker assignment constraints

#### Worker Aggregate
- **Root**: `Worker` entity
- **Entities**: Worker capabilities and assignments
- **Business Rules**: Specialization matching, availability constraints

### Domain Events

#### Property Events
- `PropertyRegisteredEvent`
- `TenantRegisteredEvent`
- `SuperintendentChangedEvent`
- `UnitAddedEvent`
- `UnitRemovedEvent`

#### TenantRequest Events
- `TenantRequestCreatedEvent`
- `TenantRequestSubmittedEvent`
- `TenantRequestScheduledEvent`
- `TenantRequestCompletedEvent`
- `TenantRequestClosedEvent`
- `TenantRequestDeclinedEvent`

#### Worker Events
- `WorkerRegisteredEvent`
- `WorkerAssignedEvent`
- `WorkCompletedEvent`
- `WorkerSpecializationChangedEvent`

### Value Objects

#### PropertyAddress
```csharp
public sealed class PropertyAddress : ValueObject
{
    public string StreetAddress { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string FullAddress => $"{StreetAddress}, {City}, {State} {ZipCode}";
}
```

#### PersonContactInfo
```csharp
public sealed class PersonContactInfo : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }
    public string EmailAddress { get; }
    public string PhoneNumber { get; }
    public string FullName => $"{FirstName} {LastName}";
}
```

#### SchedulingSlot
```csharp
public sealed class SchedulingSlot : ValueObject
{
    public DateTime Date { get; }
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public int DurationHours { get; }
}
```

### Specifications Pattern

#### Property Specifications
- `PropertyByCodeSpecification`
- `PropertiesByCitySpecification`
- `PropertiesBySuperintendentEmailSpecification`
- `PropertyWithAvailableUnitsSpecification`

#### TenantRequest Specifications
- `TenantRequestsByDateRangeSpecification`
- `TenantRequestByStatusSpecification`
- `TenantRequestByUrgencySpecification`
- `PendingTenantRequestsSpecification`
- `OverdueTenantRequestsSpecification`

#### Worker Specifications
- `WorkerBySpecializationSpecification`
- `WorkersAvailableForSchedulingSpecification`
- `ActiveWorkersSpecification`

## CQRS Implementation

### Command Pattern

#### Command Structure
```csharp
public class ExampleCommand : IRequest<ExampleResult>
{
    public Guid Id { get; set; }
    public string Data { get; set; }
}

public class ExampleCommandHandler : IRequestHandler<ExampleCommand, ExampleResult>
{
    public async Task<ExampleResult> Handle(ExampleCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate business rules
        // 2. Execute domain operations
        // 3. Persist changes
        // 4. Return result
    }
}
```

#### Key Commands Implemented

**Property Management**
- `RegisterPropertyCommand`
- `RegisterTenantCommand`

**Request Lifecycle**
- `CreateTenantRequestCommand`
- `SubmitTenantRequestCommand`
- `ScheduleServiceWorkCommand`
- `ReportWorkCompletedCommand`
- `CloseRequestCommand`
- `DeclineTenantRequestCommand`

**Worker Management**
- `RegisterWorkerCommand`
- `UpdateWorkerSpecializationCommand`

### Query Pattern

#### Query Structure
```csharp
public class ExampleQuery : IRequest<ExampleDto>
{
    public Guid Id { get; set; }
    public string Filter { get; set; }
}

public class ExampleQueryHandler : IRequestHandler<ExampleQuery, ExampleDto>
{
    public async Task<ExampleDto> Handle(ExampleQuery request, CancellationToken cancellationToken)
    {
        // 1. Apply specifications/filters
        // 2. Retrieve data
        // 3. Map to DTOs
        // 4. Return result
    }
}
```

#### Key Queries Implemented

**Property Queries**
- `GetPropertyByIdQuery`
- `GetPropertyByCodeQuery`
- `GetPropertiesQuery`
- `GetAvailableUnitsQuery`

**TenantRequest Queries**
- `GetTenantRequestByIdQuery`
- `GetTenantRequestsQuery`
- `GetWorkerRequestsQuery`
- `GetTenantRequestStatusSummaryQuery`

**Worker Queries**
- `GetWorkerByIdQuery`
- `GetWorkersQuery`
- `GetAvailableWorkersQuery`

### MediatR Pipeline Behaviors

#### Validation Behavior
```csharp
public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // FluentValidation integration for input validation
}
```

#### Performance Monitoring Behavior
```csharp
public class PerformanceMonitoringBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Request performance logging and monitoring
}
```

## Application Services

### Service Layer Pattern
- **PropertyService**: Property and tenant management
- **TenantRequestService**: Request lifecycle orchestration
- **WorkerService**: Worker management and assignment
- **NotifyPartiesService**: Cross-cutting notification concerns

### Service Implementation Example
```csharp
public class PropertyService : IPropertyService
{
    private readonly IMediator _mediator;

    public async Task<PropertyDto> RegisterPropertyAsync(RegisterPropertyRequest request)
    {
        var command = request.Adapt<RegisterPropertyCommand>();
        return await _mediator.Send(command);
    }
}
```

## Infrastructure Implementation

### Repository Pattern

#### Base Repository
```csharp
public interface IRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(ISpecification<T> specification);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task SaveChangesAsync();
}
```

#### Specialized Repositories
- `IPropertyRepository`
- `ITenantRepository`
- `ITenantRequestRepository`
- `IWorkerRepository`

### Entity Framework Configuration

#### DbContext Setup
```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantRequest> TenantRequests => Set<TenantRequest>();
    public DbSet<Worker> Workers => Set<Worker>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

#### Entity Configurations
- `PropertyConfiguration`
- `TenantConfiguration`
- `TenantRequestConfiguration`
- `WorkerConfiguration`

### Authentication & Authorization

#### Cookie-Based Authentication
```csharp
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });
```

#### Role-Based Authorization
- **SystemAdmin**: Full system access
- **PropertySuperintendent**: Property management
- **Tenant**: Submit and track requests
- **Worker**: View and complete assignments

## Presentation Layer

### Razor Pages Architecture

#### Page Model Pattern
```csharp
public class ExamplePageModel : PageModel
{
    private readonly IMediator _mediator;

    public async Task<IActionResult> OnGetAsync()
    {
        var query = new ExampleQuery();
        ViewModel = await _mediator.Send(query);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var command = Model.Adapt<ExampleCommand>();
        await _mediator.Send(command);
        return RedirectToPage();
    }
}
```

#### View Models
- Presentation-specific DTOs
- Validation attributes
- UI-focused properties
- Mapping to/from Application DTOs

### Mapster Configuration

#### Domain to DTO Mapping
```csharp
TypeAdapterConfig<Property, PropertyDto>
    .NewConfig()
    .Map(dest => dest.Address, src => src.Address.FullAddress)
    .Map(dest => dest.SuperintendentName, src => src.Superintendent.FullName);
```

#### DTO to ViewModel Mapping
```csharp
TypeAdapterConfig<PropertyDto, PropertyViewModel>
    .NewConfig()
    .Map(dest => dest.DisplayName, src => $"{src.Name} ({src.Code})")
    .Map(dest => dest.TenantCount, src => src.Tenants.Count);
```

## Testing Strategy

### Unit Testing
- **Domain Tests**: Entity behavior, business rules, value objects
- **Application Tests**: Command/query handlers, application services
- **Infrastructure Tests**: Repository implementations, configurations

### Integration Testing
- **Database Tests**: Entity Framework configurations, migrations
- **API Tests**: End-to-end request processing
- **Authentication Tests**: Security and authorization flows

### Test Architecture
```csharp
public class ExampleHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsExpectedResult()
    {
        // Arrange
        var handler = new ExampleCommandHandler(mockRepository.Object);
        var command = new ExampleCommand { /* test data */ };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // Additional assertions
    }
}
```

## Key Architecture Benefits

### 1. Testability
- Pure domain logic isolated from infrastructure
- Dependency injection enables easy mocking
- Clear separation allows focused unit tests

### 2. Maintainability
- Single Responsibility Principle throughout
- Clear architectural boundaries
- Consistent patterns and conventions

### 3. Flexibility
- Infrastructure can be swapped without affecting business logic
- UI technology can be changed independently
- External services easily mocked or replaced

### 4. Scalability
- CQRS allows read/write optimization
- Horizontal scaling of individual concerns
- Performance tuning at appropriate layers

### 5. Domain Focus
- Business logic remains pure and testable
- Rich domain models express business concepts clearly
- Domain experts can understand and validate logic

## Implementation Guidelines

### Do's
✓ Keep domain layer pure with no external dependencies  
✓ Use CQRS for clear separation of read/write operations  
✓ Implement rich domain entities with business logic  
✓ Apply specifications pattern for complex queries  
✓ Use value objects for data integrity  
✓ Implement proper aggregate boundaries  
✓ Use domain events for cross-cutting concerns  

### Don'ts
✗ Don't put business logic in controllers or page models  
✗ Don't create anemic domain models  
✗ Don't violate layer dependencies  
✗ Don't mix query and command operations  
✗ Don't expose domain entities directly to UI  
✗ Don't create circular dependencies between aggregates  

---

This architecture demonstrates a production-ready implementation of Clean Architecture principles with modern .NET development practices.