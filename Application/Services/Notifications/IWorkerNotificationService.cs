namespace RentalRepairs.Application.Services.Notifications;

/// <summary>
/// Handles worker-specific notifications
/// </summary>
public interface IWorkerNotificationService
{
    /// <summary>
    /// Notifies assigned worker about work assignments
    /// </summary>
    Task NotifyAssignmentAsync(
        Guid requestId,
        string workerEmail,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies worker about status changes (activation/deactivation)
    /// </summary>
    Task NotifyStatusChangeAsync(
        string workerEmail,
        bool isActivated,
        string? reason = null,
        CancellationToken cancellationToken = default);
}