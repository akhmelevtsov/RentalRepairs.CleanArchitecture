using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Properties;

/// <summary>
/// Handles UnitAddedEvent and UnitRemovedEvent to manage property unit changes
/// </summary>
public class PropertyUnitChangedEventHandler : 
    INotificationHandler<UnitAddedEvent>,
    INotificationHandler<UnitRemovedEvent>
{
    private readonly ILogger<PropertyUnitChangedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public PropertyUnitChangedEventHandler(
        ILogger<PropertyUnitChangedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(UnitAddedEvent notification, CancellationToken cancellationToken)
    {
        var property = notification.Property;
        var unitNumber = notification.UnitNumber;

        _logger.LogInformation("Processing UnitAddedEvent for property {PropertyCode}: Unit {UnitNumber}",
            property.Code,
            unitNumber);

        try
        {
            // Notify superintendent of new unit availability
            await _notificationService.NotifySuperintendentOfUnitAddedAsync(property, unitNumber, cancellationToken);

            // Update property capacity tracking
            await _notificationService.UpdatePropertyCapacityAsync(property, cancellationToken);

            _logger.LogInformation("Successfully processed UnitAddedEvent for property {PropertyCode}",
                property.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UnitAddedEvent for property {PropertyCode}",
                property.Code);
            throw;
        }
    }

    public async Task Handle(UnitRemovedEvent notification, CancellationToken cancellationToken)
    {
        var property = notification.Property;
        var unitNumber = notification.UnitNumber;

        _logger.LogInformation("Processing UnitRemovedEvent for property {PropertyCode}: Unit {UnitNumber}",
            property.Code,
            unitNumber);

        try
        {
            // Notify superintendent of unit removal
            await _notificationService.NotifySuperintendentOfUnitRemovedAsync(property, unitNumber, cancellationToken);

            // Update property capacity tracking
            await _notificationService.UpdatePropertyCapacityAsync(property, cancellationToken);

            _logger.LogInformation("Successfully processed UnitRemovedEvent for property {PropertyCode}",
                property.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UnitRemovedEvent for property {PropertyCode}",
                property.Code);
            throw;
        }
    }
}