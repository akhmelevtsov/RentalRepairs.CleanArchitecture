namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// ? Business-focused notification service - Issue #15 resolution
/// Abstracts business notifications from delivery mechanisms
/// </summary>
public interface IBusinessNotificationService
{
    Task<NotificationResult> NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationResult>> NotifyBulkAsync(IEnumerable<NotificationRequest> requests, CancellationToken cancellationToken = default);
    Task<NotificationResult> NotifyTenantAsync(int tenantId, NotificationType type, Dictionary<string, object> data, CancellationToken cancellationToken = default);
    Task<NotificationResult> NotifyWorkerAsync(string workerEmail, NotificationType type, Dictionary<string, object> data, CancellationToken cancellationToken = default);
    Task<NotificationResult> NotifyPropertySuperintendentAsync(int propertyId, NotificationType type, Dictionary<string, object> data, CancellationToken cancellationToken = default);
}

/// <summary>
/// ? Business notification request - focused on business context, not delivery
/// </summary>
public class NotificationRequest
{
    public required NotificationType Type { get; init; }
    public required string RecipientId { get; init; }
    public required string TemplateId { get; init; }
    public Dictionary<string, object> TemplateData { get; init; } = new();
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;
    public List<DeliveryChannel> PreferredChannels { get; init; } = new() { DeliveryChannel.Email };
    public DateTime? ScheduledTime { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientLanguage { get; init; } = "en";
}

/// <summary>
/// ? Business notification types for rental repairs domain
/// </summary>
public enum NotificationType
{
    // Tenant Request Notifications
    TenantRequestSubmitted,
    TenantRequestAssigned,
    TenantRequestInProgress,
    TenantRequestCompleted,
    TenantRequestCancelled,
    
    // Worker Notifications
    WorkerAssigned,
    WorkScheduleUpdated,
    WorkReminder,
    
    // Property Management Notifications
    PropertyRegistered,
    MaintenanceScheduled,
    PropertyInspectionRequired,
    
    // System Notifications
    SystemAlert,
    MaintenanceReminder,
    PaymentReminder,
    
    // Emergency Notifications
    EmergencyRepairRequest,
    HealthSafetyIssue
}

/// <summary>
/// ? Notification priority for business context
/// </summary>
public enum NotificationPriority
{
    Low,        // Informational updates
    Normal,     // Standard business notifications
    High,       // Important updates requiring attention
    Critical    // Emergency notifications requiring immediate action
}

/// <summary>
/// ? Business notification result with delivery tracking
/// </summary>
public class NotificationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public List<DeliveryAttempt> DeliveryAttempts { get; init; } = new();
    public NotificationRequest OriginalRequest { get; init; } = null!;
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
    public string? NotificationId { get; init; }

    public static NotificationResult Success(NotificationRequest request, List<DeliveryAttempt> attempts, string? notificationId = null)
    {
        return new NotificationResult
        {
            IsSuccess = true,
            OriginalRequest = request,
            DeliveryAttempts = attempts,
            NotificationId = notificationId
        };
    }

    public static NotificationResult Failure(NotificationRequest request, string errorMessage)
    {
        return new NotificationResult
        {
            IsSuccess = false,
            OriginalRequest = request,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// ? Individual delivery attempt tracking
/// </summary>
public class DeliveryAttempt
{
    public DeliveryChannel Channel { get; init; }
    public DeliveryResult Result { get; init; } = null!;
    public DateTime AttemptedAt { get; init; } = DateTime.UtcNow;
    public int AttemptNumber { get; init; }
    public string? ProviderName { get; init; }
}