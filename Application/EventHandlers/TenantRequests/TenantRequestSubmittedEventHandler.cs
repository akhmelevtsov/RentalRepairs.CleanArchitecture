using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.TenantRequests;

/// <summary>
/// Handles TenantRequestSubmittedEvent to process request submissions
/// </summary>
public class TenantRequestSubmittedEventHandler : INotificationHandler<TenantRequestSubmittedEvent>
{
    private readonly ILogger<TenantRequestSubmittedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantRequestSubmittedEventHandler(
        ILogger<TenantRequestSubmittedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantRequestSubmittedEvent notification, CancellationToken cancellationToken)
    {
        var request = notification.TenantRequest;

        _logger.LogInformation(
            "Processing TenantRequestSubmittedEvent for request {RequestCode} urgency {UrgencyLevel}",
            request.Code,
            request.UrgencyLevel);

        try
        {
            // Send submission confirmation to tenant
            await _notificationService.NotifyTenantOfRequestSubmissionAsync(request, cancellationToken);

            // Alert superintendent for review based on urgency
            if (request.UrgencyLevel.Equals("Emergency", StringComparison.OrdinalIgnoreCase) ||
                request.UrgencyLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
                await _notificationService.NotifySuperintendentOfUrgentRequestAsync(request, cancellationToken);
            else
                await _notificationService.NotifySuperintendentOfPendingRequestAsync(request, cancellationToken);

            _logger.LogInformation("Successfully processed TenantRequestSubmittedEvent for request {RequestCode}",
                request.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantRequestSubmittedEvent for request {RequestCode}",
                request.Code);
            throw;
        }
    }
}