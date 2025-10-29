using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;

namespace RentalRepairs.Application.Services;

/// <summary>
/// Consolidated Notification Service
/// Handles all cross-cutting notification concerns across the application
/// Renamed and expanded from NotifyPartiesService for better clarity
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IMediator mediator, ILogger<NotificationService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Notifies tenant when their request status changes
    /// </summary>
    public async Task NotifyTenantRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        string? additionalMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
            if (request == null)
            {
                _logger.LogWarning("Cannot notify tenant - request {RequestId} not found", requestId);
                return;
            }

            var subject = $"Request Status Update - {request.Title}";
            var message = $"Dear Tenant,\n\nYour maintenance request status has been updated to: {newStatus}\n\n" +
                         $"Request: {request.Title}\n" +
                         $"Description: {request.Description}\n";

            if (!string.IsNullOrEmpty(additionalMessage))
                message += $"\nAdditional Information: {additionalMessage}";

            message += "\n\nThank you for your patience.";

            await SendCustomNotificationAsync(request.TenantEmail, subject, message, cancellationToken);
            
            _logger.LogInformation("Tenant notification sent for request {RequestId} status change to {Status}", 
                requestId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending tenant notification for request {RequestId}", requestId);
            throw;
        }
    }

    /// <summary>
    /// Notifies property superintendent about request events
    /// </summary>
    public async Task NotifySuperintendentRequestEventAsync(
        Guid requestId,
        string eventType,
        string? additionalDetails = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
            if (request == null)
            {
                _logger.LogWarning("Cannot notify superintendent - request {RequestId} not found", requestId);
                return;
            }

            var subject = $"Request Event: {eventType} - {request.Title}";
            var message = $"Dear Property Superintendent,\n\n" +
                         $"Event: {eventType}\n" +
                         $"Request: {request.Title}\n" +
                         $"Status: {request.Status}\n" +
                         $"Urgency: {request.UrgencyLevel}\n";

            if (!string.IsNullOrEmpty(additionalDetails))
                message += $"\nDetails: {additionalDetails}";

            message += "\n\nPlease review and take appropriate action.";

            // In production, would get superintendent email from property data
            var superintendentEmail = "superintendent@property.com"; // Placeholder
            
            await SendCustomNotificationAsync(superintendentEmail, subject, message, cancellationToken);
            
            _logger.LogInformation("Superintendent notification sent for request {RequestId} event {EventType}", 
                requestId, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending superintendent notification for request {RequestId}", requestId);
            throw;
        }
    }

    /// <summary>
    /// Notifies assigned worker about work assignments
    /// </summary>
    public async Task NotifyWorkerAssignmentAsync(
        Guid requestId,
        string workerEmail,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
            if (request == null)
            {
                _logger.LogWarning("Cannot notify worker - request {RequestId} not found", requestId);
                return;
            }

            var subject = $"Work Assignment - {request.Title}";
            var message = $"Dear Worker,\n\n" +
                         $"You have been assigned to a maintenance request:\n\n" +
                         $"Request: {request.Title}\n" +
                         $"Description: {request.Description}\n" +
                         $"Scheduled Date: {scheduledDate:yyyy-MM-dd HH:mm}\n" +
                         $"Urgency Level: {request.UrgencyLevel}\n" +
                         $"Work Order: {request.WorkOrderNumber}\n\n" +
                         $"Please contact the tenant to arrange access.\n\n" +
                         $"Thank you.";

            await SendCustomNotificationAsync(workerEmail, subject, message, cancellationToken);
            
            _logger.LogInformation("Worker notification sent for assignment {RequestId} to {WorkerEmail}", 
                requestId, workerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending worker assignment notification for request {RequestId}", requestId);
            throw;
        }
    }

    /// <summary>
    /// Notifies worker about status changes (activation/deactivation)
    /// </summary>
    public async Task NotifyWorkerStatusChangeAsync(
        string workerEmail,
        bool isActivated,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = isActivated ? "Activated" : "Deactivated";
            var subject = $"Worker Status Update - {status}";
            var message = $"Dear Worker,\n\n" +
                         $"Your worker status has been updated to: {status}\n\n";

            if (!string.IsNullOrEmpty(reason))
                message += $"Reason: {reason}\n\n";

            if (isActivated)
            {
                message += "You can now receive new work assignments.\n\n";
            }
            else
            {
                message += "You will no longer receive new work assignments. " +
                          "Any existing assignments will need to be completed or reassigned.\n\n";
            }

            message += "If you have any questions, please contact your supervisor.\n\nThank you.";

            await SendCustomNotificationAsync(workerEmail, subject, message, cancellationToken);
            
            _logger.LogInformation("Worker status change notification sent to {WorkerEmail}: {Status}", 
                workerEmail, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending worker status change notification to {WorkerEmail}", workerEmail);
            throw;
        }
    }

    /// <summary>
    /// Sends emergency notifications for critical requests
    /// </summary>
    public async Task SendEmergencyNotificationAsync(
        Guid requestId,
        string urgencyLevel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
            if (request == null)
            {
                _logger.LogWarning("Cannot send emergency notification - request {RequestId} not found", requestId);
                return;
            }

            var subject = $"EMERGENCY REQUEST - {urgencyLevel} - {request.Title}";
            var message = $"EMERGENCY MAINTENANCE REQUEST ??\n\n" +
                         $"Urgency Level: {urgencyLevel}\n" +
                         $"Request: {request.Title}\n" +
                         $"Description: {request.Description}\n" +
                         $"Tenant Email: {request.TenantEmail}\n" +
                         $"Submitted: {request.CreatedDate:yyyy-MM-dd HH:mm}\n\n" +
                         $"IMMEDIATE ATTENTION REQUIRED ??\n\n" +
                         $"Please assign a worker immediately.";

            // Send to both superintendent and emergency contact
            var emergencyContacts = new[] { "superintendent@property.com", "emergency@maintenance.com" };
            
            foreach (var contact in emergencyContacts)
            {
                await SendCustomNotificationAsync(contact, subject, message, cancellationToken);
            }
            
            _logger.LogWarning("Emergency notification sent for request {RequestId} with urgency {UrgencyLevel}", 
                requestId, urgencyLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emergency notification for request {RequestId}", requestId);
            throw;
        }
    }

    /// <summary>
    /// Batch notification for overdue requests
    /// </summary>
    public async Task NotifyOverdueRequestsAsync(
        List<Guid> overdueRequestIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!overdueRequestIds.Any())
                return;

            var subject = $"Overdue Requests Report - {overdueRequestIds.Count} requests";
            var message = $"Dear Superintendent,\n\n" +
                         $"The following {overdueRequestIds.Count} requests are overdue and require attention:\n\n";

            foreach (var requestId in overdueRequestIds.Take(10)) // Limit to 10 for readability
            {
                try
                {
                    var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
                    if (request != null)
                    {
                        message += $"- {request.Title} (Created: {request.CreatedDate:yyyy-MM-dd}, Status: {request.Status})\n";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting details for overdue request {RequestId}", requestId);
                }
            }

            if (overdueRequestIds.Count > 10)
            {
                message += $"... and {overdueRequestIds.Count - 10} more requests.\n";
            }

            message += "\nPlease review and take appropriate action.\n\nThank you.";

            await SendCustomNotificationAsync("superintendent@property.com", subject, message, cancellationToken);
            
            _logger.LogInformation("Overdue requests notification sent for {Count} requests", overdueRequestIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending overdue requests notification");
            throw;
        }
    }

    /// <summary>
    /// Custom notification with template support
    /// </summary>
    public async Task SendCustomNotificationAsync(
        string recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In production, this would use an actual email service
            // For now, just log the notification
            _logger.LogInformation(
                "Notification sent to {RecipientEmail}\nSubject: {Subject}\nMessage: {Message}", 
                recipientEmail, subject, message);

            // Simulate async email sending
            await Task.Delay(100, cancellationToken);
            
            _logger.LogInformation("Custom notification sent to {RecipientEmail}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending custom notification to {RecipientEmail}", recipientEmail);
            throw;
        }
    }
}