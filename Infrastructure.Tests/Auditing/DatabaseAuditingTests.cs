using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace RentalRepairs.Infrastructure.Tests.Auditing;

/// <summary>
/// ? Issue #13 Fix Validation: Database Context Auditing Tests
/// Simplified test suite without external mocking dependencies
/// </summary>
public class DatabaseAuditingTests
{
    private readonly ITestOutputHelper _output;

    public DatabaseAuditingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldApplyCreationAudit_WhenEntityAdded()
    {
        // Arrange
        using var context = CreateTestContext();
        
        var property = new Property(
            "Test Property",
            "TP001",
            new PropertyAddress("123", "Main St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Doe", "john@test.com"),
            new List<string> { "101", "102" },
            "noreply@test.com");

        // Act
        await context.Properties.AddAsync(property);
        var result = await context.SaveChangesAsync();

        // Assert - Property creation might save additional related entities and trigger domain events
        result.Should().BeGreaterThanOrEqualTo(1, "At least the property should be saved");
        property.CreatedBy.Should().NotBeNullOrEmpty("CreatedBy should be populated during audit");
        property.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), "CreatedAt should be set to current time");
        property.IsDeleted.Should().BeFalse("New entities should not be marked as deleted");

        _output.WriteLine($"Creation audit applied: CreatedBy={property.CreatedBy}, CreatedAt={property.CreatedAt}");
        _output.WriteLine($"UpdatedBy={property.UpdatedBy}, UpdatedAt={property.UpdatedAt}"); // Log for diagnostics
        _output.WriteLine($"Total entities saved: {result}");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldApplyModificationAudit_WhenEntityModified()
    {
        // Arrange
        using var context = CreateTestContext();

        var property = new Property(
            "Test Property",
            "TP001",
            new PropertyAddress("123", "Main St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Doe", "john@test.com"),
            new List<string> { "101", "102" },
            "noreply@test.com");

        await context.Properties.AddAsync(property);
        await context.SaveChangesAsync();

        var originalCreatedBy = property.CreatedBy;
        var originalCreatedAt = property.CreatedAt;

        // Wait a bit to ensure different timestamps
        await Task.Delay(100);

        // Act - Modify the entity
        context.Entry(property).State = EntityState.Modified;
        var result = await context.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThanOrEqualTo(1, "At least the property should be updated");
        property.CreatedBy.Should().Be(originalCreatedBy, "CreatedBy should not change during modification");
        property.CreatedAt.Should().Be(originalCreatedAt, "CreatedAt should not change during modification");
        property.UpdatedBy.Should().NotBeNullOrEmpty("UpdatedBy should be populated during modification");
        property.UpdatedAt.Should().NotBeNull("UpdatedAt should be set during modification");

        _output.WriteLine($"Modification audit applied: UpdatedBy={property.UpdatedBy}, UpdatedAt={property.UpdatedAt}");
        _output.WriteLine($"Creation audit preserved: CreatedBy={property.CreatedBy}, CreatedAt={property.CreatedAt}");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldApplySoftDeletion_WhenEntityDeleted()
    {
        // Arrange
        using var context = CreateTestContext();

        var property = new Property(
            "Test Property",
            "TP001",
            new PropertyAddress("123", "Main St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Doe", "john@test.com"),
            new List<string> { "101", "102" },
            "noreply@test.com");

        await context.Properties.AddAsync(property);
        await context.SaveChangesAsync();

        // Act - Delete the entity
        context.Properties.Remove(property);
        var result = await context.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThanOrEqualTo(1, "At least the property should be soft deleted");
        property.IsDeleted.Should().BeTrue("Entity should be marked as deleted");
        property.DeletedBy.Should().NotBeNullOrEmpty("DeletedBy should be populated");
        property.DeletedAt.Should().NotBeNull("DeletedAt should be set");

        _output.WriteLine($"Soft deletion applied: DeletedBy={property.DeletedBy}, DeletedAt={property.DeletedAt}");
    }

    [Fact]
    public void QueryFilter_ShouldExcludeSoftDeletedEntities_ByDefault()
    {
        // Arrange
        using var context = CreateTestContext();
        
        var activeProperty = new Property(
            "Active Property",
            "AP001",
            new PropertyAddress("123", "Main St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Doe", "john@test.com"),
            new List<string> { "101" },
            "noreply@test.com");

        var deletedProperty = new Property(
            "Deleted Property",
            "DP001",
            new PropertyAddress("456", "Oak St", "Test City", "12345"),
            "555-5678",
            new PersonContactInfo("Jane", "Smith", "jane@test.com"),
            new List<string> { "201" },
            "noreply2@test.com");

        // Mark one as deleted
        deletedProperty.SoftDelete("test.user@example.com");

        context.Properties.AddRange(activeProperty, deletedProperty);
        context.SaveChanges();

        // Act
        var visibleProperties = context.Properties.ToList();

        // Assert
        visibleProperties.Should().HaveCount(1, "Only non-deleted entities should be visible");
        visibleProperties[0].Code.Should().Be("AP001", "The active property should be visible");

        _output.WriteLine($"Query filter working: {visibleProperties.Count} active properties visible, soft-deleted entities excluded");
    }

    [Fact]
    public async Task AuditService_ShouldHandleBasicAuditing_Gracefully()
    {
        // Arrange - Use a simpler entity (Worker) for basic auditing test
        using var context = CreateTestContext();
        
        var workerContact = new PersonContactInfo("Test", "Worker", "test@worker.com", "555-1234");
        var worker = new Worker(workerContact);

        // Act - Use async method for consistency
        context.Workers.Add(worker);
        var result = await context.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThanOrEqualTo(1, "At least the worker should be saved");
        worker.CreatedBy.Should().NotBeNullOrEmpty("CreatedBy should be populated during audit");
        worker.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), "CreatedAt should be set to current time");

        _output.WriteLine($"Basic auditing working: CreatedBy={worker.CreatedBy}");
        _output.WriteLine($"Total entities saved: {result}");
    }

    [Fact]
    public async Task Worker_Creation_Should_Apply_Audit_Information()
    {
        // Arrange
        using var context = CreateTestContext();
        
        var workerContact = new PersonContactInfo("Alice", "Johnson", "alice@worker.com", "555-5678");
        var worker = new Worker(workerContact);

        // Act
        await context.Workers.AddAsync(worker);
        var result = await context.SaveChangesAsync();

        // Assert - Worker creation might trigger domain events that create additional entities
        result.Should().BeGreaterThanOrEqualTo(1, "At least the worker should be saved");
        worker.CreatedBy.Should().NotBeNullOrEmpty("CreatedBy should be populated during audit");
        worker.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), "CreatedAt should be set to current time");
        worker.IsDeleted.Should().BeFalse("New entities should not be marked as deleted");

        _output.WriteLine($" Worker creation audit applied: CreatedBy={worker.CreatedBy}, CreatedAt={worker.CreatedAt}");
        _output.WriteLine($" Total entities saved: {result}");
    }

    [Fact]
    public async Task Audit_Fields_Should_Be_Preserved_Across_Operations()
    {
        // Arrange
        using var context = CreateTestContext();
        
        var workerContact = new PersonContactInfo("Bob", "Smith", "bob@worker.com", "555-9999");
        var worker = new Worker(workerContact);

        // Act - Create worker
        context.Workers.Add(worker);
        await context.SaveChangesAsync();

        var originalCreatedBy = worker.CreatedBy;
        var originalCreatedAt = worker.CreatedAt;

        // Wait to ensure different timestamp
        await Task.Delay(100);

        // Modify worker
        worker.SetSpecialization("Electrical");
        await context.SaveChangesAsync();

        // Assert
        worker.CreatedBy.Should().Be(originalCreatedBy, "CreatedBy should be preserved during updates");
        worker.CreatedAt.Should().Be(originalCreatedAt, "CreatedAt should be preserved during updates");
        worker.UpdatedBy.Should().NotBeNullOrEmpty("UpdatedBy should be set during modification");
        worker.UpdatedAt.Should().NotBeNull("UpdatedAt should be set during modification");
        worker.UpdatedAt.Should().BeAfter(originalCreatedAt, "UpdatedAt should be after CreatedAt");

        _output.WriteLine($"Audit fields preserved: CreatedBy={worker.CreatedBy}, UpdatedBy={worker.UpdatedBy}");
        _output.WriteLine($"Timestamps: Created={worker.CreatedAt}, Updated={worker.UpdatedAt}");
    }

    #region Private Methods

    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        // Create simple logger for testing
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ApplicationDbContext>();

        // Use basic auditing without complex dependencies
        return new ApplicationDbContext(options, logger);
    }

    #endregion
}

