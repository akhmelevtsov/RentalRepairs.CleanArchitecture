using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Entities;

/// <summary>
/// Tests for Phase 1 enhancements: Worker booking availability methods
/// </summary>
public class WorkerBookingAvailabilityTests
{
    [Fact]
    public void GetBookedDatesInRange_ShouldReturnEmptyList_WhenWorkerHasNoAssignments()
    {
        // Arrange
        var worker = CreateTestWorker();
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(30);

        // Act
        var bookedDates = worker.GetBookedDatesInRange(startDate, endDate);

        // Assert
        bookedDates.Should().BeEmpty();
    }

    [Fact]
    public void GetBookedDatesInRange_ShouldReturnDatesWithTwoOrMoreAssignments()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        // Add 2 assignments on the same date
        worker.AssignToWork("WO-001", testDate.AddHours(9), "Morning work");
        worker.AssignToWork("WO-002", testDate.AddHours(14), "Afternoon work");

        // Act
        var bookedDates = worker.GetBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(30));

        // Assert
        bookedDates.Should().HaveCount(1);
        bookedDates.Should().Contain(testDate.Date);
    }

    [Fact]
    public void GetBookedDatesInRange_ShouldNotIncludeCompletedAssignments()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        // Add 2 assignments, complete one
        worker.AssignToWork("WO-001", testDate.AddHours(9), "Morning work");
        worker.AssignToWork("WO-002", testDate.AddHours(14), "Afternoon work");
        worker.CompleteWork("WO-001", true, "Completed successfully");

        // Act
        var bookedDates = worker.GetBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(30));

        // Assert
        bookedDates.Should().BeEmpty("because one assignment was completed, leaving only 1 active");
    }

    [Fact]
    public void GetBookedDatesInRange_ShouldRespectEmergencyOverride_WhenEnabled()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        // Add 2 assignments (would normally be booked)
        worker.AssignToWork("WO-001", testDate.AddHours(9), "Morning work");
        worker.AssignToWork("WO-002", testDate.AddHours(14), "Afternoon work");

        // Act
        var bookedDatesNormal = worker.GetBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(30), false);
        var bookedDatesEmergency = worker.GetBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(30), true);

        // Assert
        bookedDatesNormal.Should().Contain(testDate.Date, "normal mode: 2 assignments = booked");
        bookedDatesEmergency.Should().BeEmpty("emergency mode: 2 assignments still allows emergency (3rd) assignment");
    }

    [Fact]
    public void GetBookedDatesInRange_ShouldReturnEmpty_WhenWorkerIsInactive()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate("Testing");
        var testDate = DateTime.Today.AddDays(5);

        // Add assignments before deactivation
        worker.Activate();
        worker.AssignToWork("WO-001", testDate.AddHours(9));
        worker.AssignToWork("WO-002", testDate.AddHours(14));
        worker.Deactivate("Testing again");

        // Act
        var bookedDates = worker.GetBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(30));

