using Xunit;
using FluentAssertions;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Application.Tests.Services;

/// <summary>
/// Tests for worker specialization filtering in assignment context
/// </summary>
public class WorkerSpecializationFilteringTests
{
    [Fact]
    public void Worker_HasSpecializedSkills_ElectricianShouldNotHandlePlumbing()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("Jessica", "Martinez", "electrician.martinez@workers.com");
        var worker = new Worker(contactInfo);
        worker.SetSpecialization("Electrician");

        // Act
        var canHandlePlumbing = worker.HasSpecializedSkills("Plumbing");
        var canHandleElectrical = worker.HasSpecializedSkills("Electrical");

        // Assert
        canHandlePlumbing.Should().BeFalse("An Electrician should not handle Plumbing work");
        canHandleElectrical.Should().BeTrue("An Electrician should handle Electrical work");
    }

    [Fact]
    public void Worker_HasSpecializedSkills_PlumberShouldNotHandleElectrical()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("Mike", "Smith", "mike.smith@plumber.com");
        var worker = new Worker(contactInfo);
        worker.SetSpecialization("Plumber");

        // Act
        var canHandleElectrical = worker.HasSpecializedSkills("Electrical");
        var canHandlePlumbing = worker.HasSpecializedSkills("Plumbing");

        // Assert
        canHandleElectrical.Should().BeFalse("A Plumber should not handle Electrical work");
        canHandlePlumbing.Should().BeTrue("A Plumber should handle Plumbing work");
    }

    [Fact]
    public void Worker_HasSpecializedSkills_GeneralMaintenanceShouldHandleAnything()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("John", "Doe", "john.doe@maintenance.com");
        var worker = new Worker(contactInfo);
        worker.SetSpecialization("General Maintenance");

        // Act
        var canHandlePlumbing = worker.HasSpecializedSkills("Plumbing");
        var canHandleElectrical = worker.HasSpecializedSkills("Electrical");
        var canHandleHVAC = worker.HasSpecializedSkills("HVAC");

        // Assert
        canHandlePlumbing.Should().BeTrue("General Maintenance should handle Plumbing work");
        canHandleElectrical.Should().BeTrue("General Maintenance should handle Electrical work");
        canHandleHVAC.Should().BeTrue("General Maintenance should handle HVAC work");
    }

    [Theory]
    [InlineData("Hi, there's a slow leak under the kitchen sink", "Plumbing")]
    [InlineData("Power outlet sparking in bedroom", "Electrical")]
    [InlineData("Air conditioning not working", "HVAC")]
    [InlineData("Need to paint the living room walls", "Painting")]
    [InlineData("Cabinet door is broken", "Carpentry")]
    [InlineData("Random issue with unclear description", "General Maintenance")]
    public void Worker_DetermineRequiredSpecialization_ShouldReturnCorrectSpecialization(
        string description, string expectedSpecialization)
    {
        // Act
        var specialization = Worker.DetermineRequiredSpecialization("Maintenance Request", description);

        // Assert
        specialization.Should().Be(expectedSpecialization);
    }

    [Fact]
    public void Worker_ValidateCanBeAssignedToRequest_ShouldThrowForWrongSpecialization()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("Jessica", "Martinez", "electrician.martinez@workers.com");
        var worker = new Worker(contactInfo);
        worker.SetSpecialization("Electrician");

        var futureDate = DateTime.UtcNow.AddDays(1);
        var workOrderNumber = "WO-001";
        var requiredSpecialization = "Plumbing"; // Electrician trying to do plumbing work

        // Act & Assert
        var exception = Assert.Throws<Domain.Exceptions.WorkerNotAvailableException>(
            () => worker.ValidateCanBeAssignedToRequest(futureDate, workOrderNumber, requiredSpecialization));

        exception.Message.Should().Contain("Worker does not have required specialization: Plumbing");
    }

    [Fact]
    public void Worker_ValidateCanBeAssignedToRequest_ShouldSucceedForCorrectSpecialization()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("Jessica", "Martinez", "electrician.martinez@workers.com");
        var worker = new Worker(contactInfo);
        worker.SetSpecialization("Electrician");

        var futureDate = DateTime.UtcNow.AddDays(1);
        var workOrderNumber = "WO-001";
        var requiredSpecialization = "Electrical"; // Electrician doing electrical work

        // Act & Assert - Should not throw
        worker.ValidateCanBeAssignedToRequest(futureDate, workOrderNumber, requiredSpecialization);
    }
}