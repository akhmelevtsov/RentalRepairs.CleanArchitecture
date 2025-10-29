using RentalRepairs.Application.ReadModels;
using RentalRepairs.Domain.Enums;
using RentalRepairs.WebUI.Helpers;

namespace RentalRepairs.WebUI.Tests.Unit.Helpers;

/// <summary>
/// ? CLEAN: Unit Tests for TenantRequestUIHelper
/// Tests the centralized UI helper methods independently
/// Validates consistent UI behavior across the application
/// </summary>
public class TenantRequestUIHelperTests
{
    #region Status Badge Tests

    [Theory]
    [InlineData(TenantRequestStatus.Draft, "badge bg-secondary")]
    [InlineData(TenantRequestStatus.Submitted, "badge bg-primary")]
    [InlineData(TenantRequestStatus.Scheduled, "badge bg-warning text-dark")]
    [InlineData(TenantRequestStatus.Done, "badge bg-success")]
    [InlineData(TenantRequestStatus.Failed, "badge bg-danger")]
    [InlineData(TenantRequestStatus.Declined, "badge bg-dark")]
    [InlineData(TenantRequestStatus.Closed, "badge bg-info")]
    public void GetStatusBadgeClass_WithStatus_ShouldReturnCorrectBadgeClass(TenantRequestStatus status, string expectedClass)
    {
        // Act
        var result = TenantRequestUIHelper.GetStatusBadgeClass(status);

        // Assert
        result.Should().Be(expectedClass);
    }

    [Theory]
    [InlineData("Draft", "badge bg-secondary")]
    [InlineData("Submitted", "badge bg-primary")]
    [InlineData("Scheduled", "badge bg-warning text-dark")]
    [InlineData("Done", "badge bg-success")]
    [InlineData("Failed", "badge bg-danger")]
    [InlineData("Declined", "badge bg-dark")]
    [InlineData("Closed", "badge bg-info")]
    [InlineData("Unknown", "badge bg-secondary")]
    public void GetStatusBadgeClass_WithDisplayName_ShouldReturnCorrectBadgeClass(string statusDisplayName, string expectedClass)
    {
        // Act
        var result = TenantRequestUIHelper.GetStatusBadgeClass(statusDisplayName);

        // Assert
        result.Should().Be(expectedClass);
    }

    #endregion

    #region Urgency Tests

    [Theory]
    [InlineData(true, "text-danger fw-bold")]
    [InlineData(false, "")]
    public void GetUrgencyClass_ShouldReturnCorrectClass(bool isEmergency, string expectedClass)
    {
        // Act
        var result = TenantRequestUIHelper.GetUrgencyClass(isEmergency);

        // Assert
        result.Should().Be(expectedClass);
    }

    [Theory]
    [InlineData("low", "badge bg-light text-dark")]
    [InlineData("normal", "badge bg-secondary")]
    [InlineData("high", "badge bg-warning text-dark")]
    [InlineData("critical", "badge bg-danger")]
    [InlineData("emergency", "badge bg-danger")]
    [InlineData("unknown", "badge bg-secondary")]
    [InlineData(null, "badge bg-secondary")]
    public void GetUrgencyBadgeClass_ShouldReturnCorrectBadgeClass(string urgencyLevel, string expectedClass)
    {
        // Act
        var result = TenantRequestUIHelper.GetUrgencyBadgeClass(urgencyLevel);

        // Assert
        result.Should().Be(expectedClass);
    }

    [Theory]
    [InlineData("low", "fas fa-circle text-success")]
    [InlineData("normal", "fas fa-circle text-secondary")]
    [InlineData("high", "fas fa-exclamation-circle text-warning")]
    [InlineData("critical", "fas fa-exclamation-triangle text-danger")]
    [InlineData("emergency", "fas fa-exclamation-triangle text-danger")]
    [InlineData("unknown", "fas fa-circle text-secondary")]
    [InlineData(null, "fas fa-circle text-secondary")]
    public void GetUrgencyIcon_ShouldReturnCorrectIcon(string urgencyLevel, string expectedIcon)
    {
        // Act
        var result = TenantRequestUIHelper.GetUrgencyIcon(urgencyLevel);

        // Assert
        result.Should().Be(expectedIcon);
    }

