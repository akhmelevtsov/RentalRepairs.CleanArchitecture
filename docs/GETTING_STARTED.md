# Getting Started Guide

## Quick Start (5 Minutes)

### 1. Prerequisites Check
Ensure you have **.NET 8 SDK** installed:
```bash
dotnet --version
# Should show 8.0.x or higher
```

### 2. Clone and Run
```bash
# Clone the repository
git clone https://github.com/akhmelevtsov/RentalRepairs.CleanArchitecture.git
cd RentalRepairs.CleanArchitecture

# Restore packages and run
dotnet restore
dotnet run --project src/WebUI/

# Access at: https://localhost:5001
```

### 3. Login with Demo Credentials
- **Admin**: admin@demo.com / Demo123!
- **Tenant**: tenant1.unit101@sunset.com / Demo123!
- **Worker**: plumber.smith@workers.com / Demo123!

## What You'll See

### Admin Dashboard
- **System Overview**: Properties, tenants, workers, and requests summary
- **Property Management**: Add new properties and manage units
- **User Management**: View all users across the system
- **Request Oversight**: Monitor all maintenance requests

### Tenant Experience
- **Submit Requests**: Create maintenance requests with urgency levels
- **Track Progress**: Monitor request status and worker assignments
- **Request History**: View past requests and their outcomes
- **Property Information**: Access property and superintendent details

### Worker Dashboard  
- **Current Assignments**: View scheduled work assignments
- **Work History**: Track completed jobs and performance
- **Specialization Management**: Update skills and availability
- **Complete Work**: Report work completion with detailed notes

## Key Features to Explore

### 🏠 Property Management
1. **Register Properties** (Admin only)
   - Navigate to Properties → Register
   - Add property details, address, and superintendent info
   - System validates business rules automatically

2. **Unit Management**
   - Properties contain multiple rental units
   - Units can be assigned to tenants
   - Availability tracking for vacant units

### 🔧 Tenant Operations
1. **Submit Maintenance Request**
   - Navigate to Tenant Requests → Submit
   - Fill out detailed request form
   - Select urgency level (Normal, High, Emergency)
   - System validates submission policies

2. **Request Lifecycle**
   - **Draft** → **Submitted** → **Scheduled** → **Done** → **Closed**
   - Email notifications at each status change
   - Business rules enforce valid status transitions

### 👷 Worker Assignment
1. **Skill-Based Matching**
   - Workers have specializations (Plumber, Electrician, etc.)
   - System matches requests to appropriate workers
   - Availability and workload balancing

2. **Work Completion**
   - Workers report completion status
   - Success/failure tracking with detailed notes
   - Failed work gets rescheduled automatically

## Architecture Highlights

### Clean Architecture Layers
- **Domain**: Pure business logic with rich entities
- **Application**: CQRS with MediatR for use cases
- **Infrastructure**: Entity Framework, authentication, external services
- **WebUI**: Razor Pages with proper separation of concerns

### Domain-Driven Design
- **Aggregates**: Property, Tenant, Worker, TenantRequest
- **Value Objects**: PropertyAddress, PersonContactInfo, SchedulingSlot
- **Domain Events**: Cross-cutting communication
- **Specifications**: Complex query encapsulation

### CQRS Implementation
- **45+ Handlers**: Separate command and query operations
- **Commands**: RegisterProperty, SubmitTenantRequest, ScheduleWork, etc.
- **Queries**: GetProperties, GetTenantRequests, GetWorkers, etc.
- **Pipeline Behaviors**: Validation, performance monitoring

## Business Rules Demo

### Tenant Request Policies
1. **Rate Limiting**: Tenants can't submit requests too frequently
2. **Emergency Limits**: Maximum 3 emergency requests per month  
3. **Pending Limits**: Maximum 5 pending requests at once
4. **Status Validation**: Only valid status transitions allowed

### Worker Assignment Rules
1. **Specialization Matching**: Plumber for plumbing, electrician for electrical
2. **Availability Checking**: Workers can't be double-booked
3. **Daily Limits**: Maximum assignments per worker per day
4. **Time Conflicts**: Scheduling prevents overlapping assignments

