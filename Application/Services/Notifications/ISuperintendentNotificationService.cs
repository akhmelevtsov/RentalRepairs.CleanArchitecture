namespace RentalRepairs.Application.Services.Notifications;

/// <summary>
/// Handles superintendent-specific notifications
/// </summary>
public interface ISuperintendentNotificationService
{
    /// <summary>
    /// Notifies property superintendent about request events
    /// </summary>
    Task NotifyRequestEventAsync(
        Guid requestId,
        string eventType,
        string? additionalDetails = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends emergency notifications for critical requests
    /// </summary>
    Task SendEmergencyNotificationAsync(
        Guid requestId,
        string urgencyLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch notification for overdue requests
    /// </summary>
    Task NotifyOverdueRequestsAsync(
        List<Guid> overdueRequestIds,
        CancellationToken cancellationToken = default);
}