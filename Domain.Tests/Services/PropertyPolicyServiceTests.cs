using FluentAssertions;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.ValueObjects;
using Xunit;

namespace RentalRepairs.Domain.Tests.Services;

/// <summary>
/// FIXED: Tests for the new pure domain service that follows DDD principles.
/// This replaces PropertyDomainServiceTests which tested a service that violated DDD.
/// </summary>
public class PropertyPolicyServiceTests
{
    private readonly PropertyPolicyService _policyService;

    public PropertyPolicyServiceTests()
    {
        _policyService = new PropertyPolicyService();
    }

    [Fact]
    public void ValidatePropertyCreation_Should_Succeed_With_Valid_Data()
    {
        // Arrange
        var name = "Test Property";
        var code = "TP001";
        var address = new PropertyAddress("123", "Test St", "Test City", "12345");
        var phoneNumber = "555-1234";
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-5678");
        var units = new List<string> { "101", "102", "103" };
        var noReplyEmail = "noreply@test.com";

        // Act & Assert - Should not throw
        _policyService.ValidatePropertyCreation(name, code, address, phoneNumber, superintendent, units, noReplyEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidatePropertyCreation_Should_Throw_For_Invalid_Name(string invalidName)
    {
        // Arrange
        var code = "TP001";
        var address = new PropertyAddress("123", "Test St", "Test City", "12345");
        var phoneNumber = "555-1234";
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-5678");
        var units = new List<string> { "101", "102" };
        var noReplyEmail = "noreply@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _policyService.ValidatePropertyCreation(invalidName, code, address, phoneNumber, superintendent, units,
                noReplyEmail));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidatePropertyCreation_Should_Throw_For_Invalid_Code(string invalidCode)
    {
        // Arrange
        var name = "Test Property";
        var address = new PropertyAddress("123", "Test St", "Test City", "12345");
        var phoneNumber = "555-1234";
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-5678");
        var units = new List<string> { "101", "102" };
        var noReplyEmail = "noreply@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _policyService.ValidatePropertyCreation(name, invalidCode, address, phoneNumber, superintendent, units,
                noReplyEmail));
    }

    [Fact]
    public void ValidatePropertyCreation_Should_Throw_For_Null_Address()
    {
        // Arrange
        var name = "Test Property";
        var code = "TP001";
        PropertyAddress address = null!;
        var phoneNumber = "555-1234";
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-5678");
        var units = new List<string> { "101", "102" };
        var noReplyEmail = "noreply@test.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _policyService.ValidatePropertyCreation(name, code, address, phoneNumber, superintendent, units,
                noReplyEmail));
    }

    [Fact]
    public void ValidatePropertyCreation_Should_Throw_For_Null_Superintendent()
    {
        // Arrange
        var name = "Test Property";
        var code = "TP001";
        var address = new PropertyAddress("123", "Test St", "Test City", "12345");
        var phoneNumber = "555-1234";
        PersonContactInfo superintendent = null!;
        var units = new List<string> { "101", "102" };
        var noReplyEmail = "noreply@test.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _policyService.ValidatePropertyCreation(name, code, address, phoneNumber, superintendent, units,
                noReplyEmail));
    }

    [Fact]
    public void ValidatePropertyCreation_Should_Throw_For_Empty_Units()
    {
        // Arrange
        var name = "Test Property";
        var code = "TP001";
        var address = new PropertyAddress("123", "Test St", "Test City", "12345");
        var phoneNumber = "555-1234";
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-5678");
        var units = new List<string>(); // Empty units
        var noReplyEmail = "noreply@test.com";

        // Act & Assert
        Assert.Throws<PropertyDomainException>(() =>
            _policyService.ValidatePropertyCreation(name, code, address, phoneNumber, superintendent, units,
                noReplyEmail));
    }

    [Fact]
    public void ValidatePropertyCreation_Should_Throw_For_Duplicate_Units()
    {
        // Arrange
        var name = "Test Property";
        var code = "TP001";
        var address = new PropertyAddress("123", "Test St", "Test City", "12345");
        var phoneNumber = "555-1234";
        var superintendent = new PersonContactInfo("John", "Doe", "john@test.com", "555-5678");
        var units = new List<string> { "101", "102", "101" }; // Duplicate unit
        var noReplyEmail = "noreply@test.com";

        // Act & Assert
        Assert.Throws<PropertyDomainException>(() =>
            _policyService.ValidatePropertyCreation(name, code, address, phoneNumber, superintendent, units,
                noReplyEmail));
    }

    [Fact]
    public void ValidateTenantRegistration_Should_Succeed_With_Valid_Data()
    {
        // Arrange
        var property = CreateTestProperty();
        var contactInfo = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9876");
        var unitNumber = "101";

        // Act & Assert - Should not throw
        _policyService.ValidateTenantRegistration(property, contactInfo, unitNumber);
    }

    [Fact]
    public void ValidateTenantRegistration_Should_Throw_For_Null_Property()
    {
        // Arrange
        Property property = null!;
        var contactInfo = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9876");
        var unitNumber = "101";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _policyService.ValidateTenantRegistration(property, contactInfo, unitNumber));
    }

    [Fact]
    public void ValidateTenantRegistration_Should_Throw_For_Null_ContactInfo()
    {
        // Arrange
        var property = CreateTestProperty();
        PersonContactInfo contactInfo = null!;
        var unitNumber = "101";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _policyService.ValidateTenantRegistration(property, contactInfo, unitNumber));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTenantRegistration_Should_Throw_For_Invalid_UnitNumber(string invalidUnitNumber)
    {
        // Arrange
        var property = CreateTestProperty();
        var contactInfo = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9876");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _policyService.ValidateTenantRegistration(property, contactInfo, invalidUnitNumber));
    }

    [Fact]
    public void ValidateTenantRegistration_Should_Throw_For_Nonexistent_Unit()
    {
        // Arrange
        var property = CreateTestProperty();
        var contactInfo = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9876");
        var nonexistentUnit = "999"; // This unit doesn't exist in the test property

        // Act & Assert
        Assert.Throws<PropertyDomainException>(() =>
            _policyService.ValidateTenantRegistration(property, contactInfo, nonexistentUnit));
    }

    [Fact]
    public void DetermineOptimalUnitAssignment_Should_Return_Preferred_Unit_When_Available()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContactInfo = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9876");
        var preferredUnit = "101";

        // Act
        var result = _policyService.DetermineOptimalUnitAssignment(property, tenantContactInfo, preferredUnit);

        // Assert
        result.Should().NotBeNull();
        result.IsAssignmentPossible.Should().BeTrue();
        result.RecommendedUnits.Should().Contain(preferredUnit);
        result.Reason.Should().Be("Preferred unit is available");
    }

    [Fact]
    public void DetermineOptimalUnitAssignment_Should_Return_Alternatives_When_Preferred_Not_Available()
    {
        // Arrange
        var property = CreateTestProperty();

        // Register a tenant to make unit 101 unavailable
        var existingTenantContact = new PersonContactInfo("John", "Existing", "john@test.com", "555-1111");
        property.RegisterTenant(existingTenantContact, "101");

        var tenantContactInfo = new PersonContactInfo("Jane", "Smith", "jane@test.com", "555-9876");
        var preferredUnit = "101"; // This unit is now occupied

        // Act
        var result = _policyService.DetermineOptimalUnitAssignment(property, tenantContactInfo, preferredUnit);

        // Assert
        result.Should().NotBeNull();
        result.IsAssignmentPossible.Should().BeTrue();
        result.RecommendedUnits.Should().NotContain(preferredUnit);
        result.RecommendedUnits.Should().Contain("102"); // Should recommend available units
        result.Reason.Should().Contain("not available");
    }

    private static Property CreateTestProperty()
    {
        return new Property(
            "Test Property",
            "TP001",
            new PropertyAddress("123", "Test St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Doe", "john@test.com", "555-5678"),
            new List<string> { "101", "102", "103" },
            "noreply@test.com");
    }
}