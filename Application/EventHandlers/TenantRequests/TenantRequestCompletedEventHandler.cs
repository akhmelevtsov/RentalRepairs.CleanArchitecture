using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.TenantRequests;

/// <summary>
/// Handles TenantRequestCompletedEvent to process successful work completion
/// </summary>
public class TenantRequestCompletedEventHandler : INotificationHandler<TenantRequestCompletedEvent>
{
    private readonly ILogger<TenantRequestCompletedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantRequestCompletedEventHandler(
        ILogger<TenantRequestCompletedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantRequestCompletedEvent notification, CancellationToken cancellationToken)
    {
        var request = notification.TenantRequest;
        var notes = notification.CompletionNotes; // ? Fix: Use correct property name

        _logger.LogInformation("Processing TenantRequestCompletedEvent for request {RequestCode} with notes: {Notes}",
            request.Code,
            notes);

        try
        {
            // Determine if work was successful based on request status
            var successful = request.Status == Domain.Enums.TenantRequestStatus.Done;

            // Notify tenant of work completion
            await _notificationService.NotifyTenantOfWorkCompletionAsync(request, successful, notes, cancellationToken);

            // Notify superintendent of completion for review
            await _notificationService.NotifySuperintendentOfWorkCompletionAsync(request, successful, notes,
                cancellationToken);

            _logger.LogInformation("Successfully processed TenantRequestCompletedEvent for request {RequestCode}",
                request.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantRequestCompletedEvent for request {RequestCode}",
                request.Code);
            throw;
        }
    }
}