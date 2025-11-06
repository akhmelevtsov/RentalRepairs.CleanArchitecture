using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Properties;

/// <summary>
/// Handles TenantContactInfoChangedEvent to process tenant contact updates
/// </summary>
public class TenantContactInfoChangedEventHandler : INotificationHandler<TenantContactInfoChangedEvent>
{
    private readonly ILogger<TenantContactInfoChangedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantContactInfoChangedEventHandler(
        ILogger<TenantContactInfoChangedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantContactInfoChangedEvent notification, CancellationToken cancellationToken)
    {
        var tenant = notification.Tenant;
        var oldContactInfo = notification.OldContactInfo;
        var newContactInfo = notification.NewContactInfo;

        _logger.LogInformation(
            "Processing TenantContactInfoChangedEvent for tenant in unit {UnitNumber}: {OldContact} -> {NewContact}",
            tenant.UnitNumber,
            oldContactInfo.GetFullName(),
            newContactInfo.GetFullName());

        try
        {
            // Confirm contact information update with tenant
            await _notificationService.NotifyTenantOfContactInfoChangeAsync(
                tenant, oldContactInfo, newContactInfo, cancellationToken);

            // Notify superintendent of tenant contact changes
            await _notificationService.NotifySuperintendentOfTenantContactChangeAsync(
                tenant, oldContactInfo, newContactInfo, cancellationToken);

            // Update any active service requests with new contact information
            await _notificationService.UpdateActiveRequestsWithNewContactInfoAsync(
                tenant, newContactInfo, cancellationToken);

            _logger.LogInformation("Successfully processed TenantContactInfoChangedEvent for tenant {TenantName}",
                newContactInfo.GetFullName());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantContactInfoChangedEvent for tenant {TenantName}",
                newContactInfo.GetFullName());
            throw;
        }
    }
}