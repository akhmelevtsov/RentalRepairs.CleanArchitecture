using FluentAssertions;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Infrastructure.Persistence;
using RentalRepairs.Infrastructure.Persistence.Repositories;
using RentalRepairs.Infrastructure.Persistence.Configurations;
using Xunit;

namespace RentalRepairs.Infrastructure.Tests.Persistence.Repositories;

public class RepositoryIntegrationTests
{
    [Fact]
    public void Repository_Types_Should_Implement_Correct_Interfaces()
    {
        // Verify repository types implement their interfaces
        var propertyRepoType = typeof(PropertyRepository);
        var tenantRepoType = typeof(TenantRepository);
        var tenantRequestRepoType = typeof(TenantRequestRepository);
        var workerRepoType = typeof(WorkerRepository);

        propertyRepoType.Should().BeAssignableTo<IPropertyRepository>();
        tenantRepoType.Should().BeAssignableTo<ITenantRepository>();
        tenantRequestRepoType.Should().BeAssignableTo<ITenantRequestRepository>();
        workerRepoType.Should().BeAssignableTo<IWorkerRepository>();
    }

    [Fact]
    public void Repository_Types_Should_Have_Required_Methods()
    {
        // Test PropertyRepository methods
        var propertyRepoMethods = typeof(PropertyRepository).GetMethods().Select(m => m.Name).ToList();
        propertyRepoMethods.Should().Contain("GetByIdAsync");
        propertyRepoMethods.Should().Contain("GetByCodeAsync");
        propertyRepoMethods.Should().Contain("GetAllAsync");
        propertyRepoMethods.Should().Contain("AddAsync");
        propertyRepoMethods.Should().Contain("Update");
        propertyRepoMethods.Should().Contain("Remove");
        propertyRepoMethods.Should().Contain("SaveChangesAsync");
        propertyRepoMethods.Should().Contain("ExistsByCodeAsync"); // Updated method name

        // Test TenantRequestRepository methods
        var tenantRequestRepoMethods = typeof(TenantRequestRepository).GetMethods().Select(m => m.Name).ToList();
        tenantRequestRepoMethods.Should().Contain("GetByIdAsync");
        tenantRequestRepoMethods.Should().Contain("GetByStatusAsync");
        tenantRequestRepoMethods.Should().Contain("GetByUrgencyLevelAsync");
        tenantRequestRepoMethods.Should().Contain("GetPendingRequestsAsync");
        tenantRequestRepoMethods.Should().Contain("GetOverdueRequestsAsync");
        tenantRequestRepoMethods.Should().Contain("CountByStatusAsync");
        tenantRequestRepoMethods.Should().Contain("ExistsByCodeAsync"); // Updated method name
    }

    [Fact]
    public void Repository_Types_Should_Be_In_Correct_Namespace()
    {
        var propertyRepoType = typeof(PropertyRepository);
        var tenantRepoType = typeof(TenantRepository);
        var tenantRequestRepoType = typeof(TenantRequestRepository);
        var workerRepoType = typeof(WorkerRepository);

        propertyRepoType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Repositories");
        tenantRepoType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Repositories");
        tenantRequestRepoType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Repositories");
        workerRepoType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Repositories");
    }

    [Fact]
    public void ApplicationDbContext_Type_Should_Be_Correct()
    {
        var contextType = typeof(ApplicationDbContext);
        
        contextType.Should().NotBeNull();
        contextType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence");
        
        // Check that it has the required DbSets
        var properties = contextType.GetProperties();
        var propertyNames = properties.Select(p => p.Name).ToList();
        
        propertyNames.Should().Contain("Properties");
        propertyNames.Should().Contain("Tenants");
        propertyNames.Should().Contain("TenantRequests");
        propertyNames.Should().Contain("Workers");
    }

    [Fact]
    public void Entity_Configurations_Should_Exist()
    {
        // Verify that entity configurations exist and are in correct namespace
        var propertyConfigType = typeof(PropertyConfiguration);
        var tenantConfigType = typeof(TenantConfiguration);
        var tenantRequestConfigType = typeof(TenantRequestConfiguration);
        var workerConfigType = typeof(WorkerConfiguration);

        propertyConfigType.Should().NotBeNull();
        propertyConfigType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Configurations");

        tenantConfigType.Should().NotBeNull();
        tenantConfigType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Configurations");

        tenantRequestConfigType.Should().NotBeNull();
        tenantRequestConfigType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Configurations");

        workerConfigType.Should().NotBeNull();
        workerConfigType.Namespace.Should().Be("RentalRepairs.Infrastructure.Persistence.Configurations");
    }
}