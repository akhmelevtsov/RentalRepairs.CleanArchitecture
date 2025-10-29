using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.TenantRequests;

/// <summary>
/// Handles TenantRequestCreatedEvent to notify relevant parties and perform related actions
/// </summary>
public class TenantRequestCreatedEventHandler : INotificationHandler<TenantRequestCreatedEvent>
{
    private readonly ILogger<TenantRequestCreatedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantRequestCreatedEventHandler(
        ILogger<TenantRequestCreatedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantRequestCreatedEvent notification, CancellationToken cancellationToken)
    {
        var request = notification.TenantRequest;
        
        _logger.LogInformation("Processing TenantRequestCreatedEvent for request {RequestCode} by tenant {TenantName}", 
            request.Code, 
            request.TenantFullName);

        try
        {
            // Send initial acknowledgment to tenant
            await _notificationService.NotifyTenantOfRequestCreationAsync(request, cancellationToken);

            // Notify superintendent of new request pending review
            await _notificationService.NotifySuperintendentOfNewRequestAsync(request, cancellationToken);

            _logger.LogInformation("Successfully processed TenantRequestCreatedEvent for request {RequestCode}", 
                request.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantRequestCreatedEvent for request {RequestCode}", 
                request.Code);
            throw;
        }
    }
}
