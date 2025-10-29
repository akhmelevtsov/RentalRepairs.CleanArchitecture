using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Services;

public class WorkerAssignmentServiceTests
{
    [Theory]
    [InlineData("Plumbing issue", "Leaky faucet", "Plumbing")]
    [InlineData("Electrical problem", "Outlet not working", "Electrical")]
    [InlineData("Heating not working", "HVAC system down", "HVAC")]
    [InlineData("Door handle broken", "Door needs fixing", "Carpentry")] // Fix: "door" maps to Carpentry
    [InlineData("Window issues", "Broken window pane", "General Maintenance")]
    [InlineData("Paint touch up", "Wall needs paint", "Painting")]
    public void DetermineRequiredSpecialization_ShouldReturnCorrectSpecialization(
        string title, 
        string description, 
        string expectedSpecialization)
    {
        // Act - Use Worker's static method
        var specialization = Worker.DetermineRequiredSpecialization(title, description);

        // Assert
        specialization.Should().Be(expectedSpecialization);
    }

    [Fact]
    public void DetermineRequiredSpecialization_ShouldReturnGeneralMaintenance_ForUnknownKeywords()
    {
        // Act - Use Worker's static method
        var specialization = Worker.DetermineRequiredSpecialization("Random issue", "Something unclear");

        // Assert
        specialization.Should().Be("General Maintenance");
    }

    [Theory]
    [InlineData("Water leak in bathroom", "Plumbing")]
    [InlineData("Power outlet sparking", "Electrical")]
    [InlineData("Air conditioning broken", "HVAC")]
    [InlineData("Window handle broken", "General Maintenance")] // Fix: Use window instead of door to avoid carpentry
    public void DetermineRequiredSpecialization_ShouldBeCaseInsensitive(
        string description, 
        string expectedSpecialization)
    {
        // Act - Use Worker's static method with uppercase input
        var specialization = Worker.DetermineRequiredSpecialization("ISSUE", description.ToUpperInvariant());

        // Assert
        specialization.Should().Be(expectedSpecialization);
    }

    [Fact]
    public void DetermineRequiredSpecialization_ShouldHandleEmptyInput()
    {
        // Act - Use Worker's static method with empty input
        var specialization = Worker.DetermineRequiredSpecialization("", "");

        // Assert
        specialization.Should().Be("General Maintenance");
    }

    [Fact]
    public void DetermineRequiredSpecialization_ShouldPrioritizeKeywords()
    {
        // Test keyword prioritization in the algorithm
        var plumbingSpecialization = Worker.DetermineRequiredSpecialization("Water heater repair", "Electric water heater leaking");
        var electricalSpecialization = Worker.DetermineRequiredSpecialization("Light switch", "Electrical outlet power issue");
        
        // Should prioritize plumbing keywords (water, leak) over electrical
        plumbingSpecialization.Should().Be("Plumbing");
        electricalSpecialization.Should().Be("Electrical");
    }

    private static TenantRequest CreateTestRequest(string title, string description)
    {
        var property = new Property(
            "Test Property",
            "TP001",
            new PropertyAddress("123", "Test St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Doe", "john@test.com"),
            new List<string> { "101" },
            "noreply@test.com");

        var tenant = property.RegisterTenant(
            new PersonContactInfo("Jane", "Smith", "jane@test.com"), 
            "101");

        return tenant.CreateRequest(title, description, "Normal");
    }
}