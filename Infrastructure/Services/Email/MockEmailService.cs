using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Infrastructure.Services.Email;

/// <summary>
/// Mock email service for development and testing.
/// For test assertions, use TestableEmailService wrapper in test projects.
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(EmailInfo emailInfo, CancellationToken cancellationToken = default)
    {
        if (emailInfo == null)
        {
            _logger.LogWarning("Attempted to send null email");
            return;
        }

        _logger.LogInformation("Mock email sent to {RecipientEmail} with subject '{Subject}'",
            emailInfo.RecipientEmail, emailInfo.Subject);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var emailLog = $"""
                            ==================== MOCK EMAIL ====================
                            From: {emailInfo.SenderEmail}
                            To: {emailInfo.RecipientEmail}
                            CC: {string.Join(", ", emailInfo.CcEmails)}
                            BCC: {string.Join(", ", emailInfo.BccEmails)}
                            Subject: {emailInfo.Subject}
                            Body:
                            {emailInfo.Body}
                            ====================================================
                            """;

            _logger.LogDebug("{EmailLog}", emailLog);
        }

        // Simulate async operation
        await Task.Delay(50, cancellationToken);
    }

    public async Task SendBulkEmailAsync(IEnumerable<EmailInfo> emails, CancellationToken cancellationToken = default)
    {
        foreach (var email in emails) await SendEmailAsync(email, cancellationToken);
    }
}