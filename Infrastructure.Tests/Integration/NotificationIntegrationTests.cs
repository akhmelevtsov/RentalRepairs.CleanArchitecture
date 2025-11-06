using Microsoft.Extensions.Logging;
using RentalRepairs.Application.EventHandlers.Notifications;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Infrastructure.Tests.TestHelpers;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Infrastructure.Tests.Integration;

public class NotificationIntegrationTests
{
    [Fact]
    public async Task TenantRequestNotificationHandler_Should_Send_Emails_Successfully()
    {
        // Arrange
        var emailLogger = new MockLogger<TestableEmailService>();
        var handlerLogger = new MockLogger<TenantRequestNotificationHandler>();
        var emailService = new TestableEmailService(emailLogger);
        var handler = new TenantRequestNotificationHandler(emailService, handlerLogger);

        // Create a test tenant request
        var tenantRequest = CreateTestTenantRequest();
        var submittedEvent = new TenantRequestSubmittedEvent(tenantRequest);

        // Act
        await handler.Handle(submittedEvent, CancellationToken.None);

        // Assert
        emailService.SentEmails.Should().HaveCount(2, "Should send emails to tenant and superintendent");

        var tenantEmail = emailService.SentEmails.FirstOrDefault(e => e.RecipientEmail == tenantRequest.TenantEmail);
        var superintendentEmail =
            emailService.SentEmails.FirstOrDefault(e => e.RecipientEmail == tenantRequest.SuperintendentEmail);

        tenantEmail.Should().NotBeNull();
        tenantEmail!.Subject.Should().Contain("Request Submitted");
        tenantEmail.Body.Should().Contain(tenantRequest.TenantFullName);
        tenantEmail.Body.Should().Contain(tenantRequest.Title);

        superintendentEmail.Should().NotBeNull();
        superintendentEmail!.Subject.Should().Contain("New Request");
        superintendentEmail.Body.Should().Contain(tenantRequest.PropertyName);
        superintendentEmail.Body.Should().Contain(tenantRequest.TenantUnit);
    }

    [Fact]
    public async Task TenantRequestScheduledEvent_Should_Send_Three_Emails()
    {
        // Arrange
        var emailLogger = new MockLogger<TestableEmailService>();
        var handlerLogger = new MockLogger<TenantRequestNotificationHandler>();
        var emailService = new TestableEmailService(emailLogger);
        var handler = new TenantRequestNotificationHandler(emailService, handlerLogger);

        var tenantRequest = CreateTestTenantRequest();
        var scheduleInfo = new ServiceWorkScheduleInfo(
            DateTime.Now.AddDays(1),
            "worker@example.com",
            "WO-12345",
            1);

        var scheduledEvent = new TenantRequestScheduledEvent(tenantRequest, scheduleInfo);

        // Act
        await handler.Handle(scheduledEvent, CancellationToken.None);

        // Assert
        emailService.SentEmails.Should().HaveCount(3, "Should send emails to tenant, superintendent, and worker");

        var recipients = emailService.SentEmails.Select(e => e.RecipientEmail).ToList();
        recipients.Should().Contain(tenantRequest.TenantEmail);
        recipients.Should().Contain(tenantRequest.SuperintendentEmail);
        recipients.Should().Contain("worker@example.com");
    }

    [Fact]
    public async Task TenantRequestCompletedEvent_Should_Send_Two_Emails()
    {
        // Arrange
        var emailLogger = new MockLogger<TestableEmailService>();
        var handlerLogger = new MockLogger<TenantRequestNotificationHandler>();
        var emailService = new TestableEmailService(emailLogger);
        var handler = new TenantRequestNotificationHandler(emailService, handlerLogger);

        var tenantRequest = CreateTestTenantRequest();
        tenantRequest.Submit();
        tenantRequest.Schedule(DateTime.UtcNow.AddDays(1), "worker@test.com", "WO-001");
        tenantRequest.ReportWorkCompleted(true, "Work completed successfully");

        var completedEvent = new TenantRequestCompletedEvent(tenantRequest, "Work completed successfully");

        // Act
        await handler.Handle(completedEvent, CancellationToken.None);

        // Assert
        emailService.SentEmails.Should().HaveCount(2, "Should send emails to tenant and superintendent");

        var tenantEmail = emailService.SentEmails.FirstOrDefault(e => e.RecipientEmail == tenantRequest.TenantEmail);
        var superintendentEmail =
            emailService.SentEmails.FirstOrDefault(e => e.RecipientEmail == tenantRequest.SuperintendentEmail);

        tenantEmail.Should().NotBeNull();
        tenantEmail!.Subject.Should().Contain("Work Completed");
        tenantEmail.Body.Should().Contain("completed successfully");

        superintendentEmail.Should().NotBeNull();
        superintendentEmail!.Subject.Should().Contain("Work Completed");
    }

    private static TenantRequest CreateTestTenantRequest()
    {
        return TenantRequest.CreateNew(
            "REQ-2024-001",
            "Leaky Faucet",
            "The kitchen faucet is dripping constantly",
            "Normal",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john.doe@example.com",
            "Apt 101",
            "Sunset Apartments",
            "+1-555-123-4567",
            "Jane Smith",
            "jane.smith@example.com"
        );
    }

    private class MockLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}