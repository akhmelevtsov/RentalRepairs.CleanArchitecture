using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.TenantRequests;

/// <summary>
/// Handles TenantRequestScheduledEvent to coordinate work assignments
/// </summary>
public class TenantRequestScheduledEventHandler : INotificationHandler<TenantRequestScheduledEvent>
{
    private readonly ILogger<TenantRequestScheduledEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public TenantRequestScheduledEventHandler(
        ILogger<TenantRequestScheduledEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TenantRequestScheduledEvent notification, CancellationToken cancellationToken)
    {
        var request = notification.TenantRequest;
        var scheduleInfo = notification.ScheduleInfo;

        _logger.LogInformation("Processing TenantRequestScheduledEvent for request {RequestCode} with worker {WorkerEmail} on {ServiceDate}", 
            request.Code,
            scheduleInfo.WorkerEmail,
            scheduleInfo.ServiceDate);

        try
        {
            // Notify tenant of scheduled work
            await _notificationService.NotifyTenantOfScheduledWorkAsync(request, scheduleInfo, cancellationToken);

            // Notify assigned worker of work assignment
            await _notificationService.NotifyWorkerOfWorkAssignmentAsync(request, scheduleInfo, cancellationToken);

            // Confirm scheduling with superintendent
            await _notificationService.NotifySuperintendentOfScheduledWorkAsync(request, scheduleInfo, cancellationToken);

            _logger.LogInformation("Successfully processed TenantRequestScheduledEvent for request {RequestCode}", 
                request.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TenantRequestScheduledEvent for request {RequestCode}", 
                request.Code);
            throw;
        }
    }
}
