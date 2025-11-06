using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Domain.Exceptions;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Entities;

public class PropertyTests
{
    [Fact]
    public void Property_ShouldBeCreated_WithValidParameters()
    {
        // Arrange
        var address = new PropertyAddress("123", "Main Street", "City", "12345");
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");
        var units = new List<string> { "101", "102", "103" };

        // Act
        var property = new Property("Test Property", "PROP001", address, "555-1234", superintendent, units,
            "noreply@test.com");

        // Assert
        property.Should().NotBeNull();
        property.Name.Should().Be("Test Property");
        property.Code.Should().Be("PROP001");
        property.Address.Should().Be(address);
        property.PhoneNumber.Should().Be("555-1234");
        property.Superintendent.Should().Be(superintendent);
        property.Units.Should().BeEquivalentTo(units);
        property.NoReplyEmailAddress.Should().Be("noreply@test.com");
        property.DomainEvents.Should().HaveCount(1);
        property.DomainEvents.First().Should().BeOfType<PropertyRegisteredEvent>();
    }

    [Theory]
    [InlineData("", "PROP001")] // Empty name
    [InlineData("Property", "")] // Empty code
    public void Property_ShouldThrowException_WithInvalidStringParameters(string name, string code)
    {
        // Arrange
        var address = new PropertyAddress("123", "Main Street", "City", "12345");
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");
        var units = new List<string> { "101" };

        // Act & Assert
        Action act = () => new Property(name, code, address, "555-1234", superintendent, units, "noreply@test.com");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Property_ShouldThrowException_WithNullAddress()
    {
        // Arrange
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");
        var units = new List<string> { "101" };

        // Act & Assert
        Action act = () =>
            new Property("Test Property", "PROP001", null!, "555-1234", superintendent, units, "noreply@test.com");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Property_ShouldThrowException_WithNullSuperintendent()
    {
        // Arrange
        var address = new PropertyAddress("123", "Main Street", "City", "12345");
        var units = new List<string> { "101" };

        // Act & Assert
        Action act = () =>
            new Property("Test Property", "PROP001", address, "555-1234", null!, units, "noreply@test.com");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Property_ShouldThrowException_WithEmptyUnits()
    {
        // Arrange
        var address = new PropertyAddress("123", "Main Street", "City", "12345");
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");
        var emptyUnits = new List<string>();

        // Act & Assert
        Action act = () => new Property("Test Property", "PROP001", address, "555-1234", superintendent, emptyUnits,
            "noreply@test.com");
        act.Should().Throw<PropertyDomainException>()
            .WithMessage("*must have at least one unit*");
    }

    [Fact]
    public void Property_ShouldThrowException_WithDuplicateUnits()
    {
        // Arrange
        var address = new PropertyAddress("123", "Main Street", "City", "12345");
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");
        var duplicateUnits = new List<string> { "101", "102", "101" }; // Duplicate 101

        // Act & Assert
        Action act = () => new Property("Test Property", "PROP001", address, "555-1234", superintendent, duplicateUnits,
            "noreply@test.com");
        act.Should().Throw<PropertyDomainException>()
            .WithMessage("*duplicate unit numbers*");
    }

    [Fact]
    public void RegisterTenant_ShouldAddTenantToProperty()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");

        // Act
        var tenant = property.RegisterTenant(tenantContact, "101");

        // Assert
        tenant.Should().NotBeNull();
        tenant.ContactInfo.Should().Be(tenantContact);
        tenant.UnitNumber.Should().Be("101");
        tenant.PropertyId.Should().Be(property.Id);
        property.Tenants.Should().HaveCount(1);
        property.Tenants.Should().Contain(tenant);
        property.DomainEvents.Should().Contain(e => e is TenantRegisteredEvent);
    }

    [Fact]
    public void RegisterTenant_ShouldThrowException_WithNullContactInfo()
    {
        // Arrange
        var property = CreateTestProperty();

        // Act & Assert
        Action act = () => property.RegisterTenant(null!, "101");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterTenant_ShouldThrowException_WithEmptyUnitNumber()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");

        // Act & Assert
        Action act = () => property.RegisterTenant(tenantContact, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterTenant_ShouldThrowException_WithNonExistentUnit()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");

        // Act & Assert
        Action act = () => property.RegisterTenant(tenantContact, "999"); // Unit doesn't exist
        act.Should().Throw<PropertyDomainException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public void RegisterTenant_ShouldThrowException_WithOccupiedUnit()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenant1Contact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");
        var tenant2Contact = new PersonContactInfo("Bob", "Johnson", "bob@test.com", "555-8888");

        property.RegisterTenant(tenant1Contact, "101"); // Occupy unit 101

        // Act & Assert
        Action act = () => property.RegisterTenant(tenant2Contact, "101"); // Try to occupy same unit
        act.Should().Throw<PropertyDomainException>()
            .WithMessage("*already occupied*");
    }

    [Fact]
    public void UpdateSuperintendent_ShouldUpdateSuperintendentSuccessfully()
    {
        // Arrange
        var property = CreateTestProperty();
        var newSuperintendent = new PersonContactInfo("New", "Super", "newsuper@test.com", "555-0000");

        // Act
        property.UpdateSuperintendent(newSuperintendent);

        // Assert
        property.Superintendent.Should().Be(newSuperintendent);
        property.DomainEvents.Should().Contain(e => e is SuperintendentChangedEvent);
    }

    [Fact]
    public void UpdateSuperintendent_ShouldThrowException_WithNullSuperintendent()
    {
        // Arrange
        var property = CreateTestProperty();

        // Act & Assert
        var act = () => property.UpdateSuperintendent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddUnit_ShouldAddUnitSuccessfully()
    {
        // Arrange
        var property = CreateTestProperty();

        // Act
        property.AddUnit("104");

        // Assert
        property.Units.Should().Contain("104");
        property.DomainEvents.Should().Contain(e => e is UnitAddedEvent);
    }

    [Fact]
    public void AddUnit_ShouldThrowException_WithDuplicateUnit()
    {
        // Arrange
        var property = CreateTestProperty();

        // Act & Assert
        var act = () => property.AddUnit("101"); // Unit already exists
        act.Should().Throw<PropertyDomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveUnit_ShouldRemoveUnitSuccessfully()
    {
        // Arrange
        var property = CreateTestProperty();

        // Act
        property.RemoveUnit("103");

        // Assert
        property.Units.Should().NotContain("103");
        property.DomainEvents.Should().Contain(e => e is UnitRemovedEvent);
    }

    [Fact]
    public void RemoveUnit_ShouldThrowException_WithNonExistentUnit()
    {
        // Arrange
        var property = CreateTestProperty();

        // Act & Assert
        var act = () => property.RemoveUnit("999");
        act.Should().Throw<PropertyDomainException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public void RemoveUnit_ShouldThrowException_WithOccupiedUnit()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");
        property.RegisterTenant(tenantContact, "101"); // Occupy unit 101

        // Act & Assert
        var act = () => property.RemoveUnit("101");
        act.Should().Throw<PropertyDomainException>()
            .WithMessage("*currently occupied*");
    }

    [Fact]
    public void IsUnitAvailable_ShouldReturnCorrectAvailability()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");
        property.RegisterTenant(tenantContact, "101"); // Occupy unit 101

        // Act & Assert
        property.IsUnitAvailable("101").Should().BeFalse(); // Occupied
        property.IsUnitAvailable("102").Should().BeTrue(); // Available
        property.IsUnitAvailable("999").Should().BeFalse(); // Doesn't exist
    }

    [Fact]
    public void GetAvailableUnits_ShouldReturnCorrectUnits()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");
        property.RegisterTenant(tenantContact, "101"); // Occupy unit 101

        // Act
        var availableUnits = property.GetAvailableUnits().ToList();

        // Assert
        availableUnits.Should().HaveCount(2);
        availableUnits.Should().Contain("102");
        availableUnits.Should().Contain("103");
        availableUnits.Should().NotContain("101");
    }

    [Fact]
    public void GetOccupancyRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var property = CreateTestProperty(); // 3 units total
        var tenant1Contact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");
        var tenant2Contact = new PersonContactInfo("Bob", "Johnson", "bob@test.com", "555-8888");

        property.RegisterTenant(tenant1Contact, "101"); // 1 occupied
        property.RegisterTenant(tenant2Contact, "102"); // 2 occupied

        // Act
        var occupancyRate = property.GetOccupancyRate();

        // Assert
        occupancyRate.Should().BeApproximately(0.6667, 0.001); // 2/3 = 0.6667
    }

    [Fact]
    public void CalculateMetrics_ShouldReturnCorrectMetrics()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9999");
        property.RegisterTenant(tenantContact, "101");

        // Act
        var metrics = property.CalculateMetrics();

        // Assert
        metrics.TotalUnits.Should().Be(3);
        metrics.OccupiedUnits.Should().Be(1);
        metrics.VacantUnits.Should().Be(2);
        metrics.OccupancyRate.Should().BeApproximately(0.3333, 0.001);
        metrics.RequiresAttention.Should().BeTrue(); // Occupancy < 80%
    }

    private static Property CreateTestProperty()
    {
        var address = new PropertyAddress("123", "Test Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Superintendent", "john@super.com", "555-1234");
        var units = new List<string> { "101", "102", "103" };

        return new Property("Test Property", "PROP001", address, "555-1234", superintendent, units, "noreply@test.com");
    }
}