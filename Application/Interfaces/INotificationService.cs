namespace RentalRepairs.Application.Interfaces;

/// <summary>
/// Consolidated Notification Service Interface
/// Handles all cross-cutting notification concerns across the application
/// Renamed from INotifyPartiesService for clarity and expanded functionality
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Notifies tenant when their request status changes
    /// </summary>
    Task NotifyTenantRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        string? additionalMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies property superintendent about request events
    /// </summary>
    Task NotifySuperintendentRequestEventAsync(
        Guid requestId,
        string eventType,
        string? additionalDetails = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies assigned worker about work assignments
    /// </summary>
    Task NotifyWorkerAssignmentAsync(
        Guid requestId,
        string workerEmail,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies worker about status changes (activation/deactivation)
    /// </summary>
    Task NotifyWorkerStatusChangeAsync(
        string workerEmail,
        bool isActivated,
        string? reason = null,
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

    /// <summary>
    /// Custom notification with template support
    /// </summary>
    Task SendCustomNotificationAsync(
        string recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default);
}