using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.TenantRequests;

/// <summary>
/// Handles TenantRequestDeclinedEvent to process request declinations
/// </summary>
public class TenantRequestDeclinedEventHandler : INotificationHandler<TenantRequestDeclinedEvent>
{
    private readonly ILogger<TenantRequestDeclinedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantRequestDeclinedEventHandler(
        ILogger<TenantRequestDeclinedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantRequestDeclinedEvent notification, CancellationToken cancellationToken)
    {
        var request = notification.TenantRequest;
        var reason = notification.Reason;

        _logger.LogInformation("Processing TenantRequestDeclinedEvent for request {RequestCode} with reason: {Reason}", 
            request.Code, 
            reason);

        try
        {
            // Notify tenant of request declination with reason
            await _notificationService.NotifyTenantOfRequestDeclinationAsync(request, reason, cancellationToken);

            _logger.LogInformation("Successfully processed TenantRequestDeclinedEvent for request {RequestCode}", 
                request.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantRequestDeclinedEvent for request {RequestCode}", 
                request.Code);
            throw;
        }
    }
}