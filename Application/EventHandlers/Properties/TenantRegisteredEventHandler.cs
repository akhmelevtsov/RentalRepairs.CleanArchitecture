using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Properties;

/// <summary>
/// Handles TenantRegisteredEvent to process new tenant registrations
/// </summary>
public class TenantRegisteredEventHandler : INotificationHandler<TenantRegisteredEvent>
{
    private readonly ILogger<TenantRegisteredEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantRegisteredEventHandler(
        ILogger<TenantRegisteredEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var tenant = notification.Tenant;
        var property = notification.Property;

        _logger.LogInformation("Processing TenantRegisteredEvent for tenant {TenantName} in unit {UnitNumber} at property {PropertyCode}",
            tenant.ContactInfo.GetFullName(),
            tenant.UnitNumber,
            tenant.PropertyCode);

        try
        {
            // Send welcome package to new tenant
            await _notificationService.NotifyTenantOfRegistrationAsync(tenant, property, cancellationToken);

            // Notify superintendent of new tenant
            await _notificationService.NotifySuperintendentOfNewTenantAsync(tenant, property, cancellationToken);

            // Update occupancy tracking
            await _notificationService.UpdatePropertyOccupancyAsync(property, cancellationToken);

            _logger.LogInformation("Successfully processed TenantRegisteredEvent for tenant {TenantName}",
                tenant.ContactInfo.GetFullName());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantRegisteredEvent for tenant {TenantName}",
                tenant.ContactInfo.GetFullName());
            throw;
        }
    }
}
