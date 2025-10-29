namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// Configuration for email notifications
/// </summary>
[Obsolete("Configuration moved to InfrastructureOptions")]
public interface INotificationSettings
{
    string DefaultSenderEmail { get; }
    string DefaultSenderName { get; }
    bool EnableEmailNotifications { get; }
    string NoReplyEmail { get; }
}

/// <summary>
/// ? Deprecated - Use IBusinessNotificationService instead
/// Kept for backward compatibility during migration
/// </summary>
[Obsolete("Use IBusinessNotificationService for business notifications or IMessageDeliveryService for direct message delivery")]
public interface INotificationService
{
    // Core email functionality
    [Obsolete("Use IBusinessNotificationService.NotifyAsync instead")]
    Task SendEmailNotificationAsync(string recipientEmail, string subject, string message, CancellationToken cancellationToken = default);
    
    [Obsolete("Use IBusinessNotificationService.NotifyAsync with template instead")]
    Task SendTemplatedEmailAsync(string recipientEmail, string templateId, object templateData, CancellationToken cancellationToken = default);
    
    // Bulk notifications
    [Obsolete("Use IBusinessNotificationService.NotifyBulkAsync instead")]
    Task SendBulkNotificationAsync(IEnumerable<string> recipientEmails, string subject, string message, CancellationToken cancellationToken = default);
    
    // Business domain notifications
    [Obsolete("Use IBusinessNotificationService.NotifyTenantAsync instead")]
    Task NotifyTenantAsync(int tenantId, string subject, string message, CancellationToken cancellationToken = default);
    
    [Obsolete("Use IBusinessNotificationService.NotifyPropertySuperintendentAsync instead")]
    Task NotifyPropertySuperintendentAsync(int propertyId, string subject, string message, CancellationToken cancellationToken = default);
    
    [Obsolete("Use IBusinessNotificationService.NotifyWorkerAsync instead")]
    Task NotifyWorkerAsync(string workerEmail, string subject, string message, CancellationToken cancellationToken = default);
    
    // Advanced notification features
    [Obsolete("Use IBusinessNotificationService.NotifyAsync with ScheduledTime instead")]
    Task ScheduleNotificationAsync(string recipientEmail, string subject, string message, DateTime scheduledTime, CancellationToken cancellationToken = default);
    
    [Obsolete("Use IBusinessNotificationService.NotifyAsync with Critical priority instead")]
    Task SendUrgentNotificationAsync(string recipientEmail, string subject, string message, CancellationToken cancellationToken = default);
}