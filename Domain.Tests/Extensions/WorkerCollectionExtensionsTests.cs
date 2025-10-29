using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Extensions;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Extensions;

/// <summary>
/// Tests for Worker collection extension methods added in Step 2: Push Logic to Aggregates.
/// Verifies that collection-level business operations work correctly.
/// </summary>
public class WorkerCollectionExtensionsTests
{
    #region GetAvailableForEmergency Tests

    [Fact]
    public void GetAvailableForEmergency_ActiveEmergencyCapableWorkers_ShouldReturnThem()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber@test.com"),
            CreateActiveWorker("Electrical", "electrician@test.com"),
            CreateInactiveWorker("Plumbing", "inactive@test.com") // Should be filtered out
        };

        // Act
        var result = workers.GetAvailableForEmergency();

        // Assert
        result.Should().HaveCount(2, "Should return only active emergency-capable workers");
        result.Should().OnlyContain(w => w.IsActive, "All returned workers should be active");
        result.Should().OnlyContain(w => w.IsEmergencyResponseCapable(), "All returned workers should be emergency capable");
    }

    [Fact]
    public void GetAvailableForEmergency_NoActiveWorkers_ShouldReturnEmpty()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateInactiveWorker("Plumbing", "inactive1@test.com"),
            CreateInactiveWorker("Electrical", "inactive2@test.com")
        };

        // Act
        var result = workers.GetAvailableForEmergency();

        // Assert
        result.Should().BeEmpty("No active workers should result in empty list");
    }

    #endregion

    #region FindBestMatchForRequest Tests

    [Fact]
    public void FindBestMatchForRequest_MultipleEligibleWorkers_ShouldReturnBestScoring()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("General Maintenance", "general@test.com"), // Lower score
            CreateActiveWorker("Plumbing", "plumber@test.com"), // Higher score - exact match
            CreateActiveWorker("Electrical", "electrician@test.com") // Not eligible
        };

        // Act
        var result = workers.FindBestMatchForRequest(request);

        // Assert
        result.Should().NotBeNull("Should find a matching worker");
        result!.ContactInfo.EmailAddress.Should().Be("plumber@test.com", "Should return the best scoring worker");
        result.Specialization.Should().Be("Plumbing", "Should return the worker with exact specialization match");
    }

    [Fact]
    public void FindBestMatchForRequest_NoEligibleWorkers_ShouldReturnNull()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Electrical", "electrician@test.com"), // Wrong specialization
            CreateInactiveWorker("Plumbing", "inactive@test.com") // Inactive
        };

        // Act
        var result = workers.FindBestMatchForRequest(request);

        // Assert
        result.Should().BeNull("No eligible workers should return null");
    }

    #endregion

    #region WithSpecialization Tests

    [Fact]
    public void WithSpecialization_WorkersWithMatchingSpecialization_ShouldReturnThem()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber1@test.com"),
            CreateActiveWorker("Plumbing", "plumber2@test.com"),
            CreateActiveWorker("Electrical", "electrician@test.com"),
            CreateInactiveWorker("Plumbing", "inactive@test.com")
        };

        // Act
        var result = workers.WithSpecialization("Plumbing");

        // Assert
        result.Should().HaveCount(2, "Should return only active workers with Plumbing specialization");
        result.Should().OnlyContain(w => w.IsActive, "All returned workers should be active");
        result.Should().OnlyContain(w => w.HasSpecializedSkills("Plumbing"), "All returned workers should have plumbing skills");
    }

    [Fact]
    public void WithSpecialization_NoMatchingWorkers_ShouldReturnEmpty()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateActiveWorker("Electrical", "electrician@test.com"),
            CreateActiveWorker("HVAC", "hvac@test.com")
        };

        // Act
        var result = workers.WithSpecialization("Plumbing");

        // Assert
        result.Should().BeEmpty("No workers with plumbing specialization should return empty list");
    }

    #endregion

    #region AvailableOnDate Tests

    [Fact]
    public void AvailableOnDate_WorkersAvailableOnDate_ShouldReturnThem()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "available@test.com"),
            CreateInactiveWorker("Electrical", "inactive@test.com") // Should be filtered out
        };

        // Act
        var result = workers.AvailableOnDate(tomorrow);

        // Assert
        result.Should().HaveCount(1, "Should return only active workers available on the date");
        result.Should().OnlyContain(w => w.IsActive, "All returned workers should be active");
        
        // Verify availability individually to avoid expression tree issues
        foreach (var worker in result)
        {
            worker.IsAvailableForWork(tomorrow).Should().BeTrue("Worker should be available on the specified date");
        }
    }

    [Fact]
    public void AvailableOnDate_PastDate_ShouldReturnEmpty()
    {
        // Arrange
        var yesterday = DateTime.Today.AddDays(-1);
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber@test.com")
        };

        // Act
        var result = workers.AvailableOnDate(yesterday);

        // Assert
        result.Should().BeEmpty("Past date should return no available workers");
    }

    #endregion

    #region WithLightWorkload Tests

    [Fact]
    public void WithLightWorkload_WorkersWithLightWorkload_ShouldReturnThem()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "light1@test.com"),
            CreateActiveWorker("Electrical", "light2@test.com"),
            CreateInactiveWorker("HVAC", "inactive@test.com")
        };

        // Act
        var result = workers.WithLightWorkload(2);

        // Assert
        result.Should().HaveCount(2, "Should return only active workers with light workload");
        result.Should().OnlyContain(w => w.IsActive, "All returned workers should be active");
        
        // Verify workload individually to avoid expression tree issues
        foreach (var worker in result)
        {
            worker.GetUpcomingWorkloadCount(DateTime.UtcNow).Should().BeLessOrEqualTo(2, "Worker should have light workload");
        }
    }

    #endregion

    #region GetAssignmentRecommendations Tests

    [Fact]
    public void GetAssignmentRecommendations_EligibleWorkers_ShouldReturnRecommendations()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber1@test.com"),
            CreateActiveWorker("Plumbing", "plumber2@test.com"),
            CreateActiveWorker("General Maintenance", "general@test.com"),
            CreateActiveWorker("Electrical", "electrician@test.com") // Should be filtered out
        };

        // Act
        var result = workers.GetAssignmentRecommendations(request, 3);

        // Assert
        result.Should().HaveCount(3, "Should return up to 3 recommendations");
        result.Should().OnlyContain(r => r.Worker != null, "All recommendations should have a worker");
        result.Should().OnlyContain(r => r.Score > 0, "All recommendations should have a positive score");
        result.Should().OnlyContain(r => !string.IsNullOrEmpty(r.Reasoning), "All recommendations should have reasoning");
        result.Should().OnlyContain(r => r.EstimatedCompletionTime > TimeSpan.Zero, "All recommendations should have estimated time");
    }

    [Fact]
    public void GetAssignmentRecommendations_NoEligibleWorkers_ShouldReturnEmpty()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Electrical", "electrician@test.com"), // Wrong specialization
            CreateInactiveWorker("Plumbing", "inactive@test.com") // Inactive
        };

        // Act
        var result = workers.GetAssignmentRecommendations(request, 3);

        // Assert
        result.Should().BeEmpty("No eligible workers should return empty recommendations");
    }

    #endregion

    #region GroupBySpecialization Tests

    [Fact]
    public void GroupBySpecialization_WorkersWithVariousSpecializations_ShouldGroupCorrectly()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber1@test.com"),
            CreateActiveWorker("Plumbing", "plumber2@test.com"),
            CreateActiveWorker("Electrical", "electrician@test.com"),
            CreateActiveWorker("General Maintenance", "general@test.com"),
            CreateInactiveWorker("HVAC", "inactive@test.com") // Should be filtered out
        };

        // Act
        var result = workers.GroupBySpecialization();

        // Assert
        result.Should().HaveCount(3, "Should group by 3 different specializations (excluding inactive)");
        result.Should().ContainKey("Plumbing").WhoseValue.Should().HaveCount(2);
        result.Should().ContainKey("Electrical").WhoseValue.Should().HaveCount(1);
        result.Should().ContainKey("General Maintenance").WhoseValue.Should().HaveCount(1);
        result.Should().NotContainKey("HVAC", "Inactive workers should be filtered out");
    }

    #endregion

    #region CalculateWorkloadDistribution Tests

    [Fact]
    public void CalculateWorkloadDistribution_MixedWorkloads_ShouldCalculateCorrectly()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "worker1@test.com"),
            CreateActiveWorker("Electrical", "worker2@test.com"),
            CreateActiveWorker("HVAC", "worker3@test.com"),
            CreateInactiveWorker("General", "inactive@test.com") // Should be filtered out
        };

        // Act
        var result = workers.CalculateWorkloadDistribution();

        // Assert
        result.TotalWorkers.Should().Be(3, "Should count only active workers");
        result.AverageWorkload.Should().Be(0, "New workers should have zero workload");
        result.MaxWorkload.Should().Be(0, "New workers should have zero max workload");
        result.MinWorkload.Should().Be(0, "New workers should have zero min workload");
        result.OverloadedWorkers.Should().Be(0, "New workers should not be overloaded");
    }

    [Fact]
    public void CalculateWorkloadDistribution_NoActiveWorkers_ShouldReturnZeros()
    {
        // Arrange
        var workers = new List<Worker>
        {
            CreateInactiveWorker("Plumbing", "inactive1@test.com"),
            CreateInactiveWorker("Electrical", "inactive2@test.com")
        };

        // Act
        var result = workers.CalculateWorkloadDistribution();

        // Assert
        result.TotalWorkers.Should().Be(0, "No active workers should result in zero count");
        result.AverageWorkload.Should().Be(0, "No active workers should result in zero average");
    }

    #endregion

    #region Helper Methods

    private static Worker CreateActiveWorker(string specialization, string email)
    {
        var contactInfo = new PersonContactInfo("John", "Worker", email);
        var worker = new Worker(contactInfo);
        worker.SetSpecialization(specialization);
        return worker;
    }

    private static Worker CreateInactiveWorker(string specialization, string email)
    {
        var contactInfo = new PersonContactInfo("Inactive", "Worker", email);
        var worker = new Worker(contactInfo);
        worker.SetSpecialization(specialization);
        worker.Deactivate("Test deactivation");
        return worker;
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

        var request = tenant.CreateRequest(title, description, "Normal");
        request.SubmitForReview(); // Make request assignable
        return request;
    }

    #endregion
}