    #endregion

    #region Progress Tests

    [Theory]
    [InlineData(TenantRequestStatus.Draft, 10)]
    [InlineData(TenantRequestStatus.Submitted, 30)]
    [InlineData(TenantRequestStatus.Scheduled, 60)]
    [InlineData(TenantRequestStatus.Done, 90)]
    [InlineData(TenantRequestStatus.Closed, 100)]
    [InlineData(TenantRequestStatus.Declined, 0)]
    [InlineData(TenantRequestStatus.Failed, 75)]
    public void GetProgressPercentage_ShouldReturnCorrectPercentage(TenantRequestStatus status, int expectedPercentage)
    {
        // Act
        var result = TenantRequestUIHelper.GetProgressPercentage(status);

        // Assert
        result.Should().Be(expectedPercentage);
    }

    [Theory]
    [InlineData(TenantRequestStatus.Done, "progress-bar bg-success")]
    [InlineData(TenantRequestStatus.Closed, "progress-bar bg-success")]
    [InlineData(TenantRequestStatus.Failed, "progress-bar bg-danger")]
    [InlineData(TenantRequestStatus.Declined, "progress-bar bg-dark")]
    [InlineData(TenantRequestStatus.Draft, "progress-bar bg-primary")]
    [InlineData(TenantRequestStatus.Submitted, "progress-bar bg-primary")]
    [InlineData(TenantRequestStatus.Scheduled, "progress-bar bg-primary")]
    public void GetProgressBarClass_ShouldReturnCorrectClass(TenantRequestStatus status, string expectedClass)
    {
        // Act
        var result = TenantRequestUIHelper.GetProgressBarClass(status);

        // Assert
        result.Should().Be(expectedClass);
    }

    #endregion

    #region Conditional Display Tests

