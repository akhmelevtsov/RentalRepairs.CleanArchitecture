using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Domain.Events.TenantRequests;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Events;

public class DomainEventTests
{
    [Fact]
    public void TenantRequest_Submit_ShouldRaiseTenantRequestSubmittedEvent()
    {
        // Arrange
        var request = CreateTestTenantRequest();

        // Act
        request.Submit();

        // Assert
        request.DomainEvents.Should().HaveCount(2); // Created + Submitted events
        request.DomainEvents.Should().ContainSingle(e => e is TenantRequestSubmittedEvent);
    }

    [Fact]
    public void TenantRequest_Schedule_ShouldRaiseTenantRequestScheduledEvent()
    {
        // Arrange
        var request = CreateTestTenantRequest();
        request.Submit();
        var scheduledDate = DateTime.UtcNow.AddDays(1);

        // Act
        request.Schedule(scheduledDate, "worker@test.com", "WO-123");

        // Assert
        request.DomainEvents.Should().Contain(e => e is TenantRequestScheduledEvent);
        var scheduledEvent = request.DomainEvents.OfType<TenantRequestScheduledEvent>().First();
        scheduledEvent.TenantRequest.Should().Be(request);
        scheduledEvent.ScheduleInfo.Should().NotBeNull();
    }

    [Fact]
    public void TenantRequest_ReportWorkCompleted_ShouldRaiseTenantRequestCompletedEvent()
    {
        // Arrange
        var request = CreateScheduledTenantRequest();

        // Act
        request.ReportWorkCompleted(true, "Work completed successfully");

        // Assert
        request.DomainEvents.Should().Contain(e => e is TenantRequestCompletedEvent);
        var completedEvent = request.DomainEvents.OfType<TenantRequestCompletedEvent>().First();
        completedEvent.TenantRequest.Should().Be(request);
        completedEvent.CompletionNotes.Should().Be("Work completed successfully");
    }

    [Fact]
    public void TenantRequest_Close_ShouldRaiseTenantRequestClosedEvent()
    {
        // Arrange
        var request = CreateCompletedTenantRequest();

        // Act
        request.Close("Request resolved");

        // Assert
        request.DomainEvents.Should().Contain(e => e is TenantRequestClosedEvent);
        var closedEvent = request.DomainEvents.OfType<TenantRequestClosedEvent>().First();
        closedEvent.TenantRequest.Should().Be(request);
        closedEvent.ClosureNotes.Should().Be("Request resolved");
    }

    [Fact]
    public void TenantRequest_Decline_ShouldRaiseTenantRequestDeclinedEvent()
    {
        // Arrange
        var request = CreateTestTenantRequest();
        request.Submit();

        // Act
        request.Decline("Not a maintenance issue");

        // Assert
        request.DomainEvents.Should().Contain(e => e is TenantRequestDeclinedEvent);
        var declinedEvent = request.DomainEvents.OfType<TenantRequestDeclinedEvent>().First();
        declinedEvent.TenantRequest.Should().Be(request);
        declinedEvent.Reason.Should().Be("Not a maintenance issue");
    }

    [Fact]
    public void Property_Constructor_ShouldRaisePropertyRegisteredEvent()
    {
        // Arrange & Act
        var property = CreateTestProperty();

        // Assert
        property.DomainEvents.Should().Contain(e => e is PropertyRegisteredEvent);
        var registeredEvent = property.DomainEvents.OfType<PropertyRegisteredEvent>().First();
        registeredEvent.Property.Should().Be(property);
    }

    [Fact]
    public void Property_UpdateSuperintendent_ShouldRaiseSuperintendentChangedEvent()
    {
        // Arrange
        var property = CreateTestProperty();
        var newSuperintendent = new PersonContactInfo("New", "Super", "new@test.com");

        // Act
        property.UpdateSuperintendent(newSuperintendent);

        // Assert
        property.DomainEvents.Should().Contain(e => e is SuperintendentChangedEvent);
        var changedEvent = property.DomainEvents.OfType<SuperintendentChangedEvent>().First();
        changedEvent.Property.Should().Be(property);
        changedEvent.NewSuperintendent.Should().Be(newSuperintendent);
    }

    [Fact]
    public void Property_RegisterTenant_ShouldRaiseTenantRegisteredEvent()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Doe", "jane@test.com");

        // Act
        var tenant = property.RegisterTenant(tenantContact, "101");

