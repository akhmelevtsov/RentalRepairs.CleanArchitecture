using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Services.Notifications;

namespace RentalRepairs.Application.Services;

/// <summary>
///     Legacy NotificationService - now acts as a facade/adapter
///     Delegates to specialized notification services
///     Maintains backward compatibility during migration
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ISuperintendentNotificationService _superintendentNotificationService;
    private readonly ITenantNotificationService _tenantNotificationService;
    private readonly IWorkerNotificationService _workerNotificationService;

    public NotificationService(
        ITenantNotificationService tenantNotificationService,
        ISuperintendentNotificationService superintendentNotificationService,
        IWorkerNotificationService workerNotificationService,
        IEmailNotificationService emailNotificationService)
    {
        _tenantNotificationService = tenantNotificationService;
        _superintendentNotificationService = superintendentNotificationService;
        _workerNotificationService = workerNotificationService;
        _emailNotificationService = emailNotificationService;
    }

    /// <summary>
    ///     Notifies tenant when their request status changes
    /// </summary>
    public Task NotifyTenantRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        string? additionalMessage = null,
        CancellationToken cancellationToken = default)
    {
        return _tenantNotificationService.NotifyRequestStatusChangedAsync(
            requestId, newStatus, additionalMessage, cancellationToken);
    }

    /// <summary>
    ///     Notifies property superintendent about request events
    /// </summary>
    public Task NotifySuperintendentRequestEventAsync(
        Guid requestId,
        string eventType,
        string? additionalDetails = null,
        CancellationToken cancellationToken = default)
    {
        return _superintendentNotificationService.NotifyRequestEventAsync(
            requestId, eventType, additionalDetails, cancellationToken);
    }

    /// <summary>
    ///     Notifies assigned worker about work assignments
    /// </summary>
    public Task NotifyWorkerAssignmentAsync(
        Guid requestId,
        string workerEmail,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default)
    {
        return _workerNotificationService.NotifyAssignmentAsync(
            requestId, workerEmail, scheduledDate, cancellationToken);
    }

    /// <summary>
    ///     Notifies worker about status changes (activation/deactivation)
    /// </summary>
    public Task NotifyWorkerStatusChangeAsync(
        string workerEmail,
        bool isActivated,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return _workerNotificationService.NotifyStatusChangeAsync(
            workerEmail, isActivated, reason, cancellationToken);
    }

    /// <summary>
    ///     Sends emergency notifications for critical requests
    /// </summary>
    public Task SendEmergencyNotificationAsync(
        Guid requestId,
        string urgencyLevel,
        CancellationToken cancellationToken = default)
    {
        return _superintendentNotificationService.SendEmergencyNotificationAsync(
            requestId, urgencyLevel, cancellationToken);
    }

    /// <summary>
    ///     Batch notification for overdue requests
    /// </summary>
    public Task NotifyOverdueRequestsAsync(
        List<Guid> overdueRequestIds,
        CancellationToken cancellationToken = default)
    {
        return _superintendentNotificationService.NotifyOverdueRequestsAsync(
            overdueRequestIds, cancellationToken);
    }

    /// <summary>
    ///     Custom notification with template support
    /// </summary>
    public Task SendCustomNotificationAsync(
        string recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        return _emailNotificationService.SendEmailAsync(
            recipientEmail, subject, message, cancellationToken);
    }
}