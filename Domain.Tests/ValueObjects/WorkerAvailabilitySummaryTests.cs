using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for WorkerAvailabilitySummary value object
/// </summary>
public class WorkerAvailabilitySummaryTests
{
    [Fact]
    public void CreateFromWorker_ShouldPopulateAllProperties()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);
        worker.AssignToWork("WO-001", testDate.AddHours(9));

        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(30);

        // Act
        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            startDate,
            endDate,
            DateTime.Today);

        // Assert
        summary.WorkerId.Should().Be(worker.Id);
        summary.WorkerEmail.Should().Be("john.worker@test.com");
        summary.WorkerName.Should().Be("John Worker");
        summary.Specialization.Should().Be(Enums.WorkerSpecialization.GeneralMaintenance);
        summary.IsActive.Should().BeTrue();
        summary.CurrentWorkload.Should().Be(1);
        summary.ActiveAssignmentsCount.Should().Be(1);
        summary.PartiallyBookedDates.Should().Contain(testDate.Date);
    }

    [Fact]
    public void CreateFromWorker_ShouldThrowException_WhenWorkerIsNull()
    {
        // Act
        Action act = () => WorkerAvailabilitySummary.CreateFromWorker(
            null!,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateFromWorker_ShouldIncludeBookedDates()
    {
        // Arrange
        var worker = CreateTestWorker();
        var fullyBookedDate = DateTime.Today.AddDays(5);

        worker.AssignToWork("WO-001", fullyBookedDate.AddHours(9));
        worker.AssignToWork("WO-002", fullyBookedDate.AddHours(14));

        // Act
        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Assert
        summary.BookedDates.Should().Contain(fullyBookedDate.Date);
        summary.PartiallyBookedDates.Should().NotContain(fullyBookedDate.Date);
    }

    [Fact]
    public void CreateFromWorker_ShouldRespectEmergencyOverride()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        worker.AssignToWork("WO-001", testDate.AddHours(9));
        worker.AssignToWork("WO-002", testDate.AddHours(14));

        // Act
        var summaryNormal = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today,
            false);

        var summaryEmergency = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today,
            true);

        // Assert
        summaryNormal.BookedDates.Should().Contain(testDate.Date, "normal mode: 2 assignments = booked");
        summaryEmergency.BookedDates.Should().NotContain(testDate.Date, "emergency mode: allows 3rd assignment");
    }

    [Fact]
    public void IsAvailableOnDate_ShouldReturnFalse_WhenDateIsFullyBooked()
    {
        // Arrange
        var worker = CreateTestWorker();
        var bookedDate = DateTime.Today.AddDays(5);

        worker.AssignToWork("WO-001", bookedDate.AddHours(9));
        worker.AssignToWork("WO-002", bookedDate.AddHours(14));

        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act
        var isAvailable = summary.IsAvailableOnDate(bookedDate);

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailableOnDate_ShouldReturnTrue_WhenDateIsPartiallyBooked_AndPartialAllowed()
    {
        // Arrange
        var worker = CreateTestWorker();
        var partialDate = DateTime.Today.AddDays(5);

        worker.AssignToWork("WO-001", partialDate.AddHours(9));

        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act
        var isAvailable = summary.IsAvailableOnDate(partialDate, true);

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailableOnDate_ShouldReturnFalse_WhenDateIsPartiallyBooked_AndPartialNotAllowed()
    {
// Arrange
        var worker = CreateTestWorker();
        var partialDate = DateTime.Today.AddDays(5);

        worker.AssignToWork("WO-001", partialDate.AddHours(9));

        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act
        var isAvailable = summary.IsAvailableOnDate(partialDate, false);

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailableOnDate_ShouldReturnTrue_WhenDateIsFullyAvailable()
    {
        // Arrange
        var worker = CreateTestWorker();
        var availableDate = DateTime.Today.AddDays(5);

        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act
        var isAvailable = summary.IsAvailableOnDate(availableDate);

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public void GetAvailabilityStatusForDate_ShouldReturnCorrectStatus()
    {
        // Arrange
        var worker = CreateTestWorker();
        var fullyBookedDate = DateTime.Today.AddDays(5);
        var partialDate = DateTime.Today.AddDays(6);
        var availableDate = DateTime.Today.AddDays(7);

        worker.AssignToWork("WO-001", fullyBookedDate.AddHours(9));
        worker.AssignToWork("WO-002", fullyBookedDate.AddHours(14));
        worker.AssignToWork("WO-003", partialDate.AddHours(9));

        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act
        var statusFullyBooked = summary.GetAvailabilityStatusForDate(fullyBookedDate);
        var statusPartial = summary.GetAvailabilityStatusForDate(partialDate);
        var statusAvailable = summary.GetAvailabilityStatusForDate(availableDate);

        // Assert
        statusFullyBooked.Should().Contain("Fully Booked");
        statusFullyBooked.Should().Contain("2/2");

        statusPartial.Should().Contain("Limited Availability");
        statusPartial.Should().Contain("1/2");

        statusAvailable.Should().Contain("Fully Available");
        statusAvailable.Should().Contain("0/2");
    }

    [Fact]
    public void GetAvailabilityIndicator_ShouldReturnCorrectSymbols()
    {
        // Arrange
        var worker = CreateTestWorker();
        var fullyBookedDate = DateTime.Today.AddDays(5);
        var partialDate = DateTime.Today.AddDays(6);
        var availableDate = DateTime.Today.AddDays(7);

        worker.AssignToWork("WO-001", fullyBookedDate.AddHours(9));
        worker.AssignToWork("WO-002", fullyBookedDate.AddHours(14));
        worker.AssignToWork("WO-003", partialDate.AddHours(9));

        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

// Act
        var indicatorFullyBooked = summary.GetAvailabilityIndicator(fullyBookedDate);
        var indicatorPartial = summary.GetAvailabilityIndicator(partialDate);
        var indicatorAvailable = summary.GetAvailabilityIndicator(availableDate);

        // Assert - Check that different symbols are returned for different availability states
        indicatorFullyBooked.Should().NotBeNullOrEmpty("fully booked should have indicator");
        indicatorPartial.Should().NotBeNullOrEmpty("partial should have indicator");
        indicatorAvailable.Should().NotBeNullOrEmpty("available should have indicator");

        // Different states should have different indicators
        indicatorFullyBooked.Should().NotBe(indicatorAvailable, "fully booked and available should differ");
        indicatorPartial.Should().NotBe(indicatorAvailable, "partial and available should differ");
    }

    [Fact]
    public void Equality_ShouldBeBasedOnWorkerIdAndBookingData()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);
        worker.AssignToWork("WO-001", testDate.AddHours(9));

        var summary1 = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        var summary2 = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act & Assert
        summary1.Should().Be(summary2, "same worker and same booking data should be equal");
    }

    [Fact]
    public void ToString_ShouldReturnReadableFormat()
    {
        // Arrange
        var worker = CreateTestWorker();
        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act
        var result = summary.ToString();

        // Assert
        result.Should().Contain("John Worker");
        result.Should().Contain("General Maintenance");
        result.Should().Contain("Next available");
    }

    [Fact]
    public void ToString_ShouldHandleNoAvailability()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Book starting from tomorrow for 70 days
        for (var i = 1; i <= 70; i++)
        {
            var date = DateTime.UtcNow.AddDays(i);
            worker.AssignToWork($"WO-{i}-1", date);
            worker.AssignToWork($"WO-{i}-2", date.AddHours(5));
        }

        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act
        var result = summary.ToString();

        // Assert
        // Today is available so should show "Next available: [today's date]"
        result.Should().Contain("Worker", "string should contain worker information");
        result.Should().Contain("General Maintenance", "string should contain specialization");
    }

    [Fact]
    public void BookedDates_ShouldBeReadOnly()
    {
        // Arrange
        var worker = CreateTestWorker();
        var summary = WorkerAvailabilitySummary.CreateFromWorker(
            worker,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            DateTime.Today);

        // Act & Assert
        summary.BookedDates.Should().BeAssignableTo<IReadOnlyList<DateTime>>();
        summary.PartiallyBookedDates.Should().BeAssignableTo<IReadOnlyList<DateTime>>();
    }

    private static Worker CreateTestWorker()
    {
        var contactInfo = new PersonContactInfo("John", "Worker", "john.worker@test.com", "555-1234");
        var worker = new Worker(contactInfo);
        worker.SetSpecialization(Enums.WorkerSpecialization.GeneralMaintenance);
        return worker;
    }
}