    [Fact]
    public void ShouldShowEmergencyAlert_WithEmergencyRequest_ShouldReturnTrue()
    {
        // Arrange - Create request with High urgency (which makes IsEmergency true)
        var request = new TenantRequestDetailsReadModel
        {
            UrgencyLevel = "High",
            Status = Domain.Enums.TenantRequestStatus.Submitted
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowEmergencyAlert(request);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldShowEmergencyAlert_WithEmergencyUrgencyRequest_ShouldReturnTrue()
    {
        // Arrange - Create request with Emergency urgency
        var request = new TenantRequestDetailsReadModel
        {
            UrgencyLevel = "Emergency",
            Status = Domain.Enums.TenantRequestStatus.Submitted
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowEmergencyAlert(request);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldShowEmergencyAlert_WithNonEmergencyRequest_ShouldReturnFalse()
    {
        // Arrange - Create request with Normal urgency (which makes IsEmergency false)
        var request = new TenantRequestDetailsReadModel
        {
            UrgencyLevel = "Normal",
            Status = Domain.Enums.TenantRequestStatus.Submitted
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowEmergencyAlert(request);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldShowEmergencyAlert_WithCompletedEmergencyRequest_ShouldReturnFalse()
    {
        // Arrange - Even emergency requests don't show alert when completed
        var request = new TenantRequestDetailsReadModel
        {
            UrgencyLevel = "Emergency",
            Status = Domain.Enums.TenantRequestStatus.Done
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowEmergencyAlert(request);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldShowOverdueWarning_WithOverdueRequest_ShouldReturnTrue()
    {
        // Arrange - Create request with overdue scheduled date
        var request = new TenantRequestDetailsReadModel
        {
            ScheduledDate = DateTime.Now.AddHours(-2), // Overdue
            Status = Domain.Enums.TenantRequestStatus.Scheduled
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowOverdueWarning(request);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldShowOverdueWarning_WithFutureScheduledRequest_ShouldReturnFalse()
    {
        // Arrange - Create request with future scheduled date
        var request = new TenantRequestDetailsReadModel
        {
            ScheduledDate = DateTime.Now.AddHours(2), // Future
            Status = Domain.Enums.TenantRequestStatus.Scheduled
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowOverdueWarning(request);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldShowOverdueWarning_WithCompletedRequest_ShouldReturnFalse()
    {
        // Arrange - Completed requests don't show overdue warning
        var request = new TenantRequestDetailsReadModel
        {
            ScheduledDate = DateTime.Now.AddHours(-2), // Would be overdue
            Status = Domain.Enums.TenantRequestStatus.Done // But completed
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowOverdueWarning(request);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanEditRequest_WithDraftStatus_ShouldReturnTrue()
    {
        // Act
        var result = TenantRequestUIHelper.CanEditRequest(Domain.Enums.TenantRequestStatus.Draft);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanEditRequest_WithNonDraftStatus_ShouldReturnFalse()
    {
        // Act
        var result = TenantRequestUIHelper.CanEditRequest(Domain.Enums.TenantRequestStatus.Submitted);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldShowWorkAssignment_WithAssignedWorker_ShouldReturnTrue()
    {
        // Arrange
        var request = new TenantRequestDetailsReadModel
        {
            AssignedWorkerEmail = "worker@test.com"
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowWorkAssignment(request);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldShowWorkAssignment_WithoutAssignedWorker_ShouldReturnFalse()
    {
        // Arrange
        var request = new TenantRequestDetailsReadModel
        {
            AssignedWorkerEmail = null
        };

        // Act
        var result = TenantRequestUIHelper.ShouldShowWorkAssignment(request);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void FormatDate_WithValidDate_ShouldReturnFormattedString()
    {
        // Arrange
        var date = new DateTime(2023, 12, 25, 14, 30, 0);

        // Act
        var result = TenantRequestUIHelper.FormatDate(date);

        // Assert
        result.Should().Be("Dec 25, 2023 2:30 PM");
    }

    [Fact]
    public void FormatDate_WithNullDate_ShouldReturnNotSet()
    {
        // Act
        var result = TenantRequestUIHelper.FormatDate(null);

        // Assert
        result.Should().Be("Not set");
    }

    [Fact]
    public void GetStatusDescription_ShouldReturnCorrectDescription()
    {
        // Act
        var result = TenantRequestUIHelper.GetStatusDescription(TenantRequestStatus.Draft);

        // Assert
        result.Should().Be("Request is being prepared");
    }

    #endregion

    #region Action Button Tests

    [Theory]
    [InlineData(TenantRequestStatus.Draft, new[] { "Submit", "Edit", "Delete" })]
    [InlineData(TenantRequestStatus.Submitted, new[] { "Assign Worker", "Decline" })]
    [InlineData(TenantRequestStatus.Scheduled, new[] { "Complete", "Reschedule", "Report Issue" })]
    [InlineData(TenantRequestStatus.Done, new[] { "Close", "Reopen" })]
    [InlineData(TenantRequestStatus.Failed, new[] { "Reschedule", "Close" })]
    public void GetQuickActions_ShouldReturnCorrectActions(TenantRequestStatus status, string[] expectedActions)
    {
        // Act
        var result = TenantRequestUIHelper.GetQuickActions(status);

        // Assert
        result.Should().BeEquivalentTo(expectedActions);
    }

    [Theory]
    [InlineData("Submit", "btn btn-primary")]
    [InlineData("Edit", "btn btn-outline-primary")]
    [InlineData("Delete", "btn btn-outline-danger")]
    [InlineData("Complete", "btn btn-success")]
    [InlineData("Unknown", "btn btn-secondary")]
    public void GetActionButtonClass_ShouldReturnCorrectClass(string actionName, string expectedClass)
    {
        // Act
        var result = TenantRequestUIHelper.GetActionButtonClass(actionName);

        // Assert
        result.Should().Be(expectedClass);
    }

    #endregion
}