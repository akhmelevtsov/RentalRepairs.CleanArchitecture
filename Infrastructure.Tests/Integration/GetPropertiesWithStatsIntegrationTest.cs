using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Queries.Properties.GetPropertiesWithStats;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Infrastructure.Persistence;
using Xunit;

namespace RentalRepairs.Infrastructure.Tests.Integration;

/// <summary>
/// Integration test to verify that the GetPropertiesWithStatsQueryHandler works correctly
/// with the fixed Units value converter from PropertyConfiguration
/// </summary>
public class GetPropertiesWithStatsIntegrationTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetPropertiesWithStatsQueryHandler _handler;

    public GetPropertiesWithStatsIntegrationTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<ApplicationDbContext>>();
        var mockAuditService = new Mock<IAuditService>();
        var mockDomainEventPublisher = new Mock<IDomainEventPublisher>();

        _context = new ApplicationDbContext(options, mockLogger.Object, mockAuditService.Object,
            mockDomainEventPublisher.Object);
        _handler = new GetPropertiesWithStatsQueryHandler(_context);
    }

    [Fact]
    public async Task GetPropertiesWithStats_AfterUnitsValueConverterFix_ReturnsCorrectUnitCounts()
    {
        // Arrange - Create test properties with different unit configurations
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");

        // Property 1: Single unit
        var property1 = new Domain.Entities.Property(
            "Single Unit Property", "SUP001", address, "555-0123", superintendent,
            new List<string> { "101" }, "noreply@test.com");

        // Property 2: Multiple units
        var property2 = new Domain.Entities.Property(
            "Multi Unit Property", "MUP001", address, "555-0123", superintendent,
            new List<string> { "A1", "A2", "A3", "B1", "B2" }, "noreply@test.com");

        // Property 3: Many units
        var property3 = new Domain.Entities.Property(
            "Large Property", "LP001", address, "555-0123", superintendent,
            new List<string> { "101", "102", "103", "201", "202", "203", "301", "302", "303", "401" },
            "noreply@test.com");

        await _context.Properties.AddRangeAsync(property1, property2, property3);
        await _context.SaveChangesAsync();

        // Act - Execute the GetPropertiesWithStatsQuery
        var query = new GetPropertiesWithStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify that all properties return correct unit counts
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var resultsList = result.ToList();

        // Find each property by code and verify unit counts
        var singleUnitProperty = resultsList.First(p => p.Code == "SUP001");
        singleUnitProperty.TotalUnits.Should().Be(1);
        singleUnitProperty.Units.Should().HaveCount(1);
        singleUnitProperty.Units.Should().Contain("101");

        var multiUnitProperty = resultsList.First(p => p.Code == "MUP001");
        multiUnitProperty.TotalUnits.Should().Be(5);
        multiUnitProperty.Units.Should().HaveCount(5);
        multiUnitProperty.Units.Should().Contain(new[] { "A1", "A2", "A3", "B1", "B2" });

        var largeProperty = resultsList.First(p => p.Code == "LP001");
        largeProperty.TotalUnits.Should().Be(10);
        largeProperty.Units.Should().HaveCount(10);
        largeProperty.Units.Should().Contain(new[]
            { "101", "102", "103", "201", "202", "203", "301", "302", "303", "401" });

        // Verify the issue is fixed - no properties should have 0 units when they actually have units
        resultsList.Should().NotContain(p => p.TotalUnits == 0, "because all properties have at least one unit");
    }

    [Fact]
    public async Task GetPropertiesWithStats_VerifyOccupancyCalculationsWithUnitsCount()
    {
        // Arrange - Create property with tenants to test occupancy calculations
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");

        var property = new Domain.Entities.Property(
            "Test Property", "TP001", address, "555-0123", superintendent,
            new List<string> { "101", "102", "103", "104", "105" }, "noreply@test.com");

        // Add some tenants (2 out of 5 units occupied)
        var tenant1ContactInfo = new PersonContactInfo("Jane", "Doe", "jane@test.com", "555-0001");
        var tenant2ContactInfo = new PersonContactInfo("John", "Smith", "john@test.com", "555-0002");

        var tenant1 = property.RegisterTenant(tenant1ContactInfo, "101");
        var tenant2 = property.RegisterTenant(tenant2ContactInfo, "103");

        await _context.Properties.AddAsync(property);
        await _context.SaveChangesAsync();

        // Act
        var query = new GetPropertiesWithStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify occupancy calculations work correctly with proper unit counts
        result.Should().HaveCount(1);
        var propertyStats = result.First();

        propertyStats.TotalUnits.Should().Be(5);
        propertyStats.OccupiedUnits.Should().Be(2);
        propertyStats.VacantUnits.Should().Be(3);
        propertyStats.OccupancyRate.Should().BeApproximately(40.0, 0.1); // 2/5 = 40%

        // Verify Units collection is properly populated
        propertyStats.Units.Should().HaveCount(5);
        propertyStats.Units.Should().Contain(new[] { "101", "102", "103", "104", "105" });
    }

    [Fact]
    public async Task GetPropertiesWithStats_VerifyResultsAreSortedByName()
    {
        // Arrange - Create properties in non-alphabetical order
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "super@test.com", "555-0456");

        var propertyZ = new Domain.Entities.Property(
            "Zebra Property", "ZP001", address, "555-0123", superintendent,
            new List<string> { "Z1", "Z2", "Z3" }, "noreply@test.com");

        var propertyA = new Domain.Entities.Property(
            "Alpha Property", "AP001", address, "555-0123", superintendent,
            new List<string> { "A1" }, "noreply@test.com");

        var propertyM = new Domain.Entities.Property(
            "Middle Property", "MP001", address, "555-0123", superintendent,
            new List<string> { "M1", "M2" }, "noreply@test.com");

        await _context.Properties.AddRangeAsync(propertyZ, propertyA, propertyM);
        await _context.SaveChangesAsync();

        // Act
        var query = new GetPropertiesWithStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify properties are sorted by name and have correct unit counts
        result.Should().HaveCount(3);
        var resultsList = result.ToList();

        resultsList[0].Name.Should().Be("Alpha Property");
        resultsList[0].TotalUnits.Should().Be(1);

        resultsList[1].Name.Should().Be("Middle Property");
        resultsList[1].TotalUnits.Should().Be(2);

        resultsList[2].Name.Should().Be("Zebra Property");
        resultsList[2].TotalUnits.Should().Be(3);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}