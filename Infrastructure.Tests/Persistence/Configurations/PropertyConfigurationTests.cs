using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Infrastructure.Persistence;
using Xunit;

namespace RentalRepairs.Infrastructure.Tests.Persistence.Configurations;

/// <summary>
/// Tests for PropertyConfiguration to ensure Units value converter works correctly
/// </summary>
public class PropertyConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public PropertyConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<ApplicationDbContext>>();
        var mockAuditService = new Mock<IAuditService>();
        var mockDomainEventPublisher = new Mock<IDomainEventPublisher>();

        _context = new ApplicationDbContext(options, mockLogger.Object, mockAuditService.Object,
            mockDomainEventPublisher.Object);
    }

    [Fact]
    public async Task Units_ValueConverter_HandlesSingleUnit_ReturnsCorrectCount()
    {
        // Arrange - Create property with single unit (minimum allowed)
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");
        var singleUnit = new List<string> { "101" };

        var property = new Domain.Entities.Property(
            "Single Unit Property",
            "SUP001",
            address,
            "555-0123",
            superintendent,
            singleUnit,
            "noreply@test.com");

        // Act - Save and retrieve
        await _context.Properties.AddAsync(property);
        await _context.SaveChangesAsync();

        // Clear context to ensure fresh load
        _context.Entry(property).State = EntityState.Detached;

        var retrievedProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Code == "SUP001");

        // Assert
        retrievedProperty.Should().NotBeNull();
        retrievedProperty!.Units.Should().NotBeNull();
        retrievedProperty.Units.Should().HaveCount(1);
        retrievedProperty.Units.Should().Contain("101");
    }

    [Fact]
    public async Task Units_ValueConverter_HandlesMultipleUnits_ReturnsCorrectCount()
    {
        // Arrange - Create property with multiple units
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");
        var multipleUnits = new List<string> { "101", "102", "103", "104", "105" };

        var property = new Domain.Entities.Property(
            "Multi Unit Property",
            "MUP001",
            address,
            "555-0123",
            superintendent,
            multipleUnits,
            "noreply@test.com");

        // Act - Save and retrieve
        await _context.Properties.AddAsync(property);
        await _context.SaveChangesAsync();

        // Clear context to ensure fresh load
        _context.Entry(property).State = EntityState.Detached;

        var retrievedProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Code == "MUP001");

        // Assert
        retrievedProperty.Should().NotBeNull();
        retrievedProperty!.Units.Should().NotBeNull();
        retrievedProperty.Units.Should().HaveCount(5);
        retrievedProperty.Units.Should().Contain(new[] { "101", "102", "103", "104", "105" });
    }

    [Fact]
    public async Task Units_Projection_WorksCorrectlyInQueries()
    {
        // Arrange - Create multiple properties with different unit counts
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");

        // Property 1: 3 units
        var property1 = new Domain.Entities.Property(
            "Property 1", "P001", address, "555-0123", superintendent,
            new List<string> { "A1", "A2", "A3" }, "noreply@test.com");

        // Property 2: 1 unit (minimum allowed)
        var property2 = new Domain.Entities.Property(
            "Property 2", "P002", address, "555-0123", superintendent,
            new List<string> { "B1" }, "noreply@test.com");

        // Property 3: 2 units
        var property3 = new Domain.Entities.Property(
            "Property 3", "P003", address, "555-0123", superintendent,
            new List<string> { "X1", "X2" }, "noreply@test.com");

        // Act - Save properties
        await _context.Properties.AddRangeAsync(property1, property2, property3);
        await _context.SaveChangesAsync();

        // Test projection query similar to GetPropertiesWithStatsQueryHandler
        var propertyStats = await _context.Properties
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                TotalUnits = p.Units.Count
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        // Assert
        propertyStats.Should().HaveCount(3);

        var prop1Stats = propertyStats.First(p => p.Code == "P001");
        prop1Stats.TotalUnits.Should().Be(3);

        var prop2Stats = propertyStats.First(p => p.Code == "P002");
        prop2Stats.TotalUnits.Should().Be(1);

        var prop3Stats = propertyStats.First(p => p.Code == "P003");
        prop3Stats.TotalUnits.Should().Be(2);
    }

    [Fact]
    public async Task Units_SaveAndRetrieve_MaintainsCorrectValues()
    {
        // Arrange - Create a property using the domain model
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");
        var units = new List<string> { "101", "102", "103", "201", "202" };

        var property = new Domain.Entities.Property(
            "Test Property",
            "TP001",
            address,
            "555-0123",
            superintendent,
            units,
            "noreply@test.com");

        // Act - Save to database
        await _context.Properties.AddAsync(property);
        await _context.SaveChangesAsync();

        // Clear context to ensure fresh load
        _context.Entry(property).State = EntityState.Detached;

        // Retrieve from database
        var retrievedProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Code == "TP001");

        // Assert
        retrievedProperty.Should().NotBeNull();
        retrievedProperty!.Units.Should().NotBeNull();
        retrievedProperty.Units.Should().HaveCount(5);
        retrievedProperty.Units.Should().Contain(new[] { "101", "102", "103", "201", "202" });
    }

    [Fact]
    public async Task Units_QueryProjection_HandlesVariousScenarios()
    {
        // Arrange - Create properties with different unit configurations
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");

        var properties = new[]
        {
            new Domain.Entities.Property("Small Property", "SP001", address, "555-0123", superintendent,
                new List<string> { "1" }, "noreply@test.com"),
            new Domain.Entities.Property("Medium Property", "MP001", address, "555-0123", superintendent,
                new List<string> { "A", "B", "C", "D", "E" }, "noreply@test.com"),
            new Domain.Entities.Property("Large Property", "LP001", address, "555-0123", superintendent,
                new List<string> { "101", "102", "103", "201", "202", "203", "301", "302", "303", "401" },
                "noreply@test.com")
        };

        await _context.Properties.AddRangeAsync(properties);
        await _context.SaveChangesAsync();

        // Act - Test GetPropertiesWithStats-like query
        var stats = await _context.Properties
            .Select(p => new
            {
                p.Name,
                p.Code,
                TotalUnits = p.Units.Count,
                VacantUnits = p.Units.Count - p.Tenants.Count,
                OccupancyRate = p.Units.Count > 0 ? (double)p.Tenants.Count / p.Units.Count * 100 : 0
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        // Assert
        stats.Should().HaveCount(3);

        // Large Property
        var largePropertyStats = stats.First(s => s.Code == "LP001");
        largePropertyStats.TotalUnits.Should().Be(10);
        largePropertyStats.VacantUnits.Should().Be(10); // No tenants
        largePropertyStats.OccupancyRate.Should().Be(0);

        // Medium Property
        var mediumPropertyStats = stats.First(s => s.Code == "MP001");
        mediumPropertyStats.TotalUnits.Should().Be(5);
        mediumPropertyStats.VacantUnits.Should().Be(5);
        mediumPropertyStats.OccupancyRate.Should().Be(0);

        // Small Property
        var smallPropertyStats = stats.First(s => s.Code == "SP001");
        smallPropertyStats.TotalUnits.Should().Be(1);
        smallPropertyStats.VacantUnits.Should().Be(1);
        smallPropertyStats.OccupancyRate.Should().Be(0);
    }

    [Fact]
    public async Task Units_ValueConverter_HandlesSpecialCharacters()
    {
        // Arrange - Test with unit numbers containing various characters
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");
        var specialUnits = new List<string> { "A-1", "B 2", "C3", "D-4 E" };

        var property = new Domain.Entities.Property(
            "Special Units Property",
            "SUP002",
            address,
            "555-0123",
            superintendent,
            specialUnits,
            "noreply@test.com");

        // Act - Save and retrieve
        await _context.Properties.AddAsync(property);
        await _context.SaveChangesAsync();

        // Clear context to ensure fresh load
        _context.Entry(property).State = EntityState.Detached;

        var retrievedProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Code == "SUP002");

        // Assert
        retrievedProperty.Should().NotBeNull();
        retrievedProperty!.Units.Should().NotBeNull();
        retrievedProperty.Units.Should().HaveCount(4);
        retrievedProperty.Units.Should().Contain(new[] { "A-1", "B 2", "C3", "D-4 E" });
    }

    [Fact]
    public async Task Units_ValueConverter_MaintainsOrderAfterSaveAndLoad()
    {
        // Arrange - Test that unit order is preserved
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");
        var orderedUnits = new List<string> { "Z99", "A01", "M50", "B02", "X88" }; // Intentionally not alphabetical

        var property = new Domain.Entities.Property(
            "Ordered Units Property",
            "OUP001",
            address,
            "555-0123",
            superintendent,
            orderedUnits,
            "noreply@test.com");

        // Act - Save and retrieve
        await _context.Properties.AddAsync(property);
        await _context.SaveChangesAsync();

        // Clear context to ensure fresh load
        _context.Entry(property).State = EntityState.Detached;

        var retrievedProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Code == "OUP001");

        // Assert - Order should be preserved
        retrievedProperty.Should().NotBeNull();
        retrievedProperty!.Units.Should().NotBeNull();
        retrievedProperty.Units.Should().HaveCount(5);
        retrievedProperty.Units.Should().ContainInOrder("Z99", "A01", "M50", "B02", "X88");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}