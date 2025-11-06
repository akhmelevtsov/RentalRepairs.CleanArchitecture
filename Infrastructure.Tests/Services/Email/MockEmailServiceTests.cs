using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Tests.TestHelpers;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Infrastructure.Tests.Services.Email;

/// <summary>
/// Tests for email service functionality using TestableEmailService for assertions
/// </summary>
public class MockEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_Should_LogEmail_And_Store_In_History()
    {
        // Arrange
        var logger = new MockLogger<TestableEmailService>();
        var emailService = new TestableEmailService(logger);

        var emailInfo = new EmailInfo
        {
            RecipientEmail = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            SenderEmail = "sender@example.com"
        };

        // Act
        await emailService.SendEmailAsync(emailInfo);

        // Assert
        emailService.LastSentEmail.Should().NotBeNull();
        emailService.LastSentEmail!.RecipientEmail.Should().Be("test@example.com");
        emailService.LastSentEmail.Subject.Should().Be("Test Subject");
        emailService.LastSentEmail.Body.Should().Be("Test Body");

        emailService.SentEmails.Should().HaveCount(1);
        emailService.SentEmails[0].Should().Be(emailInfo);
    }

    [Fact]
    public async Task SendBulkEmailAsync_Should_Send_Multiple_Emails()
    {
        // Arrange
        var logger = new MockLogger<TestableEmailService>();
        var emailService = new TestableEmailService(logger);

        var emails = new[]
        {
            new EmailInfo { RecipientEmail = "user1@example.com", Subject = "Test 1", Body = "Body 1" },
            new EmailInfo { RecipientEmail = "user2@example.com", Subject = "Test 2", Body = "Body 2" },
            new EmailInfo { RecipientEmail = "user3@example.com", Subject = "Test 3", Body = "Body 3" }
        };

        // Act
        await emailService.SendBulkEmailAsync(emails);

        // Assert
        emailService.SentEmails.Should().HaveCount(3);
        emailService.LastSentEmail!.RecipientEmail.Should().Be("user3@example.com");
    }

    [Fact]
    public async Task SendEmailAsync_With_Null_Email_Should_Not_Throw()
    {
        // Arrange
        var logger = new MockLogger<TestableEmailService>();
        var emailService = new TestableEmailService(logger);

        // Act & Assert
        await emailService.SendEmailAsync(null!);

        emailService.SentEmails.Should().BeEmpty();
        emailService.LastSentEmail.Should().BeNull();
    }

    [Fact]
    public async Task ClearHistory_Should_Remove_All_Sent_Emails()
    {
        // Arrange
        var logger = new MockLogger<TestableEmailService>();
        var emailService = new TestableEmailService(logger);

        var emailInfo = new EmailInfo
        {
            RecipientEmail = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        // Act
        await emailService.SendEmailAsync(emailInfo);
        emailService.ClearHistory();

        // Assert
        emailService.SentEmails.Should().BeEmpty();
        emailService.LastSentEmail.Should().BeNull();
    }

    [Fact]
    public async Task HasSentEmailTo_Should_Return_True_When_Email_Sent()
    {
        // Arrange
        var logger = new MockLogger<TestableEmailService>();
        var emailService = new TestableEmailService(logger);

        var emailInfo = new EmailInfo
        {
            RecipientEmail = "recipient@example.com",
            Subject = "Test",
            Body = "Test"
        };

        // Act
        await emailService.SendEmailAsync(emailInfo);

        // Assert
        emailService.HasSentEmailTo("recipient@example.com").Should().BeTrue();
        emailService.HasSentEmailTo("other@example.com").Should().BeFalse();
    }

    [Fact]
    public async Task GetEmailsSentTo_Should_Return_Matching_Emails()
    {
        // Arrange
        var logger = new MockLogger<TestableEmailService>();
        var emailService = new TestableEmailService(logger);

        await emailService.SendEmailAsync(new EmailInfo
        {
            RecipientEmail = "user1@example.com",
            Subject = "Email 1"
        });
        await emailService.SendEmailAsync(new EmailInfo
        {
            RecipientEmail = "user2@example.com",
            Subject = "Email 2"
        });
        await emailService.SendEmailAsync(new EmailInfo
        {
            RecipientEmail = "user1@example.com",
            Subject = "Email 3"
        });

        // Act
        var user1Emails = emailService.GetEmailsSentTo("user1@example.com").ToList();

        // Assert
        user1Emails.Should().HaveCount(2);
        user1Emails.All(e => e.RecipientEmail == "user1@example.com").Should().BeTrue();
    }

    [Fact]
    public async Task HasEmailWithSubject_Should_Return_True_When_Subject_Matches()
    {
        // Arrange
        var logger = new MockLogger<TestableEmailService>();
        var emailService = new TestableEmailService(logger);

        await emailService.SendEmailAsync(new EmailInfo
        {
            RecipientEmail = "test@example.com",
            Subject = "Important: Action Required",
            Body = "Test"
        });

        // Act & Assert
        emailService.HasEmailWithSubject("Action Required").Should().BeTrue();
        emailService.HasEmailWithSubject("important").Should().BeTrue(); // Case insensitive
        emailService.HasEmailWithSubject("Irrelevant").Should().BeFalse();
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