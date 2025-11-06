using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using RentalRepairs.Domain.Repositories;

namespace RentalRepairs.Application.Services.Notifications;

/// <summary>
/// Handles superintendent-specific notifications
/// Single Responsibility: Superintendent communication only
/// </summary>
public class SuperintendentNotificationService : ISuperintendentNotificationService
{
    private readonly IMediator _mediator;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<SuperintendentNotificationService> _logger;

    public SuperintendentNotificationService(
        IMediator mediator,
        IPropertyRepository propertyRepository,
        IEmailNotificationService emailService,
        ILogger<SuperintendentNotificationService> logger)
    {
        _mediator = mediator;
        _propertyRepository = propertyRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task NotifyRequestEventAsync(
        Guid requestId,
        string eventType,
        string? additionalDetails = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
        if (request == null)
        {
            _logger.LogWarning(
                "Cannot notify superintendent - request {RequestId} not found",
                requestId);
            return;
        }

        var property = await _propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property == null)
        {
            _logger.LogWarning(
                "Cannot notify superintendent - property {PropertyId} not found for request {RequestId}",
                request.PropertyId, requestId);
            return;
        }

        var superintendentEmail = property.Superintendent.EmailAddress;
        var subject = $"Request Event: {eventType} - {request.Title}";
        var message = BuildRequestEventMessage(request, eventType, additionalDetails);

        await _emailService.SendEmailAsync(superintendentEmail, subject, message, cancellationToken);

        _logger.LogInformation(
            "Superintendent notification sent for request {RequestId} event {EventType} to {Email}",
            requestId, eventType, superintendentEmail);
    }

    public async Task SendEmergencyNotificationAsync(
        Guid requestId,
        string urgencyLevel,
        CancellationToken cancellationToken = default)
    {
        var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
        if (request == null)
        {
            _logger.LogWarning(
                "Cannot send emergency notification - request {RequestId} not found",
                requestId);
            return;
        }

        var property = await _propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property == null)
        {
            _logger.LogWarning(
                "Cannot send emergency notification - property {PropertyId} not found for request {RequestId}",
                request.PropertyId, requestId);
            return;
        }

        var superintendentEmail = property.Superintendent.EmailAddress;
        var subject = $"EMERGENCY REQUEST - {urgencyLevel} - {request.Title}";
        var message = BuildEmergencyMessage(request, urgencyLevel);

        await _emailService.SendEmailAsync(superintendentEmail, subject, message, cancellationToken);

        _logger.LogWarning(
            "Emergency notification sent for request {RequestId} with urgency {UrgencyLevel} to {Email}",
            requestId, urgencyLevel, superintendentEmail);
    }

    public async Task NotifyOverdueRequestsAsync(
        List<Guid> overdueRequestIds,
        CancellationToken cancellationToken = default)
    {
        if (!overdueRequestIds.Any())
        {
            _logger.LogDebug("No overdue requests to notify");
            return;
        }

        var subject = $"Overdue Requests Report - {overdueRequestIds.Count} requests";
        var message = await BuildOverdueRequestsMessageAsync(overdueRequestIds, cancellationToken);

        // Get superintendent email from first request's property
        var firstRequest = await _mediator.Send(
            new GetTenantRequestByIdQuery(overdueRequestIds.First()),
            cancellationToken);

        if (firstRequest != null)
        {
            var property = await _propertyRepository.GetByIdAsync(firstRequest.PropertyId, cancellationToken);
            if (property != null)
            {
                var superintendentEmail = property.Superintendent.EmailAddress;
                await _emailService.SendEmailAsync(superintendentEmail, subject, message, cancellationToken);

                _logger.LogInformation(
                    "Overdue requests notification sent for {Count} requests to {Email} for property {PropertyId}",
                    overdueRequestIds.Count, superintendentEmail, property.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Cannot send overdue notification - property {PropertyId} not found",
                    firstRequest.PropertyId);
            }
        }
        else
        {
            _logger.LogWarning(
                "Cannot send overdue notification - first request {RequestId} not found",
                overdueRequestIds.First());
        }
    }

    private static string BuildRequestEventMessage(
        dynamic request,
        string eventType,
        string? additionalDetails)
    {
        var message = $"Dear Property Superintendent,\n\n" +
                      $"Event: {eventType}\n" +
                      $"Request: {request.Title}\n" +
                      $"Status: {request.Status}\n" +
                      $"Urgency: {request.UrgencyLevel}\n";

        if (!string.IsNullOrEmpty(additionalDetails))
            message += $"\nDetails: {additionalDetails}";

        message += "\n\nPlease review and take appropriate action.";

        return message;
    }

    private static string BuildEmergencyMessage(dynamic request, string urgencyLevel)
    {
        return $"EMERGENCY MAINTENANCE REQUEST ⚠️\n\n" +
               $"Urgency Level: {urgencyLevel}\n" +
               $"Request: {request.Title}\n" +
               $"Description: {request.Description}\n" +
               $"Tenant Email: {request.TenantEmail}\n" +
               $"Submitted: {request.CreatedDate:yyyy-MM-dd HH:mm}\n\n" +
               $"IMMEDIATE ATTENTION REQUIRED ⚠️\n\n" +
               $"Please assign a worker immediately.";
    }

    private async Task<string> BuildOverdueRequestsMessageAsync(
        List<Guid> overdueRequestIds,
        CancellationToken cancellationToken)
    {
        var message = $"Dear Superintendent,\n\n" +
                      $"The following {overdueRequestIds.Count} requests are overdue and require attention:\n\n";

        foreach (var requestId in overdueRequestIds.Take(10))
            try
            {
                var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
                if (request != null)
                    message +=
                        $"- {request.Title} (Created: {request.CreatedDate:yyyy-MM-dd}, Status: {request.Status})\n";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error getting details for overdue request {RequestId}",
                    requestId);
            }

        if (overdueRequestIds.Count > 10) message += $"... and {overdueRequestIds.Count - 10} more requests.\n";

        message += "\nPlease review and take appropriate action.\n\nThank you.";

        return message;
    }
}