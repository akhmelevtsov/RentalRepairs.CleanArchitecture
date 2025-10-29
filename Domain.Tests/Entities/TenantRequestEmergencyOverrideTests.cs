using Xunit;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Events.TenantRequests;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Entities;

/// <summary>
/// Tests for TenantRequest emergency override functionality
/// Validates the FailDueToEmergencyOverride method and related business rules
/// </summary>
public class TenantRequestEmergencyOverrideTests
{
    [Fact]
    public void FailDueToEmergencyOverride_ValidScheduledRequest_ShouldTransitionToFailed()
    {
        // Arrange
        var request = CreateScheduledRequest();
        var reason = "Emergency water leak requires immediate attention";

        // Act
        request.FailDueToEmergencyOverride(reason);

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Failed);
        request.CompletionNotes.Should().Contain("Work cancelled due to emergency override");
        request.CompletionNotes.Should().Contain(reason);
        request.ClosureNotes.Should().Contain("Emergency override cancelled assignment");
        request.AssignedWorkerEmail.Should().BeNull();
        request.WorkOrderNumber.Should().BeNull();
        request.ScheduledDate.Should().BeNull();
        request.AssignedWorkerName.Should().BeNull();
    }

    [Fact]
    public void FailDueToEmergencyOverride_PreservesAuditTrail_ShouldCaptureOriginalAssignment()
    {
        // Arrange
        var request = CreateScheduledRequest();
        var originalWorker = request.AssignedWorkerEmail;
        var originalWorkOrder = request.WorkOrderNumber;
        var originalScheduledDate = request.ScheduledDate;
        var reason = "Emergency override";

        // Act
        request.FailDueToEmergencyOverride(reason);

        // Assert
        request.ClosureNotes.Should().Contain(originalWorker);
        request.ClosureNotes.Should().Contain(originalWorkOrder);
        request.ClosureNotes.Should().Contain(originalScheduledDate?.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void FailDueToEmergencyOverride_GeneratesDomainEvent_ShouldPublishCompletedEvent()
    {
        // Arrange
        var request = CreateScheduledRequest();
        var initialEventCount = request.DomainEvents.Count;

        // Act
        request.FailDueToEmergencyOverride("Emergency override");

        // Assert
        request.DomainEvents.Should().HaveCount(initialEventCount + 1);
        request.DomainEvents.Last().Should().BeOfType<TenantRequestCompletedEvent>();
    }

    [Fact]
    public void FailDueToEmergencyOverride_FromSubmittedStatus_ShouldThrowException()
    {
        // Arrange
        var request = CreateSubmittedRequest();

        // Act & Assert
        var action = () => request.FailDueToEmergencyOverride("Emergency override");
        action.Should().Throw<TenantRequestDomainException>()
            .WithMessage("*can only be failed from Scheduled status*");
    }

    [Fact]
    public void FailDueToEmergencyOverride_FromDraftStatus_ShouldThrowException()
    {
        // Arrange
        var request = CreateDraftRequest();

        // Act & Assert
        var action = () => request.FailDueToEmergencyOverride("Emergency override");
        action.Should().Throw<TenantRequestDomainException>()
            .WithMessage("*can only be failed from Scheduled status*");
    }

    [Fact]
    public void FailDueToEmergencyOverride_FromDoneStatus_ShouldThrowException()
    {
        // Arrange
        var request = CreateCompletedRequest();

        // Act & Assert
        var action = () => request.FailDueToEmergencyOverride("Emergency override");
        action.Should().Throw<TenantRequestDomainException>()
            .WithMessage("*can only be failed from Scheduled status*");
    }

    [Fact]
    public void FailDueToEmergencyOverride_WithoutWorkerAssignment_ShouldThrowException()
    {
        // Arrange
        var request = CreateRequestWithoutWorkerAssignment();

        // Act & Assert
        var action = () => request.FailDueToEmergencyOverride("Emergency override");
        action.Should().Throw<TenantRequestDomainException>()
            .WithMessage("*no worker assignment found*");
    }

    [Fact]
    public void FailDueToEmergencyOverride_WithEmptyReason_ShouldStillWork()
    {
        // Arrange
        var request = CreateScheduledRequest();

        // Act
        request.FailDueToEmergencyOverride("");

        // Assert
        request.Status.Should().Be(TenantRequestStatus.Failed);
        request.CompletionNotes.Should().Contain("Work cancelled due to emergency override");
    }

    [Fact]
    public void FailDueToEmergencyOverride_AllowsStatusTransitionToScheduled_ShouldFollowExistingPattern()
    {
        // Arrange
        var request = CreateScheduledRequest();
        request.FailDueToEmergencyOverride("Emergency override");

        // Act & Assert - Should be able to reschedule from Failed status
        request.IsStatusTransitionValid(TenantRequestStatus.Scheduled).Should().BeTrue();
    }

    [Theory]
    [InlineData("Network outage requiring immediate IT support")]
    [InlineData("Water main break - emergency plumbing needed")]
    [InlineData("Power failure - electrical emergency")]
    [InlineData("Gas leak detected - emergency HVAC required")]
    public void FailDueToEmergencyOverride_WithVariousReasons_ShouldCaptureReasonInNotes(string reason)
    {
        // Arrange
        var request = CreateScheduledRequest();

        // Act
        request.FailDueToEmergencyOverride(reason);

        // Assert
        request.CompletionNotes.Should().Contain(reason);
        request.Status.Should().Be(TenantRequestStatus.Failed);
    }

    #region Helper Methods

    private TenantRequest CreateScheduledRequest()
    {
        var request = TenantRequest.CreateNew(
            "TEST-001",
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
            "Jane Super",
            "jane@super.com");

        // Move to submitted then scheduled
        request.Submit();
        request.Schedule(DateTime.Now.AddDays(1), "worker@test.com", "WO-001", "Test Worker");

        return request;
    }

    private TenantRequest CreateSubmittedRequest()
    {
        var request = TenantRequest.CreateNew(
            "TEST-002",
            "Test Request",
            "Test Description",
            "Normal",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john@test.com",
            "102",
            "Test Property",
            "555-1234",
            "Jane Super",
            "jane@super.com");

        request.Submit();
        return request;
    }

    private TenantRequest CreateDraftRequest()
    {
        return TenantRequest.CreateNew(
            "TEST-003",
            "Test Request",
            "Test Description",
            "Normal",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john@test.com",
            "103",
            "Test Property",
            "555-1234",
            "Jane Super",
            "jane@super.com");
    }

    private TenantRequest CreateCompletedRequest()
    {
        var request = CreateScheduledRequest();
        request.ReportWorkCompleted(true, "Work completed successfully");
        return request;
    }

    private TenantRequest CreateRequestWithoutWorkerAssignment()
    {
        var request = TenantRequest.CreateNew(
            "TEST-004",
            "Test Request",
            "Test Description",
            "Normal",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john@test.com",
            "104",
            "Test Property",
            "555-1234",
            "Jane Super",
            "jane@super.com");

        request.Submit();
        
        // Manually set status to Scheduled without proper worker assignment (edge case)
        // This simulates a data corruption scenario
        request.GetType().GetProperty("Status")?.SetValue(request, TenantRequestStatus.Scheduled);
        
        return request;
    }

    #endregion
}