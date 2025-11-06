using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Domain.Enums;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Entities;

public class WorkerTests
{
    [Fact]
    public void Worker_ShouldBeCreated_WithValidContactInfo()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("Bob", "Builder", "bob@workers.com", "555-9999");

        // Act
        var worker = new Worker(contactInfo);

        // Assert
        worker.Should().NotBeNull();
        worker.ContactInfo.Should().Be(contactInfo);
        worker.IsActive.Should().BeTrue();
        worker.Specialization.Should().Be(WorkerSpecialization.GeneralMaintenance); // Default enum value
        worker.Notes.Should().BeNull();
        worker.DomainEvents.Should().HaveCount(1);
        worker.DomainEvents.First().Should().BeOfType<WorkerRegisteredEvent>();
    }

    [Fact]
    public void SetSpecialization_ShouldUpdateSpecialization()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act
        worker.SetSpecialization(WorkerSpecialization.Plumbing);

        // Assert
        worker.Specialization.Should().Be(WorkerSpecialization.Plumbing);
    }

    [Fact]
    public void AddNotes_ShouldAddNotesToWorker()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act
        worker.AddNotes("Experienced with HVAC systems");
        worker.AddNotes("Available weekends");

        // Assert
        worker.Notes.Should().NotBeNull();
        worker.Notes.Should().Contain("Experienced with HVAC systems");
        worker.Notes.Should().Contain("Available weekends");
    }

    [Fact]
    public void AddNotes_ShouldIgnoreEmptyNotes()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act
        worker.AddNotes("");
        worker.AddNotes("   ");
        worker.AddNotes("Valid note");

        // Assert
        worker.Notes.Should().NotBeNull();
        worker.Notes.Should().Contain("Valid note");
        // Check that empty notes are ignored by verifying only valid content exists
        worker.Notes!.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            .All(note => !string.IsNullOrWhiteSpace(note.Trim())).Should().BeTrue("empty notes should be ignored");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act
        worker.Deactivate();

        // Assert
        worker.IsActive.Should().BeFalse();
        worker.DomainEvents.Should().Contain(e => e is WorkerDeactivatedEvent);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate(); // First deactivate

        // Act
        worker.Activate();

        // Assert
        worker.IsActive.Should().BeTrue();
        worker.DomainEvents.Should().Contain(e => e is WorkerActivatedEvent);
    }

    [Fact]
    public void CanBeAssignedToRequest_ShouldReturnTrue_WhenActive()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act & Assert
        worker.CanBeAssignedToRequest().Should().BeTrue();
    }

    [Fact]
    public void CanBeAssignedToRequest_ShouldReturnFalse_WhenInactive()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate();

        // Act & Assert
        worker.CanBeAssignedToRequest().Should().BeFalse();
    }

    [Fact]
    public void IsAvailableForWork_ShouldReturnFalse_WhenInactive()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate();

        // Act & Assert
        worker.IsAvailableForWork(DateTime.Today.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void IsAvailableForWork_ShouldReturnFalse_WhenDateInPast()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act & Assert
        worker.IsAvailableForWork(DateTime.Today.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void IsAvailableForWork_ShouldReturnTrue_WhenActiveAndDateInFuture()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act & Assert
        worker.IsAvailableForWork(DateTime.Today.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void AssignToWork_ShouldCreateWorkAssignment()
    {
        // Arrange
        var worker = CreateTestWorker();
        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act
        worker.AssignToWork("WO-12345", futureDate, "Plumbing repair");

        // Assert
        worker.Assignments.Should().HaveCount(1);
        var assignment = worker.Assignments.First();
        assignment.WorkOrderNumber.Should().Be("WO-12345");
        assignment.ScheduledDate.Should().Be(futureDate);
        assignment.Notes.Should().Be("Plumbing repair");
        worker.DomainEvents.Should().Contain(e => e is WorkerAssignedEvent);
    }

    [Fact]
    public void AssignToWork_ShouldThrowException_WithEmptyWorkOrderNumber()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act & Assert
        var act = () => worker.AssignToWork("", DateTime.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignToWork_ShouldThrowException_WithPastDate()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act & Assert - Domain logic in Worker.AssignToWork should still enforce business rules
        var act = () => worker.AssignToWork("WO-12345", DateTime.UtcNow.AddDays(-1));
        act.Should().Throw<ArgumentException>()
            .WithMessage("Scheduled date must be today or in the future*");
    }

    [Fact]
    public void AssignToWork_ShouldThrowException_WhenInactive()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate();

        // Act & Assert
        var act = () => worker.AssignToWork("WO-12345", DateTime.UtcNow.AddDays(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CompleteWork_ShouldMarkAssignmentComplete()
    {
        // Arrange
        var worker = CreateTestWorker();
        var futureDate = DateTime.UtcNow.AddDays(1);
        worker.AssignToWork("WO-12345", futureDate);

        // Act
        worker.CompleteWork("WO-12345", true, "Work completed successfully");

        // Assert
        var assignment = worker.Assignments.First(a => a.WorkOrderNumber == "WO-12345");
        assignment.IsCompleted.Should().BeTrue();
        assignment.CompletedSuccessfully.Should().BeTrue();
        assignment.CompletionNotes.Should().Be("Work completed successfully");
        worker.DomainEvents.Should().Contain(e => e is WorkCompletedEvent);
    }

    [Fact]
    public void CompleteWork_ShouldThrowException_WhenWorkOrderNotFound()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act & Assert
        var act = () => worker.CompleteWork("NONEXISTENT", true);
        act.Should().Throw<InvalidOperationException>();
    }

    private static Worker CreateTestWorker()
    {
        var contactInfo = new PersonContactInfo("Bob", "Builder", "bob@workers.com", "555-9999");
        return new Worker(contactInfo);
    }
}