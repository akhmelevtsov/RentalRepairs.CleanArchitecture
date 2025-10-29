using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Workers;

/// <summary>
/// Handles WorkerActivatedEvent and WorkerDeactivatedEvent to manage worker status
/// </summary>
public class WorkerStatusChangedEventHandler : 
    INotificationHandler<WorkerActivatedEvent>,
    INotificationHandler<WorkerDeactivatedEvent>
{
    private readonly ILogger<WorkerStatusChangedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public WorkerStatusChangedEventHandler(
        ILogger<WorkerStatusChangedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(WorkerActivatedEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;

        _logger.LogInformation("Processing WorkerActivatedEvent for worker {WorkerEmail}",
            worker.ContactInfo.EmailAddress);

        try
        {
            // Notify worker of reactivation
            await _notificationService.NotifyWorkerOfStatusChangeAsync(worker, true, cancellationToken);

            // Update workforce capacity planning
            await _notificationService.UpdateWorkforceCapacityAsync(worker, true, cancellationToken);

            _logger.LogInformation("Successfully processed WorkerActivatedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WorkerActivatedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
            throw;
        }
    }

    public async Task Handle(WorkerDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;
        var reason = notification.Reason;

        _logger.LogInformation("Processing WorkerDeactivatedEvent for worker {WorkerEmail} with reason: {Reason}",
            worker.ContactInfo.EmailAddress,
            reason);

        try
        {
            // Notify worker of deactivation
            await _notificationService.NotifyWorkerOfDeactivationAsync(worker, reason, cancellationToken);

            // Handle reassignment of pending work orders
            await _notificationService.HandleWorkerDeactivationReassignmentsAsync(worker, cancellationToken);

            // Update workforce capacity planning
            await _notificationService.UpdateWorkforceCapacityAsync(worker, false, cancellationToken);

            _logger.LogInformation("Successfully processed WorkerDeactivatedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WorkerDeactivatedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
            throw;
        }
    }
}