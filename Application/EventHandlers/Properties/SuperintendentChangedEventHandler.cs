using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Properties;

/// <summary>
/// Handles SuperintendentChangedEvent to process superintendent updates
/// </summary>
public class SuperintendentChangedEventHandler : INotificationHandler<SuperintendentChangedEvent>
{
    private readonly ILogger<SuperintendentChangedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public SuperintendentChangedEventHandler(
        ILogger<SuperintendentChangedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(SuperintendentChangedEvent notification, CancellationToken cancellationToken)
    {
        var property = notification.Property;
        var oldSuperintendent = notification.OldSuperintendent;
        var newSuperintendent = notification.NewSuperintendent;

        _logger.LogInformation(
            "Processing SuperintendentChangedEvent for property {PropertyCode}: {OldSuper} -> {NewSuper}",
            property.Code,
            oldSuperintendent.GetFullName(),
            newSuperintendent.GetFullName());

        try
        {
            // Notify all tenants of superintendent change
            await _notificationService.NotifyTenantsOfSuperintendentChangeAsync(
                property, oldSuperintendent, newSuperintendent, cancellationToken);

            // Welcome new superintendent
            await _notificationService.NotifyNewSuperintendentOfAssignmentAsync(
                property, newSuperintendent, cancellationToken);

            // Archive old superintendent's access
            await _notificationService.ArchiveOldSuperintendentAccessAsync(
                property, oldSuperintendent, cancellationToken);

            // Transfer pending requests and responsibilities
            await _notificationService.TransferSuperintendentResponsibilitiesAsync(
                property, oldSuperintendent, newSuperintendent, cancellationToken);

            _logger.LogInformation("Successfully processed SuperintendentChangedEvent for property {PropertyCode}",
                property.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SuperintendentChangedEvent for property {PropertyCode}",
                property.Code);
            throw;
        }
    }
}