using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Entities;

/// <summary>
/// Tests for the new Worker aggregate methods added in Step 2: Push Logic to Aggregates.
/// Verifies that business logic moved from domain services to aggregates works correctly.
/// </summary>
public class WorkerAggregateBusinessLogicTests
{
    #region CalculateScoreForRequest Tests

    [Fact]
    public void CalculateScoreForRequest_ExactSpecializationMatch_ShouldReturnHighScore()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var score = worker.CalculateScoreForRequest(request);

        // Assert
        score.Should().BeGreaterThan(300, "Exact specialization match should get high score (100 base + 200 specialization + 50 availability + workload bonus)");
    }

    [Fact]
    public void CalculateScoreForRequest_GeneralMaintenanceWorker_ShouldReturnMediumScore()
    {
        // Arrange
        var worker = CreateActiveWorker("General Maintenance", "general@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var score = worker.CalculateScoreForRequest(request);

        // Assert
        score.Should().BeGreaterThan(200, "General maintenance worker should get medium score");
        score.Should().BeLessThan(400, "General maintenance should score less than exact match for same conditions");
        // Note: General Maintenance workers can handle Plumbing requests, so they get the specialization bonus
    }

    [Fact]
    public void CalculateScoreForRequest_InactiveWorker_ShouldReturnZero()
    {
        // Arrange
        var worker = CreateInactiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var score = worker.CalculateScoreForRequest(request);

        // Assert
        score.Should().Be(0, "Inactive worker should get zero score");
    }

    [Fact]
    public void CalculateScoreForRequest_EmergencyRequest_ShouldGetBonus()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var emergencyRequest = CreateEmergencyRequest("Emergency plumbing", "Burst pipe");

        // Act
        var score = worker.CalculateScoreForRequest(emergencyRequest);

        // Assert
        score.Should().BeGreaterThan(330, "Emergency request should get bonus points");
    }



    #endregion

    #region CanBeAssignedToRequest Tests

    [Fact]
    public void CanBeAssignedToRequest_ValidWorkerAndRequest_ShouldReturnTrue()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var result = worker.CanBeAssignedToRequest(request);

        // Assert
        result.Should().BeTrue("Active worker with matching specialization should be assignable");
    }

    [Fact]
    public void CanBeAssignedToRequest_InactiveWorker_ShouldReturnFalse()
    {
        // Arrange
        var worker = CreateInactiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var result = worker.CanBeAssignedToRequest(request);

        // Assert
        result.Should().BeFalse("Inactive worker should not be assignable");
    }

    [Fact]
    public void CanBeAssignedToRequest_WrongSpecialization_ShouldReturnFalse()
    {
        // Arrange
        var worker = CreateActiveWorker("Electrical", "electrician@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var result = worker.CanBeAssignedToRequest(request);

        // Assert
        result.Should().BeFalse("Worker without required specialization should not be assignable");
    }

    [Fact]
    public void CanBeAssignedToRequest_RequestNotInAssignableStatus_ShouldReturnFalse()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        
        // Complete the workflow to closed status
        request.ScheduleWork(DateTime.UtcNow.AddHours(1), "worker@test.com", "WO123");
        request.ReportWorkCompleted(true, "Work done");
        request.Close("Completed");

        // Act
        var result = worker.CanBeAssignedToRequest(request);

        // Assert
        result.Should().BeFalse("Request in Closed status should not be assignable");
    }

    #endregion

    #region CalculateRecommendationConfidence Tests

    [Fact]
    public void CalculateRecommendationConfidence_ExactSpecializationMatch_ShouldReturnHighConfidence()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var confidence = worker.CalculateRecommendationConfidence(request);

        // Assert
        confidence.Should().Be(0.90, "Exact specialization match should have high confidence");
    }

    [Fact]
    public void CalculateRecommendationConfidence_EmergencyRequest_ShouldReturnHigherConfidence()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var emergencyRequest = CreateEmergencyRequest("Emergency plumbing", "Burst pipe");

        // Act
        var confidence = worker.CalculateRecommendationConfidence(emergencyRequest);

        // Assert
        confidence.Should().Be(0.95, "Emergency request with exact match should have highest confidence");
    }

    [Fact]
    public void CalculateRecommendationConfidence_InactiveWorker_ShouldReturnZero()
    {
        // Arrange
        var worker = CreateInactiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var confidence = worker.CalculateRecommendationConfidence(request);

        // Assert
        confidence.Should().Be(0.0, "Inactive worker should have zero confidence");
    }

    #endregion

    #region GenerateRecommendationReasoning Tests

    [Fact]
    public void GenerateRecommendationReasoning_ExactSpecialization_ShouldMentionExactMatch()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var reasoning = worker.GenerateRecommendationReasoning(request);

        // Assert
        reasoning.Should().Contain("exact Plumbing specialization", "Should mention exact specialization match");
        reasoning.Should().Contain("Available for immediate assignment", "Should mention availability");
    }

    [Fact]
    public void GenerateRecommendationReasoning_EmergencyRequest_ShouldMentionEmergencyCapability()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var emergencyRequest = CreateEmergencyRequest("Emergency plumbing", "Burst pipe");

        // Act
        var reasoning = worker.GenerateRecommendationReasoning(emergencyRequest);

        // Assert
        reasoning.Should().Contain("emergency requests", "Should mention emergency capability");
    }

    [Fact]
    public void GenerateRecommendationReasoning_InactiveWorker_ShouldReturnInactiveMessage()
    {
        // Arrange
        var worker = CreateInactiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var reasoning = worker.GenerateRecommendationReasoning(request);

        // Assert
        reasoning.Should().Be("Worker is inactive", "Inactive worker should return inactive message");
    }

    #endregion

    #region EstimateCompletionTime Tests

    [Fact]
    public void EstimateCompletionTime_ExactSpecialization_ShouldReturnBaseTime()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var estimatedTime = worker.EstimateCompletionTime(request);

        // Assert
        estimatedTime.Should().Be(TimeSpan.FromHours(2), "Exact specialization should get base time");
    }

    [Fact]
    public void EstimateCompletionTime_EmergencyRequest_ShouldReturnBaseTime()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var emergencyRequest = CreateEmergencyRequest("Emergency plumbing", "Burst pipe");

        // Act
        var estimatedTime = worker.EstimateCompletionTime(emergencyRequest);

        // Assert
        estimatedTime.Should().Be(TimeSpan.FromHours(2), "Emergency request should get priority timing");
    }

    [Fact]
    public void EstimateCompletionTime_NoSpecializationMatch_ShouldReturnLongerTime()
    {
        // Arrange
        var worker = CreateActiveWorker("Electrical", "electrician@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var estimatedTime = worker.EstimateCompletionTime(request);

        // Assert
        estimatedTime.Should().Be(TimeSpan.FromHours(3), "No specialization match should take longer");
    }

    [Fact]
    public void EstimateCompletionTime_InactiveWorker_ShouldReturnZero()
    {
        // Arrange
        var worker = CreateInactiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var estimatedTime = worker.EstimateCompletionTime(request);

        // Assert
        estimatedTime.Should().Be(TimeSpan.Zero, "Inactive worker should return zero time");
    }

    #endregion

    #region ValidateAssignmentToRequest Tests

    [Fact]
    public void ValidateAssignmentToRequest_ValidAssignment_ShouldReturnSuccess()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var scheduledDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = worker.ValidateAssignmentToRequest(request, scheduledDate);

        // Assert
        result.IsValid.Should().BeTrue("Valid assignment should return success");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateAssignmentToRequest_InactiveWorker_ShouldReturnFailure()
    {
        // Arrange
        var worker = CreateInactiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var scheduledDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = worker.ValidateAssignmentToRequest(request, scheduledDate);

        // Assert
        result.IsValid.Should().BeFalse("Inactive worker should return failure");
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public void ValidateAssignmentToRequest_PastDate_ShouldReturnFailure()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var scheduledDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = worker.ValidateAssignmentToRequest(request, scheduledDate);

        // Assert
        result.IsValid.Should().BeFalse("Past date should return failure");
        result.ErrorMessage.Should().Contain("future");
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

    private static TenantRequest CreateEmergencyRequest(string title, string description)
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

        var request = tenant.CreateRequest(title, description, "Emergency");
        request.SubmitForReview(); // Make request assignable
        return request;
    }

    #endregion
}