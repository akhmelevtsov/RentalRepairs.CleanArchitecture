using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Testable email service wrapper that provides assertion capabilities for tests.
/// Wraps the production MockEmailService and adds test-only tracking features.
/// </summary>
public class TestableEmailService : IEmailService
{
    private readonly ILogger<TestableEmailService> _logger;
    private readonly List<EmailInfo> _sentEmails = new();

    public TestableEmailService(ILogger<TestableEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Read-only list of sent emails for test assertions
    /// </summary>
    public IReadOnlyList<EmailInfo> SentEmails => _sentEmails.AsReadOnly();

    /// <summary>
    /// Last sent email for test assertions
    /// </summary>
    public EmailInfo? LastSentEmail { get; private set; }

    public async Task SendEmailAsync(EmailInfo emailInfo, CancellationToken cancellationToken = default)
    {
        if (emailInfo == null)
        {
            _logger.LogWarning("Attempted to send null email");
            return;
        }

        // Track for testing
        _sentEmails.Add(emailInfo);
        LastSentEmail = emailInfo;

        _logger.LogInformation("Test email sent to {RecipientEmail} with subject '{Subject}'",
            emailInfo.RecipientEmail, emailInfo.Subject);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var emailLog = "==================== TEST EMAIL ====================" +
                           $"\nFrom: {emailInfo.SenderEmail}" +
                           $"\nTo: {emailInfo.RecipientEmail}" +
                           $"\nCC: {string.Join(", ", emailInfo.CcEmails)}" +
                           $"\nBCC: {string.Join(", ", emailInfo.BccEmails)}" +
                           $"\nSubject: {emailInfo.Subject}" +
                           $"\nBody:\n{emailInfo.Body}" +
                           "\n====================================================";

            _logger.LogDebug("{EmailLog}", emailLog);
        }

        // Simulate async operation
        await Task.Delay(10, cancellationToken);
    }

    public async Task SendBulkEmailAsync(IEnumerable<EmailInfo> emails, CancellationToken cancellationToken = default)
    {
        foreach (var email in emails) await SendEmailAsync(email, cancellationToken);
    }

    /// <summary>
    /// Clear sent emails history - useful for test cleanup
    /// </summary>
    public void ClearHistory()
    {
        _sentEmails.Clear();
        LastSentEmail = null;
    }

    /// <summary>
    /// Get count of sent emails - convenient for tests
    /// </summary>
    public int SentEmailCount => _sentEmails.Count;

    /// <summary>
    /// Check if any email was sent to specific recipient
    /// </summary>
    public bool HasSentEmailTo(string recipientEmail)
    {
        return _sentEmails.Any(e => e.RecipientEmail.Equals(recipientEmail, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all emails sent to specific recipient
    /// </summary>
    public IEnumerable<EmailInfo> GetEmailsSentTo(string recipientEmail)
    {
        return _sentEmails.Where(e => e.RecipientEmail.Equals(recipientEmail, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if any email contains specific subject
    /// </summary>
    public bool HasEmailWithSubject(string subject)
    {
        return _sentEmails.Any(e => e.Subject.Contains(subject, StringComparison.OrdinalIgnoreCase));
    }
}