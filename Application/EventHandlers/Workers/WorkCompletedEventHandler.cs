using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Workers;

/// <summary>
/// Handles WorkCompletedEvent to process work completion from worker perspective
/// </summary>
public class WorkCompletedEventHandler : INotificationHandler<WorkCompletedEvent>
{
    private readonly ILogger<WorkCompletedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public WorkCompletedEventHandler(
        ILogger<WorkCompletedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(WorkCompletedEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;
        var assignment = notification.Assignment;
        var successful = notification.Successful;
        var notes = notification.Notes;

        _logger.LogInformation("Processing WorkCompletedEvent for worker {WorkerEmail} on work order {WorkOrderNumber}: {Status}",
            worker.ContactInfo.EmailAddress,
            assignment.WorkOrderNumber,
            successful ? "Successful" : "Failed");

        try
        {
            // Update worker's schedule and availability
            await _notificationService.UpdateWorkerAvailabilityAsync(worker, assignment, cancellationToken);

            // Track worker performance metrics
            await _notificationService.RecordWorkerPerformanceAsync(worker, assignment, successful, cancellationToken);

            _logger.LogInformation("Successfully processed WorkCompletedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WorkCompletedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
            throw;
        }
    }
}
