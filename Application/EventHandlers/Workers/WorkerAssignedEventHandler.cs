using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Workers;

/// <summary>
/// Handles WorkerAssignedEvent to track work assignments
/// </summary>
public class WorkerAssignedEventHandler : INotificationHandler<WorkerAssignedEvent>
{
    private readonly ILogger<WorkerAssignedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public WorkerAssignedEventHandler(
        ILogger<WorkerAssignedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(WorkerAssignedEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;
        var assignment = notification.Assignment;

        _logger.LogInformation("Processing WorkerAssignedEvent for worker {WorkerEmail} assigned to work order {WorkOrderNumber} on {ScheduledDate}",
            worker.ContactInfo.EmailAddress,
            assignment.WorkOrderNumber,
            assignment.ScheduledDate);

        try
        {
            // Notification is typically handled by TenantRequestScheduledEventHandler
            // This handler focuses on worker-specific actions like calendar integration
            await _notificationService.UpdateWorkerScheduleAsync(worker, assignment, cancellationToken);

            _logger.LogInformation("Successfully processed WorkerAssignedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WorkerAssignedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
            throw;
        }
    }
}
