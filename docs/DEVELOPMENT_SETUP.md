# Development Setup Guide

## Prerequisites

### Required Software
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server LocalDB** - Included with Visual Studio or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **Git** - [Download](https://git-scm.com/downloads)

### Recommended IDEs
- **Visual Studio 2022** (Community/Professional/Enterprise) - [Download](https://visualstudio.microsoft.com/)
- **Visual Studio Code** with C# extension - [Download](https://code.visualstudio.com/)
- **JetBrains Rider** - [Download](https://www.jetbrains.com/rider/)

### Optional Tools
- **SQL Server Management Studio (SSMS)** - For database inspection
- **Postman** - For API testing (if API layer is added)
- **Git GUI** - SourceTree, GitKraken, or GitHub Desktop

## Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/akhmelevtsov/RentalRepairs.CleanArchitecture.git
cd RentalRepairs.CleanArchitecture
```

### 2. Restore Dependencies
```bash
# Restore all NuGet packages
dotnet restore

# Verify all projects build
dotnet build
```

### 3. Database Setup

#### Option A: Automatic Setup (Recommended)
The application will automatically:
1. Create the database on first run
2. Apply all migrations
3. Seed demo data
4. Generate credentials file

```bash
# Just run the application
dotnet run --project src/WebUI/
```

#### Option B: Manual Database Setup
```bash
# Navigate to Infrastructure project
cd src/Infrastructure/

# Add migration (if creating new ones)
dotnet ef migrations add InitialCreate --startup-project ../WebUI/

# Update database
dotnet ef database update --startup-project ../WebUI/
```

### 4. Run the Application
```bash
# Run from solution root
dotnet run --project src/WebUI/

# Or with specific profile
dotnet run --project src/WebUI/ --launch-profile "https"
```

### 5. Access the Application
- **HTTPS**: https://localhost:7001
- **HTTP**: http://localhost:7000

## Demo Credentials

The application includes pre-configured demo users:

### System Administrator
- **Email**: admin@demo.com
- **Password**: Demo123!
- **Access**: Full system administration

### Property Superintendent  
- **Email**: super.johnson@sunset.com
- **Password**: Demo123!
- **Property**: Sunset Apartments
- **Access**: Property management, request approval

### Tenants
- **Email**: tenant1.unit101@sunset.com
- **Password**: Demo123!
- **Unit**: 101, Sunset Apartments

- **Email**: tenant2.unit202@sunset.com  
- **Password**: Demo123!
- **Unit**: 202, Sunset Apartments

### Workers
- **Email**: plumber.smith@workers.com
- **Password**: Demo123!
- **Specialization**: Plumber

- **Email**: electrician.jones@workers.com
- **Password**: Demo123!  
- **Specialization**: Electrician

## Development Workflow

### Project Structure
```
src/
├── Domain/                 # Core business logic
├── Application/            # Use cases and CQRS
├── Infrastructure/         # Data access and external services
├── WebUI/                 # Razor Pages presentation
├── CompositionRoot/       # Dependency injection setup
├── Domain.Tests/          # Domain unit tests
├── Application.Tests/     # Application layer tests
├── Infrastructure.Tests/  # Infrastructure integration tests
└── WebUI.Tests/          # Presentation layer tests
```

### Building the Solution
```bash
# Build all projects
dotnet build

# Build specific project
dotnet build src/Domain/

# Build in Release mode
dotnet build --configuration Release
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test src/Domain.Tests/

# Run tests with filter
dotnet test --filter "Category=Unit"
```

### Database Operations

#### View Current Migrations
```bash
dotnet ef migrations list --project src/Infrastructure/ --startup-project src/WebUI/
```

#### Add New Migration
```bash
dotnet ef migrations add [MigrationName] --project src/Infrastructure/ --startup-project src/WebUI/
```

#### Update Database
```bash
dotnet ef database update --project src/Infrastructure/ --startup-project src/WebUI/
```

#### Remove Last Migration
```bash
dotnet ef migrations remove --project src/Infrastructure/ --startup-project src/WebUI/
```

#### Reset Database (Development Only)
```bash
dotnet ef database drop --project src/Infrastructure/ --startup-project src/WebUI/
dotnet ef database update --project src/Infrastructure/ --startup-project src/WebUI/
```

## Code Style and Standards

### Coding Conventions
- **C# Naming**: PascalCase for public members, camelCase for private fields
- **File Organization**: One class per file, organized by feature
- **Folder Structure**: Group by feature, then by technical concern
- **Documentation**: XML documentation for public APIs

### Architecture Patterns
- **Domain**: Rich entities, value objects, domain services
- **Application**: CQRS with MediatR, application services
- **Infrastructure**: Repository pattern, Entity Framework
- **Presentation**: Page Model pattern, view models



## Development Tips


### Debug Configuration
- **Visual Studio**: F5 to run with debugger
- **VS Code**: Use launch.json configuration
- **Command Line**: `dotnet run` with `--configuration Debug`

### Environment Variables
Create `appsettings.Development.json` in WebUI project:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RentalRepairs;Trusted_Connection=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Common Issues and Solutions

#### Database Connection Issues
```bash
# Check LocalDB instances
sqllocaldb info

# Start LocalDB
sqllocaldb start MSSQLLocalDB

# Recreate LocalDB instance
sqllocaldb delete MSSQLLocalDB
sqllocaldb create MSSQLLocalDB
```

#### Build Issues
```bash
# Clean solution
dotnet clean

# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore
```

#### Port Conflicts
- Modify `launchSettings.json` in WebUI/Properties/
- Change `applicationUrl` to different ports
- Update any hardcoded URLs in configuration

## Testing Guidelines

### Unit Test Structure
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var entity = new Entity(validParameters);
    
    // Act  
    var result = entity.MethodUnderTest();
    
    // Assert
    result.Should().Be(expectedValue);
}
```

### Integration Test Setup
```csharp
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    
    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
}
```

### Test Categories
- **Unit**: Fast, isolated, no external dependencies
- **Integration**: Database, HTTP, external services
- **Functional**: End-to-end user scenarios

## Troubleshooting

### Common Commands
```bash
# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks

# Verify project dependencies
dotnet list package

# Check for outdated packages
dotnet list package --outdated
```

### Log Analysis
- Check application logs in `logs/` directory
- Review EF Core query logs for performance issues
- Monitor authentication failures
- Track domain event processing

## Additional Resources

### Documentation
- [Clean Architecture Guide](ARCHITECTURE_IMPLEMENTATION.md)

### External References
- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design - Eric Evans](https://domainlanguage.com/ddd/)
- [CQRS Journey - Microsoft](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10))
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

---

This setup guide should get you up and running with the RentalRepairs Clean Architecture project. For additional help, refer to the architecture documentation or create an issue in the repository.