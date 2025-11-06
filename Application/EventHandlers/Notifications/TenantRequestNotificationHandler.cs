using MediatR;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Notifications;

/// <summary>
/// ? Event-driven notification handler - replaces bloated NotifyPartiesService
/// </summary>
public class TenantRequestNotificationHandler :
    INotificationHandler<TenantRequestSubmittedEvent>,
    INotificationHandler<TenantRequestScheduledEvent>,
    INotificationHandler<TenantRequestCompletedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TenantRequestNotificationHandler> _logger;

    public TenantRequestNotificationHandler(
        IEmailService emailService,
        ILogger<TenantRequestNotificationHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(TenantRequestSubmittedEvent @event, CancellationToken cancellationToken)
    {
        var request = @event.TenantRequest;

        try
        {
            // ? Business logic encapsulated in domain value object
            var tenantNotification = NotificationData.CreateTenantSubmissionNotification(request);
            var superintendentNotification = NotificationData.CreateSuperintendentNewRequestNotification(request);

            // ? Convert to EmailInfo objects
            var tenantEmailInfo = CreateEmailInfo(tenantNotification);
            var superintendentEmailInfo = CreateEmailInfo(superintendentNotification);

            // ? Send notifications in parallel for performance
            var tenantTask = _emailService.SendEmailAsync(tenantEmailInfo, cancellationToken);
            var superintendentTask = _emailService.SendEmailAsync(superintendentEmailInfo, cancellationToken);

            await Task.WhenAll(tenantTask, superintendentTask);

            _logger.LogInformation(
                "Notifications sent for request submission {RequestCode} to tenant {TenantEmail} and superintendent {SuperintendentEmail}",
                request.Code, request.TenantEmail, request.SuperintendentEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send notifications for request submission {RequestCode}",
                request.Code);
            throw;
        }
    }

    public async Task Handle(TenantRequestScheduledEvent @event, CancellationToken cancellationToken)
    {
        var request = @event.TenantRequest;
        var scheduleInfo = @event.ScheduleInfo;

        try
        {
            // ? Create notifications using domain business logic
            var tenantNotification = CreateTenantScheduledNotification(request);
            var superintendentNotification = CreateSuperintendentScheduledNotification(request);
            var workerNotification =
                NotificationData.CreateWorkerAssignmentNotification(request, scheduleInfo.WorkerEmail);

            // ? Convert to EmailInfo objects
            var tenantEmailInfo = CreateEmailInfo(tenantNotification);
            var superintendentEmailInfo = CreateEmailInfo(superintendentNotification);
            var workerEmailInfo = CreateEmailInfo(workerNotification);

            // ? Send all notifications in parallel
            var tasks = new[]
            {
                _emailService.SendEmailAsync(tenantEmailInfo, cancellationToken),
                _emailService.SendEmailAsync(superintendentEmailInfo, cancellationToken),
                _emailService.SendEmailAsync(workerEmailInfo, cancellationToken)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Scheduling notifications sent for request {RequestCode} to tenant, superintendent, and worker {WorkerEmail}",
                request.Code, scheduleInfo.WorkerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send scheduling notifications for request {RequestCode}",
                request.Code);
            throw;
        }
    }

    public async Task Handle(TenantRequestCompletedEvent @event, CancellationToken cancellationToken)
    {
        var request = @event.TenantRequest;
        var notes = @event.CompletionNotes; // ? Fix: Use correct property name

        // ? Determine success based on request status and completion data
        var successful = request.WorkCompletedSuccessfully == true;

        try
        {
            var tenantNotification = CreateTenantCompletionNotification(request, successful, notes);
            var superintendentNotification = CreateSuperintendentCompletionNotification(request, successful, notes);

            // ? Convert to EmailInfo objects
            var tenantEmailInfo = CreateEmailInfo(tenantNotification);
            var superintendentEmailInfo = CreateEmailInfo(superintendentNotification);

            var tasks = new[]
            {
                _emailService.SendEmailAsync(tenantEmailInfo, cancellationToken),
                _emailService.SendEmailAsync(superintendentEmailInfo, cancellationToken)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Completion notifications sent for request {RequestCode} - Status: {Status}",
                request.Code, successful ? "Success" : "Issues");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send completion notifications for request {RequestCode}",
                request.Code);
            throw;
        }
    }

    // ? Helper method to convert NotificationData to EmailInfo
    private static EmailInfo CreateEmailInfo(NotificationData notification)
    {
        return new EmailInfo
        {
            RecipientEmail = notification.RecipientEmail,
            Subject = notification.Subject,
            Body = notification.Body,
            SenderEmail = "noreply@rentalrepairs.com", // Default sender
            IsBodyHtml = false // Plain text emails for now
        };
    }

    private static NotificationData CreateTenantScheduledNotification(Domain.Entities.TenantRequest request)
    {
        var body = $@"Dear {request.TenantFullName},

Your maintenance request has been scheduled for service.

Scheduled Work Details:
• Request: {request.Title}
• Scheduled Date: {request.ScheduledDate?.ToString("yyyy-MM-dd") ?? "TBD"}
• Work Order: {request.WorkOrderNumber}
• Assigned Worker: {request.AssignedWorkerEmail}

The assigned worker will contact you to arrange access. Please ensure someone is available to provide access during the scheduled time.

Thank you for your patience.

Best regards,
Rental Repairs Team";

        return new NotificationData
        {
            Subject = $"Work Scheduled: {request.Title}",
            Body = body,
            RecipientEmail = request.TenantEmail,
            Type = NotificationType.TenantRequestScheduled,
            Priority = request.UrgencyLevel == "Emergency" ? NotificationPriority.High : NotificationPriority.Normal
        };
    }

    private static NotificationData CreateSuperintendentScheduledNotification(Domain.Entities.TenantRequest request)
    {
        var body = $@"Dear {request.SuperintendentFullName},

Maintenance work has been scheduled for your property.

Scheduled Work Details:
• Property: {request.PropertyName}
• Unit: {request.TenantUnit}
• Request: {request.Title}
• Scheduled Date: {request.ScheduledDate?.ToString("yyyy-MM-dd") ?? "TBD"}
• Work Order: {request.WorkOrderNumber}
• Assigned Worker: {request.AssignedWorkerEmail}

The tenant and worker have been notified. The work is scheduled to proceed as planned.

Best regards,
Rental Repairs System";

        return new NotificationData
        {
            Subject = $"Work Scheduled: {request.PropertyName} - Unit {request.TenantUnit}",
            Body = body,
            RecipientEmail = request.SuperintendentEmail,
            Type = NotificationType.TenantRequestScheduled,
            Priority = NotificationPriority.Normal
        };
    }

    private static NotificationData CreateTenantCompletionNotification(Domain.Entities.TenantRequest request,
        bool successful, string? notes)
    {
        var status = successful ? "completed successfully" : "encountered some issues";
        var body = $@"Dear {request.TenantFullName},

Your maintenance request has been {status}.

Work Details:
• Request: {request.Title}
• Work Order: {request.WorkOrderNumber}
• Completion Status: {(successful ? "Successful" : "Issues Encountered")}
• Worker Notes: {notes ?? "No additional notes provided"}
• Completed Date: {DateTime.UtcNow:yyyy-MM-dd}

{(successful ?
    "Thank you for your patience throughout this process." :
    "We will follow up to resolve any remaining issues. Please contact us if you have concerns.")}

Best regards,
Rental Repairs Team";

        return new NotificationData
        {
            Subject = $"Work {(successful ? "Completed" : "Update")}: {request.Title}",
            Body = body,
            RecipientEmail = request.TenantEmail,
            Type = NotificationType.TenantRequestCompleted,
            Priority = successful ? NotificationPriority.Normal : NotificationPriority.High
        };
    }

    private static NotificationData CreateSuperintendentCompletionNotification(Domain.Entities.TenantRequest request,
        bool successful, string? notes)
    {
        var body = $@"Dear {request.SuperintendentFullName},

Maintenance work has been {(successful ? "completed successfully" : "completed with issues")}.

Work Summary:
• Property: {request.PropertyName}
• Unit: {request.TenantUnit}
• Request: {request.Title}
• Worker: {request.AssignedWorkerEmail}
• Completion Status: {(successful ? "Successful" : "Issues Encountered")}
• Worker Notes: {notes ?? "No additional notes provided"}
• Completed Date: {DateTime.UtcNow:yyyy-MM-dd}

{(successful ?
    "The tenant has been notified of the successful completion." :
    "Please review the issues noted and determine if follow-up action is required.")}

Best regards,
Rental Repairs System";

        return new NotificationData
        {
            Subject =
                $"Work {(successful ? "Completed" : "Issues")}: {request.PropertyName} - Unit {request.TenantUnit}",
            Body = body,
            RecipientEmail = request.SuperintendentEmail,
            Type = NotificationType.TenantRequestCompleted,
            Priority = successful ? NotificationPriority.Normal : NotificationPriority.High
        };
    }
}