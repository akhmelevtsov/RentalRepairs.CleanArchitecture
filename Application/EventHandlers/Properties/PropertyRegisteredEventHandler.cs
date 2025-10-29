using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Properties;

/// <summary>
/// Handles PropertyRegisteredEvent to process new property registrations
/// </summary>
public class PropertyRegisteredEventHandler : INotificationHandler<PropertyRegisteredEvent>
{
    private readonly ILogger<PropertyRegisteredEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public PropertyRegisteredEventHandler(
        ILogger<PropertyRegisteredEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(PropertyRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var property = notification.Property;

        _logger.LogInformation("Processing PropertyRegisteredEvent for property {PropertyCode} - {PropertyName}",
            property.Code,
            property.Name);

        try
        {
            // Welcome the superintendent to the system
            await _notificationService.NotifySuperintendentOfPropertyRegistrationAsync(property, cancellationToken);

            // Notify system administrators of new property
            await _notificationService.NotifyAdministratorsOfNewPropertyAsync(property, cancellationToken);

            // Initialize property-specific resources (maintenance schedules, etc.)
            await _notificationService.InitializePropertyResourcesAsync(property, cancellationToken);

            _logger.LogInformation("Successfully processed PropertyRegisteredEvent for property {PropertyCode}",
                property.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PropertyRegisteredEvent for property {PropertyCode}",
                property.Code);
            throw;
        }
    }
}
