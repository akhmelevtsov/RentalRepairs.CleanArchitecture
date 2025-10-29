using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Services.Email;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Infrastructure.Tests.Services.Email;

public class MockEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_Should_LogEmail_And_Store_In_History()
    {
        // Arrange
        var logger = new MockLogger<MockEmailService>();
        var emailService = new MockEmailService(logger);
        
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
        var logger = new MockLogger<MockEmailService>();
        var emailService = new MockEmailService(logger);
        
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
        var logger = new MockLogger<MockEmailService>();
        var emailService = new MockEmailService(logger);

        // Act & Assert
        await emailService.SendEmailAsync(null!);
        
        emailService.SentEmails.Should().BeEmpty();
        emailService.LastSentEmail.Should().BeNull();
    }

    [Fact]
    public void ClearHistory_Should_Remove_All_Sent_Emails()
    {
        // Arrange
        var logger = new MockLogger<MockEmailService>();
        var emailService = new MockEmailService(logger);
        
        var emailInfo = new EmailInfo
        {
            RecipientEmail = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        // Act
        emailService.SendEmailAsync(emailInfo).Wait();
        emailService.ClearHistory();

        // Assert
        emailService.SentEmails.Should().BeEmpty();
        emailService.LastSentEmail.Should().BeNull();
    }

    private class MockLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}