        // Assert
        property.DomainEvents.Should().Contain(e => e is TenantRegisteredEvent);
        var registeredEvent = property.DomainEvents.OfType<TenantRegisteredEvent>().First();
        registeredEvent.Property.Should().Be(property);
        registeredEvent.Tenant.Should().Be(tenant);
    }

    [Fact]
    public void Worker_Constructor_ShouldRaiseWorkerRegisteredEvent()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("Bob", "Builder", "bob@test.com");

        // Act
        var worker = new Worker(contactInfo);

        // Assert
        worker.DomainEvents.Should().Contain(e => e is WorkerRegisteredEvent);
        var registeredEvent = worker.DomainEvents.OfType<WorkerRegisteredEvent>().First();
        registeredEvent.Worker.Should().Be(worker);
    }

    [Fact]
    public void Worker_Activate_ShouldRaiseWorkerActivatedEvent()
    {
        // Arrange
        var worker = CreateTestWorker();
        worker.Deactivate(); // First deactivate

        // Act
        worker.Activate();

        // Assert
        worker.DomainEvents.Should().Contain(e => e is WorkerActivatedEvent);
        var activatedEvent = worker.DomainEvents.OfType<WorkerActivatedEvent>().First();
        activatedEvent.Worker.Should().Be(worker);
    }

    [Fact]
    public void Worker_Deactivate_ShouldRaiseWorkerDeactivatedEvent()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act
        worker.Deactivate("Testing deactivation");

        // Assert
        worker.DomainEvents.Should().Contain(e => e is WorkerDeactivatedEvent);
        var deactivatedEvent = worker.DomainEvents.OfType<WorkerDeactivatedEvent>().First();
        deactivatedEvent.Worker.Should().Be(worker);
        deactivatedEvent.Reason.Should().Be("Testing deactivation");
    }

    [Fact]
    public void Tenant_UpdateContactInfo_ShouldRaiseTenantContactInfoChangedEvent()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenant = property.RegisterTenant(
            new PersonContactInfo("John", "Doe", "john@test.com"), "101");
        var newContactInfo = new PersonContactInfo("John", "Smith", "john.smith@test.com");

        // Act
        tenant.UpdateContactInfo(newContactInfo);

        // Assert
        tenant.DomainEvents.Should().Contain(e => e is TenantContactInfoChangedEvent);
        var changedEvent = tenant.DomainEvents.OfType<TenantContactInfoChangedEvent>().First();
        changedEvent.Tenant.Should().Be(tenant);
        changedEvent.NewContactInfo.Should().Be(newContactInfo);
    }

    [Fact]
    public void DomainEvents_MultipleEventTypes_ShouldAllBeRaised()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Jane", "Doe", "jane@test.com");
        var tenant = property.RegisterTenant(tenantContact, "101");
        var request = tenant.CreateRequest("Test Issue", "Description", "Normal");

        // Act - Perform multiple operations that raise events
        request.Submit();

        // Assert - Check that all expected event types are present
        var createdEvent = request.DomainEvents.OfType<TenantRequestCreatedEvent>().FirstOrDefault();
        var submittedEvent = request.DomainEvents.OfType<TenantRequestSubmittedEvent>().FirstOrDefault();

        createdEvent.Should().NotBeNull("TenantRequestCreatedEvent should be raised");
        submittedEvent.Should().NotBeNull("TenantRequestSubmittedEvent should be raised");
    }

    [Fact]
    public void DomainEvents_EntityCreation_ShouldHaveCorrectEventData()
    {
        // Arrange & Act
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Test", "Tenant", "test@test.com");
        var tenant = property.RegisterTenant(tenantContact, "102");
        var request = tenant.CreateRequest("Plumbing Issue", "Leaky faucet", "High");

        // Assert - Verify event data integrity
        var createdEvent = request.DomainEvents.OfType<TenantRequestCreatedEvent>().First();
        createdEvent.TenantRequest.Title.Should().Be("Plumbing Issue");
        createdEvent.TenantRequest.UrgencyLevel.Should().Be("High");
    }

    [Fact]
    public void DomainEvents_ComplexWorkflow_ShouldRaiseAllExpectedEvents()
    {
        // Arrange
        var property = CreateTestProperty();
        var tenantContact = new PersonContactInfo("Test", "User", "test@user.com");
        var tenant = property.RegisterTenant(tenantContact, "103");
        var request = tenant.CreateRequest("Emergency Repair", "Water leak", "Emergency");
        var worker = CreateTestWorker();

        // Act - Simulate full workflow
        request.Submit();
        request.Schedule(DateTime.UtcNow.AddDays(1), "worker@test.com", "WO-456");
        request.ReportWorkCompleted(true, "Repair completed");
        request.Close("Issue resolved");

        // Assert - Verify all events in workflow were raised
        request.DomainEvents.Should().HaveCount(5); // Created, Submitted, Scheduled, Completed, Closed
        request.DomainEvents.OfType<TenantRequestCreatedEvent>().Should().HaveCount(1);
        request.DomainEvents.OfType<TenantRequestSubmittedEvent>().Should().HaveCount(1);
        request.DomainEvents.OfType<TenantRequestScheduledEvent>().Should().HaveCount(1);
        request.DomainEvents.OfType<TenantRequestCompletedEvent>().Should().HaveCount(1);
        request.DomainEvents.OfType<TenantRequestClosedEvent>().Should().HaveCount(1);
    }

    #region Helper Methods

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

    private static TenantRequest CreateScheduledTenantRequest()
    {
        var request = CreateTestTenantRequest();
        request.Submit();
        request.Schedule(DateTime.UtcNow.AddDays(1), "worker@test.com", "WO-123");
        return request;
    }

    private static TenantRequest CreateCompletedTenantRequest()
    {
        var request = CreateScheduledTenantRequest();
        request.ReportWorkCompleted(true, "Work completed");
        return request;
    }

    private static Property CreateTestProperty()
    {
        return new Property(
            "Test Property",
            "TP001",
            new PropertyAddress("123", "Test St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Superintendent", "john@super.com"),
            new List<string> { "101", "102", "103" },
            "noreply@test.com");
    }

    private static Worker CreateTestWorker()
    {
        var contactInfo = new PersonContactInfo("Bob", "Builder", "bob@test.com");
        return new Worker(contactInfo);
    }

    #endregion
}