/// <summary>
/// ? Integration tests for audit configuration 
/// </summary>
public class AuditConfigurationTests
{
    private readonly ITestOutputHelper _output;

    public AuditConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ModelConfiguration_ShouldConfigureAuditFields_Correctly()
    {
        // Arrange
        using var context = CreateConfigurationTestContext();

        // Act
        var model = context.Model;
        var propertyEntityType = model.FindEntityType(typeof(Property));

        // Assert
        propertyEntityType.Should().NotBeNull();
        
        var createdAtProperty = propertyEntityType!.FindProperty(nameof(IAuditableEntity.CreatedAt));
        var createdByProperty = propertyEntityType.FindProperty(nameof(IAuditableEntity.CreatedBy));
        var isDeletedProperty = propertyEntityType.FindProperty(nameof(ISoftDeletableEntity.IsDeleted));
        var rowVersionProperty = propertyEntityType.FindProperty(nameof(IVersionedEntity.RowVersion));

        createdAtProperty.Should().NotBeNull();
        createdByProperty.Should().NotBeNull();
        isDeletedProperty.Should().NotBeNull();
        rowVersionProperty.Should().NotBeNull();

        createdAtProperty!.IsNullable.Should().BeFalse();
        createdByProperty!.IsNullable.Should().BeFalse();
        createdByProperty.GetMaxLength().Should().Be(256);
        isDeletedProperty!.GetDefaultValue().Should().Be(false);
        rowVersionProperty!.IsConcurrencyToken.Should().BeTrue();

        _output.WriteLine("All audit fields configured correctly in EF model");
    }

    private static ApplicationDbContext CreateConfigurationTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ApplicationDbContext>();

        return new ApplicationDbContext(options, logger);
    }
}