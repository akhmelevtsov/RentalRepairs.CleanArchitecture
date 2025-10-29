using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Events.TenantRequests;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Entities;

public class TenantRequestTests
{
    [Fact]
    public void CreateNew_ShouldCreateValidTenantRequest()
    {
        // Arrange & Act
        var request = CreateTestTenantRequest();

        // Assert
        request.Should().NotBeNull();
        request.Code.Should().Be("TP001-101-0001");
        request.Title.Should().Be("Test Request");
        request.Description.Should().Be("Test Description");
        request.UrgencyLevel.Should().Be("Normal");
        request.Status.Should().Be(TenantRequestStatus.Draft);
        request.DomainEvents.Should().HaveCount(1);
        request.DomainEvents.First().Should().BeOfType<TenantRequestCreatedEvent>();
    }

    [Theory]
    [InlineData("", "Description", "Normal")]    // Empty title
    [InlineData("Title", "Description", "")]     // Empty urgency
    [InlineData("Title", "Description", "Invalid")] // Invalid urgency
    public void CreateNew_ShouldThrowException_WithInvalidInput(string title, string description, string urgencyLevel)
    {
        // Act & Assert
        Action act = () => TenantRequest.CreateNew(
            "TP001-101-0001", title, description, urgencyLevel,
            Guid.NewGuid(), Guid.NewGuid(),
            "John Doe", "john@test.com", "101",
            "Test Property", "555-1234",
            "Super Intendent", "super@test.com");

        act.Should().Throw<TenantRequestDomainException>();
    }