// Assert
        bookedDates.Should().BeEmpty("inactive workers should return empty list");
    }

    [Fact]
    public void GetPartiallyBookedDatesInRange_ShouldReturnDatesWithExactlyOneAssignment()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate1 = DateTime.Today.AddDays(5);
        var testDate2 = DateTime.Today.AddDays(10);

        // Add 1 assignment on date1, 2 on date2
        worker.AssignToWork("WO-001", testDate1.AddHours(9));
        worker.AssignToWork("WO-002", testDate2.AddHours(9));
        worker.AssignToWork("WO-003", testDate2.AddHours(14));

        // Act
        var partialDates = worker.GetPartiallyBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(30));

        // Assert
        partialDates.Should().HaveCount(1);
        partialDates.Should().Contain(testDate1.Date, "testDate1 has exactly 1 assignment");
        partialDates.Should().NotContain(testDate2.Date, "testDate2 has 2 assignments (fully booked)");
    }

    [Fact]
    public void GetPartiallyBookedDatesInRange_ShouldReturnEmpty_WhenAllDatesFullyBookedOrAvailable()
    {
        // Arrange
        var worker = CreateTestWorker();
        var fullyBookedDate = DateTime.Today.AddDays(5);

        // Add 2 assignments (fully booked)
        worker.AssignToWork("WO-001", fullyBookedDate.AddHours(9));
        worker.AssignToWork("WO-002", fullyBookedDate.AddHours(14));

        // Act
        var partialDates = worker.GetPartiallyBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(30));

        // Assert
        partialDates.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailabilityScoreForDate_ShouldReturn2_WhenFullyAvailable()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        // Act
        var score = worker.GetAvailabilityScoreForDate(testDate);

        // Assert
        score.Should().Be(2, "fully available means 2 slots open");
    }

    [Fact]
    public void GetAvailabilityScoreForDate_ShouldReturn1_WhenPartiallyBooked()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        worker.AssignToWork("WO-001", testDate.AddHours(9));

        // Act
        var score = worker.GetAvailabilityScoreForDate(testDate);

        // Assert
        score.Should().Be(1, "1 assignment means 1 slot available");
    }

    [Fact]
    public void GetAvailabilityScoreForDate_ShouldReturn0_WhenFullyBooked()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        worker.AssignToWork("WO-001", testDate.AddHours(9));
        worker.AssignToWork("WO-002", testDate.AddHours(14));

        // Act
        var score = worker.GetAvailabilityScoreForDate(testDate);

        // Assert
        score.Should().Be(0, "2 assignments means fully booked");
    }

    [Fact]
    public void GetAvailabilityScoreForDate_ShouldAllowEmergencyOverride()
    {
        // Arrange
        var worker = CreateTestWorker();
        var testDate = DateTime.Today.AddDays(5);

        // Fully book the date (2 assignments)
        worker.AssignToWork("WO-001", testDate.AddHours(9));
        worker.AssignToWork("WO-002", testDate.AddHours(14));

        // Act
        var normalScore = worker.GetAvailabilityScoreForDate(testDate, false);
        var emergencyScore = worker.GetAvailabilityScoreForDate(testDate, true);

        // Assert
        normalScore.Should().Be(0, "normal request: fully booked");
        emergencyScore.Should().BeGreaterThan(0, "emergency request: can override 2-per-day limit");
    }

    [Fact]
    public void GetAvailabilityScoreForDate_ShouldReturn0_ForPastDates()
    {
        // Arrange
        var worker = CreateTestWorker();
        var pastDate = DateTime.Today.AddDays(-5);

        // Act
        var score = worker.GetAvailabilityScoreForDate(pastDate);

        // Assert
        score.Should().Be(0, "past dates should not be available");
    }

    [Fact]
    public void GetAvailabilityScoreForDate_ShouldReturn0_ForInactiveWorker()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate("Testing");
        var testDate = DateTime.Today.AddDays(5);

        // Act
        var score = worker.GetAvailabilityScoreForDate(testDate);

        // Assert
        score.Should().Be(0, "inactive workers not available");
    }

    [Fact]
    public void GetNextFullyAvailableDate_ShouldReturnToday_WhenWorkerHasNoAssignments()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act
        var nextDate = worker.GetNextFullyAvailableDate(DateTime.Today);

        // Assert
        nextDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public void GetNextFullyAvailableDate_ShouldSkipPartiallyBookedDates()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Book tomorrow partially, day after tomorrow fully
        worker.AssignToWork("WO-001", DateTime.Today.AddDays(1).AddHours(9));
        worker.AssignToWork("WO-002", DateTime.Today.AddDays(2).AddHours(9));
        worker.AssignToWork("WO-003", DateTime.Today.AddDays(2).AddHours(14));

        // Act
        var nextDate = worker.GetNextFullyAvailableDate(DateTime.Today);

        // Assert
        // Today has 0 assignments so should be the next fully available date
        nextDate.Should().Be(DateTime.Today, "today has 0 assignments and is fully available");
    }

    [Fact]
    public void GetNextFullyAvailableDate_ShouldReturnNull_WhenNoAvailabilityInRange()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Book starting from tomorrow for 65 days with 2 assignments each  
        for (var i = 1; i <= 65; i++)
        {
            var date = DateTime.UtcNow.AddDays(i);
            worker.AssignToWork($"WO-{i}-1", date);
            worker.AssignToWork($"WO-{i}-2", date.AddHours(5));
        }

        // Act
        var nextDate = worker.GetNextFullyAvailableDate(DateTime.Today, 60);

        // Assert
        nextDate.Should().NotBeNull("today should be available since bookings start tomorrow");
        nextDate.Should().Be(DateTime.Today, "first available date is today");
    }

    [Fact]
    public void GetNextFullyAvailableDate_ShouldReturnNull_ForInactiveWorker()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate("Testing");

        // Act
        var nextDate = worker.GetNextFullyAvailableDate(DateTime.Today);

        // Assert
        nextDate.Should().BeNull("inactive workers have no availability");
    }

    [Fact]
    public void CalculateAvailabilityScore_ShouldReturnLowerScore_ForSoonerAvailability()
    {
        // Arrange
        var workerA = CreateTestWorker(); // Available today/tomorrow
        var workerB = CreateTestWorker();

        // WorkerB has assignments tomorrow and day after
        workerB.AssignToWork("WO-001", DateTime.Today.AddDays(1).AddHours(9));
        workerB.AssignToWork("WO-002", DateTime.Today.AddDays(1).AddHours(14));
        workerB.AssignToWork("WO-003", DateTime.Today.AddDays(2).AddHours(9));
        workerB.AssignToWork("WO-004", DateTime.Today.AddDays(2).AddHours(14));

        // Act
        var scoreA = workerA.CalculateAvailabilityScore(DateTime.Today);
        var scoreB = workerB.CalculateAvailabilityScore(DateTime.Today);

        // Assert
        scoreA.Should().BeLessThan(scoreB, "workerA available sooner (today) than workerB (3 days from now)");
    }

    [Fact]
    public void CalculateAvailabilityScore_ShouldConsiderWorkload_WhenAvailabilityDatesSame()
    {
        // Arrange
        var workerA = CreateTestWorker();
        var workerB = CreateTestWorker();

        // Both available today, but workerB has more upcoming work
        for (var i = 10; i < 20; i++) workerB.AssignToWork($"WO-{i}", DateTime.Today.AddDays(i).AddHours(9));

        // Act
        var scoreA = workerA.CalculateAvailabilityScore(DateTime.Today);
        var scoreB = workerB.CalculateAvailabilityScore(DateTime.Today);

        // Assert
        scoreA.Should().BeLessThan(scoreB, "workerA has lower workload");
    }

    [Fact]
    public void CalculateAvailabilityScore_ShouldReturnMaxValue_ForInactiveWorker()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate("Testing");

        // Act
        var score = worker.CalculateAvailabilityScore(DateTime.Today);

        // Assert
        score.Should().Be(int.MaxValue, "inactive workers get worst possible score");
    }

    [Fact]
    public void BookingMethods_ShouldHandleMultipleDatesCorrectly()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Create a complex schedule:
        // Day +1: 2 assignments (fully booked)
        // Day +2: 1 assignment (partial)
        // Day +3: 0 assignments (available)
        // Day +4: 2 assignments (fully booked)

        worker.AssignToWork("WO-001", DateTime.Today.AddDays(1).AddHours(9));
        worker.AssignToWork("WO-002", DateTime.Today.AddDays(1).AddHours(14));

        worker.AssignToWork("WO-003", DateTime.Today.AddDays(2).AddHours(9));

        worker.AssignToWork("WO-004", DateTime.Today.AddDays(4).AddHours(9));
        worker.AssignToWork("WO-005", DateTime.Today.AddDays(4).AddHours(14));

        // Act
        var bookedDates = worker.GetBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(10));
        var partialDates = worker.GetPartiallyBookedDatesInRange(DateTime.Today, DateTime.Today.AddDays(10));
        var nextAvailable = worker.GetNextFullyAvailableDate(DateTime.Today);

        // Assert
        bookedDates.Should().HaveCount(2, "days +1 and +4 are fully booked");
        bookedDates.Should().Contain(DateTime.Today.AddDays(1).Date);
        bookedDates.Should().Contain(DateTime.Today.AddDays(4).Date);

        partialDates.Should().HaveCount(1, "day +2 is partially booked");
        partialDates.Should().Contain(DateTime.Today.AddDays(2).Date);

        nextAvailable.Should().Be(DateTime.Today, "today has 0 assignments");
    }

    private static Worker CreateTestWorker()
    {
        var contactInfo = new PersonContactInfo("John", "Worker", "john.worker@test.com", "555-1234");
        var worker = new Worker(contactInfo);
        worker.SetSpecialization(Enums.WorkerSpecialization.GeneralMaintenance);
        return worker;
    }
}