namespace RentalRepairs.Application.Services.Notifications;

/// <summary>
/// Handles tenant-specific notifications
/// </summary>
public interface ITenantNotificationService
{
    /// <summary>
    /// Notifies tenant when their request status changes
    /// </summary>
    Task NotifyRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        string? additionalMessage = null,
        CancellationToken cancellationToken = default);
}