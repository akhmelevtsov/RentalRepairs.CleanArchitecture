using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Services;

public class WorkerAssignmentPolicyServiceTests
{
    private readonly WorkerAssignmentPolicyService _service;

    public WorkerAssignmentPolicyServiceTests()
    {
        _service = new WorkerAssignmentPolicyService();
    }

    #region CanAssignWorkerToRequest Tests

    [Fact]
    public void CanAssignWorkerToRequest_ValidWorkerAndRequest_ShouldReturnTrue()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumber", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var result = _service.CanAssignWorkerToRequest(worker, request);

        // Assert
        result.Should().BeTrue("Active worker with matching specialization should be assignable");
    }

    [Fact]
    public void CanAssignWorkerToRequest_InactiveWorker_ShouldReturnFalse()
    {
        // Arrange
        var worker = CreateInactiveWorker("Plumber", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var result = _service.CanAssignWorkerToRequest(worker, request);

        // Assert
        result.Should().BeFalse("Inactive worker should not be assignable");
    }

    [Fact]
    public void CanAssignWorkerToRequest_WrongSpecialization_ShouldReturnFalse()
    {
        // Arrange
        var worker = CreateActiveWorker("Electrical", "electrician@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");

        // Act
        var result = _service.CanAssignWorkerToRequest(worker, request);

        // Assert
        result.Should().BeFalse("Worker without required specialization should not be assignable");
    }

    [Fact]
    public void CanAssignWorkerToRequest_RequestNotInAssignableStatus_ShouldReturnFalse()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumber", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        
        // Complete the workflow to closed status (non-assignable)
        request.ScheduleWork(DateTime.UtcNow.AddHours(1), "worker@test.com", "WO123");
        request.ReportWorkCompleted(true, "Work done");
        request.Close("Completed"); // Change to non-assignable status

        // Act
        var result = _service.CanAssignWorkerToRequest(worker, request);

        // Assert
        result.Should().BeFalse("Request in Closed status should not be assignable");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("plumber@test.com", null)]
    [InlineData(null, "Plumbing issue")]
    public void CanAssignWorkerToRequest_NullParameters_ShouldReturnFalse(string? workerEmail, string? requestTitle)
    {
        // Arrange
        var worker = workerEmail != null ? CreateActiveWorker("Plumber", workerEmail) : null;
        var request = requestTitle != null ? CreateTestRequest(requestTitle, "Description") : null;

        // Act
        var result = _service.CanAssignWorkerToRequest(worker, request);

        // Assert
        result.Should().BeFalse("Null worker or request should return false");
    }

    #endregion

    #region FilterEligibleWorkers Tests

    [Fact]
    public void FilterEligibleWorkers_MultipleWorkers_ShouldReturnOnlyEligible()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber1@test.com"),
            CreateActiveWorker("Electrical", "electrician@test.com"), // Wrong specialization
            CreateInactiveWorker("Plumbing", "plumber2@test.com"), // Inactive
            CreateActiveWorker("General Maintenance", "general@test.com") // General maintenance can handle anything
        };

        // Act
        var result = _service.FilterEligibleWorkers(request, workers);

        // Assert
        result.Should().HaveCount(2, "Should return only eligible workers");
        result.Should().Contain(w => w.ContactInfo.EmailAddress == "plumber1@test.com");
        result.Should().Contain(w => w.ContactInfo.EmailAddress == "general@test.com");
        result.Should().NotContain(w => w.ContactInfo.EmailAddress == "electrician@test.com");
        result.Should().NotContain(w => w.ContactInfo.EmailAddress == "plumber2@test.com");
    }

    [Fact]
    public void FilterEligibleWorkers_EmptyWorkerList_ShouldReturnEmpty()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>();

        // Act
        var result = _service.FilterEligibleWorkers(request, workers);

        // Assert
        result.Should().BeEmpty("Empty worker list should return empty result");
    }

    [Fact]
    public void FilterEligibleWorkers_NullParameters_ShouldReturnEmpty()
    {
        // Arrange & Act
        var result1 = _service.FilterEligibleWorkers(null, new List<Worker>());
        var result2 = _service.FilterEligibleWorkers(CreateTestRequest("Test", "Test"), null);

        // Assert
        result1.Should().BeEmpty("Null request should return empty result");
        result2.Should().BeEmpty("Null worker list should return empty result");
    }

    #endregion

    #region FindBestWorker Tests

    [Fact]
    public void FindBestWorker_MultipleEligibleWorkers_ShouldReturnBestMatch()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("General Maintenance", "general@test.com"), // Lower score
            CreateActiveWorker("Plumbing", "plumber@test.com") // Higher score - exact match
        };

        // Act
        var result = _service.FindBestWorker(request, workers);

        // Assert
        result.Should().NotBeNull("Should return a worker");
        result!.ContactInfo.EmailAddress.Should().Be("plumber@test.com", "Should return worker with exact specialization match");
        
        // ? STEP 2: Verify that the service now uses Worker aggregate methods
        var plumberScore = result.CalculateScoreForRequest(request);
        var generalScore = workers.First(w => w.ContactInfo.EmailAddress == "general@test.com").CalculateScoreForRequest(request);
        plumberScore.Should().BeGreaterThan(generalScore, "Plumber should score higher than general maintenance worker");
    }

    [Fact]
    public void FindBestWorker_EmptyWorkerList_ShouldReturnNull()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>();

        // Act
        var result = _service.FindBestWorker(request, workers);

        // Assert
        result.Should().BeNull("Empty worker list should return null");
    }

    #endregion

    #region CanAutoAssignRequest Tests

    [Fact]
    public void CanAutoAssignRequest_EmergencyRequestWithEligibleWorkers_ShouldReturnTrue()
    {
        // Arrange
        var request = CreateEmergencyRequest("Emergency plumbing", "Burst pipe");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber@test.com")
        };

        // Act
        var result = _service.CanAutoAssignRequest(request, workers);

        // Assert
        result.Should().BeTrue("Emergency request with eligible workers should be auto-assignable");
    }

    [Fact]
    public void CanAutoAssignRequest_NonEmergencyRequest_ShouldReturnFalse()
    {
        // Arrange
        var request = CreateTestRequest("Normal plumbing", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber@test.com")
        };

        // Act
        var result = _service.CanAutoAssignRequest(request, workers);

        // Assert
        result.Should().BeFalse("Non-emergency request should not be auto-assignable");
    }

    [Fact]
    public void CanAutoAssignRequest_EmergencyRequestWithoutEligibleWorkers_ShouldReturnFalse()
    {
        // Arrange
        var request = CreateEmergencyRequest("Emergency plumbing", "Burst pipe");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Electrical", "electrician@test.com") // Wrong specialization
        };

        // Act
        var result = _service.CanAutoAssignRequest(request, workers);

        // Assert
        result.Should().BeFalse("Emergency request without eligible workers should not be auto-assignable");
    }

    #endregion

    #region ValidateBasicAssignment Tests

    [Fact]
    public void ValidateBasicAssignment_ValidAssignment_ShouldReturnSuccess()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var scheduledDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = _service.ValidateBasicAssignment(worker, request, scheduledDate);

        // Assert
        result.IsValid.Should().BeTrue("Valid assignment should return success");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateBasicAssignment_PastScheduledDate_ShouldReturnFailure()
    {
        // Arrange
        var worker = CreateActiveWorker("Plumbing", "plumber@test.com");
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var scheduledDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = _service.ValidateBasicAssignment(worker, request, scheduledDate);

        // Assert
        result.IsValid.Should().BeFalse("Past scheduled date should return failure");
        result.ErrorMessage.Should().Contain("must be in the future");
    }

    [Fact]
    public void ValidateBasicAssignment_NullWorker_ShouldReturnFailure()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var scheduledDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = _service.ValidateBasicAssignment(null, request, scheduledDate);

        // Assert
        result.IsValid.Should().BeFalse("Null worker should return failure");
        result.ErrorMessage.Should().Contain("Worker not found");
    }

    #endregion

    #region GetAssignmentRecommendations Tests

    [Fact]
    public void GetAssignmentRecommendations_MultipleWorkers_ShouldReturnTop3Recommendations()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber1@test.com"),
            CreateActiveWorker("Plumbing", "plumber2@test.com"),
            CreateActiveWorker("General Maintenance", "general1@test.com"),
            CreateActiveWorker("General Maintenance", "general2@test.com"),
            CreateActiveWorker("General Maintenance", "general3@test.com")
        };

        // Act
        var result = _service.GetAssignmentRecommendations(request, workers);

        // Assert
        result.Should().HaveCountLessOrEqualTo(3, "Should return at most 3 recommendations");
        result.Should().OnlyContain(r => r.Worker != null, "All recommendations should have a worker");
        result.Should().OnlyContain(r => r.Score > 0, "All recommendations should have a positive score");
        result.Should().OnlyContain(r => !string.IsNullOrEmpty(r.Reasoning), "All recommendations should have reasoning");
    }

    [Fact]
    public void GetAssignmentRecommendations_NoEligibleWorkers_ShouldReturnEmpty()
    {
        // Arrange
        var request = CreateTestRequest("Plumbing issue", "Leaky faucet");
        var workers = new List<Worker>
        {
            CreateActiveWorker("Electrical", "electrician@test.com") // Wrong specialization
        };

        // Act
        var result = _service.GetAssignmentRecommendations(request, workers);

        // Assert
        result.Should().BeEmpty("No eligible workers should return empty recommendations");
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