    [Fact]
    public void Submit_ShouldUpdateStatusToSubmitted()
    {
        // Arrange
        var request = CreateTestTenantRequest();

        // Act
        request.Submit();

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Submitted);
        request.DomainEvents.Should().Contain(e => e is TenantRequestSubmittedEvent);
    }

    [Fact]
    public void Submit_ShouldThrowException_WhenNotInDraftStatus()
    {
        // Arrange
        var request = CreateTestTenantRequest();
        request.Submit(); // Already submitted

        // Act & Assert
        Action act = () => request.Submit();
        act.Should().Throw<TenantRequestDomainException>()
           .WithMessage("*Draft status*");
    }

    [Fact]
    public void Schedule_ShouldUpdateStatusToScheduled()
    {
        // Arrange
        var request = CreateTestTenantRequest();
        request.Submit();
        
        var futureDate = DateTime.UtcNow.AddDays(1);
        const string workerEmail = "worker@test.com";
        const string workOrderNumber = "WO-123";

        // Act
        request.Schedule(futureDate, workerEmail, workOrderNumber);

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Scheduled);
        request.ScheduledDate.Should().Be(futureDate);
        request.AssignedWorkerEmail.Should().Be(workerEmail);
        request.WorkOrderNumber.Should().Be(workOrderNumber);
        request.DomainEvents.Should().Contain(e => e is TenantRequestScheduledEvent);
    }

    [Theory]
    [InlineData("2023-01-01")] // Past date
    public void Schedule_ShouldThrowException_WithInvalidScheduleDate(string dateString)
    {
        // Arrange
        var request = CreateTestTenantRequest();
        request.Submit();
        
        var invalidDate = DateTime.Parse(dateString);

        // Act & Assert
        Action act = () => request.Schedule(invalidDate, "worker@test.com", "WO-123");
        act.Should().Throw<TenantRequestDomainException>()
           .WithMessage("*must be in the future*");
    }

    [Theory]
    [InlineData("", "WO-123")] // Empty worker email
    [InlineData("worker@test.com", "")] // Empty work order
    public void Schedule_ShouldThrowException_WithInvalidParameters(string workerEmail, string workOrderNumber)
    {
        // Arrange
        var request = CreateTestTenantRequest();
        request.Submit();
        
        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act & Assert
        Action act = () => request.Schedule(futureDate, workerEmail, workOrderNumber);
        act.Should().Throw<TenantRequestDomainException>();
    }

    [Fact]
    public void ReportWorkCompleted_ShouldUpdateToCompletedStatus_WhenSuccessful()
    {
        // Arrange
        var request = CreateScheduledRequest();

        // Act
        request.ReportWorkCompleted(true, "Work completed successfully");

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Done);
        request.WorkCompletedSuccessfully.Should().BeTrue();
        request.CompletionNotes.Should().Be("Work completed successfully");
        request.CompletedDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        request.DomainEvents.Should().Contain(e => e is TenantRequestCompletedEvent);
    }

    [Fact]
    public void ReportWorkCompleted_ShouldUpdateToFailedStatus_WhenUnsuccessful()
    {
        // Arrange
        var request = CreateScheduledRequest();

        // Act
        request.ReportWorkCompleted(false, "Unable to complete work");

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Failed);
        request.WorkCompletedSuccessfully.Should().BeFalse();
        request.CompletionNotes.Should().Be("Unable to complete work");
    }

    [Fact]
    public void Close_ShouldUpdateToClosedStatus_FromDoneStatus()
    {
        // Arrange
        var request = CreateCompletedRequest();

        // Act
        request.Close("Request fully resolved");

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Closed);
        request.ClosureNotes.Should().Be("Request fully resolved");
        request.DomainEvents.Should().Contain(e => e is TenantRequestClosedEvent);
    }

    [Fact]
    public void Decline_ShouldUpdateToDeclinedStatus_FromSubmittedStatus()
    {
        // Arrange
        var request = CreateTestTenantRequest();
        request.Submit();

        // Act
        request.Decline("Not a valid maintenance request");

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Declined);
        request.DomainEvents.Should().Contain(e => e is TenantRequestDeclinedEvent);
    }

    [Fact]
    public void IsStatusTransitionValid_ShouldValidateCorrectly()
    {
        // Arrange
        var request = CreateTestTenantRequest(); // Draft status

        // Assert Draft transitions
        request.IsStatusTransitionValid(TenantRequestStatus.Submitted).Should().BeTrue();
        request.IsStatusTransitionValid(TenantRequestStatus.Scheduled).Should().BeFalse();

        // Test Submitted transitions
        request.Submit();
        request.IsStatusTransitionValid(TenantRequestStatus.Scheduled).Should().BeTrue();
        request.IsStatusTransitionValid(TenantRequestStatus.Declined).Should().BeTrue();
        request.IsStatusTransitionValid(TenantRequestStatus.Done).Should().BeFalse();
    }

    [Fact]
    public void RequiresImmediateAttention_ShouldReturnTrue_ForEmergency()
    {
        // Arrange
        var request = CreateEmergencyRequest();

        // Act & Assert
        request.RequiresImmediateAttention().Should().BeTrue();
    }

    [Fact]
    public void GetExpectedResolutionHours_ShouldReturnCorrectHours()
    {
        // Arrange & Act & Assert
        var emergency = CreateRequestWithUrgency("Emergency");
        emergency.GetExpectedResolutionHours().Should().Be(2);

        var critical = CreateRequestWithUrgency("Critical");
        critical.GetExpectedResolutionHours().Should().Be(4);

        var high = CreateRequestWithUrgency("High");
        high.GetExpectedResolutionHours().Should().Be(24);

        var normal = CreateRequestWithUrgency("Normal");
        normal.GetExpectedResolutionHours().Should().Be(72);

        var low = CreateRequestWithUrgency("Low");
        low.GetExpectedResolutionHours().Should().Be(168);
    }

    [Fact]
    public void UpdateTenantInformation_ShouldUpdateSuccessfully()
    {
        // Arrange
        var request = CreateTestTenantRequest();

        // Act
        request.UpdateTenantInformation("New Name", "new@email.com", "102", "New Property");

        // Assert
        request.TenantFullName.Should().Be("New Name");
        request.TenantEmail.Should().Be("new@email.com");
        request.TenantUnit.Should().Be("102");
        request.PropertyName.Should().Be("New Property");
        request.DomainEvents.Should().Contain(e => e is TenantRequestTenantInfoUpdatedEvent);
    }

    [Fact]
    public void UpdateTenantInformation_ShouldThrowException_WhenRequestCompleted()
    {
        // Arrange
        var request = CreateCompletedRequest();

        // Act & Assert
        Action act = () => request.UpdateTenantInformation("New Name", "new@email.com", "102", "New Property");
        act.Should().Throw<TenantRequestDomainException>()
           .WithMessage("*cannot update tenant information*");
    }

    private static TenantRequest CreateTestTenantRequest()
    {
        return TenantRequest.CreateNew(
            "TP001-101-0001",
            "Test Request",
            "Test Description",
            "Normal",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john@test.com",
            "101",
            "Test Property",
            "555-1234",
            "Super Intendent",
            "super@test.com");
    }

    private static TenantRequest CreateEmergencyRequest()
    {
        return TenantRequest.CreateNew(
            "TP001-101-0002",
            "Emergency Request",
            "Emergency Description",
            "Emergency",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jane Doe",
            "jane@test.com",
            "102",
            "Test Property",
            "555-1234",
            "Super Intendent",
            "super@test.com");
    }

    private static TenantRequest CreateRequestWithUrgency(string urgencyLevel)
    {
        return TenantRequest.CreateNew(
            "TP001-101-0003",
            $"{urgencyLevel} Request",
            $"{urgencyLevel} Description",
            urgencyLevel,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Tenant",
            "test@test.com",
            "103",
            "Test Property",
            "555-1234",
            "Super Intendent",
            "super@test.com");
    }

    private static TenantRequest CreateScheduledRequest()
    {
        var request = CreateTestTenantRequest();
        request.Submit();
        request.Schedule(DateTime.UtcNow.AddDays(1), "worker@test.com", "WO-123");
        return request;
    }

    private static TenantRequest CreateCompletedRequest()
    {
        var request = CreateScheduledRequest();
        request.ReportWorkCompleted(true, "Work completed");
        return request;
    }
}