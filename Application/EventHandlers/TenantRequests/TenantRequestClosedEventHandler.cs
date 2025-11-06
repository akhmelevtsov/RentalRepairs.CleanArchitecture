using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.TenantRequests;

/// <summary>
/// Handles TenantRequestClosedEvent to finalize request lifecycle
/// </summary>
public class TenantRequestClosedEventHandler : INotificationHandler<TenantRequestClosedEvent>
{
    private readonly ILogger<TenantRequestClosedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantRequestClosedEventHandler(
        ILogger<TenantRequestClosedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantRequestClosedEvent notification, CancellationToken cancellationToken)
    {
        var request = notification.TenantRequest;
        var closureNotes = notification.ClosureNotes;

        _logger.LogInformation(
            "Processing TenantRequestClosedEvent for request {RequestCode} with closure notes: {ClosureNotes}",
            request.Code,
            closureNotes);

        try
        {
            // Send final notification to tenant
            await _notificationService.NotifyTenantOfRequestClosureAsync(request, closureNotes, cancellationToken);

            // Archive notification to superintendent
            await _notificationService.NotifySuperintendentOfRequestClosureAsync(request, closureNotes,
                cancellationToken);

            _logger.LogInformation("Successfully processed TenantRequestClosedEvent for request {RequestCode}",
                request.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantRequestClosedEvent for request {RequestCode}",
                request.Code);
            throw;
        }
    }
}