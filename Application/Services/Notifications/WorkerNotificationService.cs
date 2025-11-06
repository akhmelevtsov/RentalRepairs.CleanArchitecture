using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;

namespace RentalRepairs.Application.Services.Notifications;

/// <summary>
///     Handles worker-specific notifications
///     Single Responsibility: Worker communication only
/// </summary>
public class WorkerNotificationService : IWorkerNotificationService
{
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<WorkerNotificationService> _logger;
    private readonly IMediator _mediator;

    public WorkerNotificationService(
        IMediator mediator,
        IEmailNotificationService emailService,
        ILogger<WorkerNotificationService> logger)
    {
        _mediator = mediator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task NotifyAssignmentAsync(
        Guid requestId,
        string workerEmail,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default)
    {
        var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
        if (request == null)
        {
            _logger.LogWarning(
                "Cannot notify worker - request {RequestId} not found",
                requestId);
            return;
        }

        var subject = $"Work Assignment - {request.Title}";
        var message = BuildAssignmentMessage(request, scheduledDate);

        await _emailService.SendEmailAsync(workerEmail, subject, message, cancellationToken);

        _logger.LogInformation(
            "Worker assignment notification sent for request {RequestId} to {WorkerEmail} scheduled for {ScheduledDate}",
            requestId, workerEmail, scheduledDate);
    }

    public async Task NotifyStatusChangeAsync(
        string workerEmail,
        bool isActivated,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var status = isActivated ? "Activated" : "Deactivated";
        var subject = $"Worker Status Update - {status}";
        var message = BuildStatusChangeMessage(isActivated, status, reason);

        await _emailService.SendEmailAsync(workerEmail, subject, message, cancellationToken);

        _logger.LogInformation(
            "Worker status change notification sent to {WorkerEmail}: {Status}, Reason: {Reason}",
            workerEmail, status, reason ?? "None");
    }

    private static string BuildAssignmentMessage(dynamic request, DateTime scheduledDate)
    {
        return $"Dear Worker,\n\n" +
               $"You have been assigned to a maintenance request:\n\n" +
               $"Request: {request.Title}\n" +
               $"Description: {request.Description}\n" +
               $"Scheduled Date: {scheduledDate:yyyy-MM-dd HH:mm}\n" +
               $"Urgency Level: {request.UrgencyLevel}\n" +
               $"Work Order: {request.WorkOrderNumber}\n\n" +
               $"Please contact the tenant to arrange access.\n\n" +
               $"Thank you.";
    }

    private static string BuildStatusChangeMessage(bool isActivated, string status, string? reason)
    {
        var message = $"Dear Worker,\n\n" +
                      $"Your worker status has been updated to: {status}\n\n";

        if (!string.IsNullOrEmpty(reason))
            message += $"Reason: {reason}\n\n";

        if (isActivated)
            message += "You can now receive new work assignments.\n\n";
        else
            message += "You will no longer receive new work assignments. " +
                       "Any existing assignments will need to be completed or reassigned.\n\n";

        message += "If you have any questions, please contact your supervisor.\n\nThank you.";

        return message;
    }
}