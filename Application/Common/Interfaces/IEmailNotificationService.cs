namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// Base interface for sending email notifications
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Sends an email notification to a recipient
    /// </summary>
    Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default);
}