### Property Management Rules
1. **Code Uniqueness**: Each property has unique identifier
2. **Unit Validation**: Units must exist before tenant assignment
3. **Superintendent Required**: Every property needs management contact
4. **Address Validation**: Complete address information required

## Testing the Architecture

### Unit Tests
```bash
# Run all tests
dotnet test

# Domain tests (business rules)
dotnet test src/Domain.Tests/

# Application tests (CQRS handlers)
dotnet test src/Application.Tests/
```

### Integration Tests
```bash
# Infrastructure tests (database)
dotnet test src/Infrastructure.Tests/

# WebUI tests (end-to-end)
dotnet test src/WebUI.Tests/
```

### Test Coverage
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Common Scenarios to Try

### Complete Maintenance Request Workflow
Experience the full lifecycle of a maintenance request from submission to completion:

1. **Tenant Submits Request**
   - **Login as Tenant** (tenant1.unit101@sunset.com / Demo123!)
   - Navigate to **Tenant Requests → Submit**
   - Fill out maintenance request (e.g., "Leaky kitchen faucet")
   - Select urgency level and submit

2. **Superintendent Assigns Worker**
   - **Login as Admin/Superintendent** (admin@demo.com / Demo123!)
   - Navigate to **Request Management**
   - View the submitted request
   - Assign appropriate worker based on specialization
   - Schedule work appointment

3. **Worker Completes Request**
   - **Login as Worker** (plumber.smith@workers.com / Demo123!)
   - Navigate to **My Assignments**
   - View assigned work details
   - Complete the work and report status
   - Add completion notes (success/failure details)

This workflow demonstrates the core business process and showcases:
- **Domain-driven design** with rich business entities
- **CQRS pattern** with separate command and query operations
- **Clean architecture** with proper separation of concerns
- **Business rule validation** at each step
- **Cross-cutting concerns** like notifications and auditing

## Development Tips

### Database Inspection
- LocalDB creates database automatically
- Use SQL Server Management Studio to inspect
- Connection: `(localdb)\MSSQLLocalDB`
- Database: `RentalRepairs`

### Code Navigation
- **Domain Entities**: `src/Domain/Entities/`
- **Command Handlers**: `src/Application/Commands/`
- **Query Handlers**: `src/Application/Queries/`
- **Repository Implementations**: `src/Infrastructure/Persistence/Repositories/`
- **Razor Pages**: `src/WebUI/Pages/`

## Understanding the Code

### Rich Domain Entities
```csharp
// Example: TenantRequest with business rules
public class TenantRequest : BaseEntity
{
    public void SubmitForReview()
    {
        ValidateCanBeSubmitted(); // Business rule validation
        Status = TenantRequestStatus.Submitted;
        AddDomainEvent(new TenantRequestSubmittedEvent(this));
    }
}
```

### CQRS Handlers
```csharp
// Example: Command handler
public class SubmitTenantRequestCommandHandler : IRequestHandler<SubmitTenantRequestCommand, Result>
{
    public async Task<Result> Handle(SubmitTenantRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Load aggregate
        // 2. Execute business operation  
        // 3. Persist changes
        // 4. Return result
    }
}
```

### Specifications Pattern
```csharp
// Example: Complex query encapsulation
public class TenantRequestsByDateRangeSpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestsByDateRangeSpecification(DateTime startDate, DateTime endDate)
        : base(tr => tr.CreatedAt >= startDate && tr.CreatedAt <= endDate)
    {
        AddInclude(tr => tr.Property);
        AddInclude(tr => tr.Tenant);
        AddOrderBy(tr => tr.CreatedAt);
    }
}
```



## Support and Resources

### Documentation
- [Architecture Implementation](ARCHITECTURE_IMPLEMENTATION.md)
- [Development Setup](DEVELOPMENT_SETUP.md)

### External Resources
- [Clean Architecture Blog](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://domainlanguage.com/ddd/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

---

The codebase demonstrates production-ready patterns and practices for building maintainable, testable, and scalable .NET applications.