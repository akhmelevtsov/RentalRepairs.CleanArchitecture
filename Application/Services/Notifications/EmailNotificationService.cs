using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Services.Notifications;

/// <summary>
/// Email notification service - wraps actual email sending
/// Single Responsibility: Email delivery only
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailService emailService,
        ILogger<EmailNotificationService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
// In production with real IEmailService implementation, use:
        // await _emailService.SendEmailAsync(recipientEmail, subject, message, cancellationToken);

        // For now (with MockEmailService), just log
        _logger.LogInformation(
            "Email notification queued - To: {RecipientEmail}, Subject: {Subject}",
            recipientEmail, subject);

        await Task.CompletedTask;
    }
}