using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;

namespace RentalRepairs.Application.Services.Notifications;

/// <summary>
/// Handles tenant-specific notifications
/// Single Responsibility: Tenant communication only
/// </summary>
public class TenantNotificationService : ITenantNotificationService
{
    private readonly IMediator _mediator;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<TenantNotificationService> _logger;

    public TenantNotificationService(
        IMediator mediator,
        IEmailNotificationService emailService,
        ILogger<TenantNotificationService> logger)
    {
        _mediator = mediator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task NotifyRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        string? additionalMessage = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
        if (request == null)
        {
            _logger.LogWarning(
                "Cannot notify tenant - request {RequestId} not found",
                requestId);
            return;
        }

        var subject = $"Request Status Update - {request.Title}";
        var message = BuildStatusChangeMessage(request, newStatus, additionalMessage);

        await _emailService.SendEmailAsync(request.TenantEmail, subject, message, cancellationToken);

        _logger.LogInformation(
            "Tenant notification sent for request {RequestId} status change to {NewStatus} to {TenantEmail}",
            requestId, newStatus, request.TenantEmail);
    }

    private static string BuildStatusChangeMessage(
        dynamic request,
        string newStatus,
        string? additionalMessage)
    {
        var message = $"Dear Tenant,\n\n" +
                      $"Your maintenance request status has been updated to: {newStatus}\n\n" +
                      $"Request: {request.Title}\n" +
                      $"Description: {request.Description}\n";

        if (!string.IsNullOrEmpty(additionalMessage))
            message += $"\nAdditional Information: {additionalMessage}";

        message += "\n\nThank you for your patience.";

        return message;
    }
}