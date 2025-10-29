using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Workers;

/// <summary>
/// Handles WorkerSpecializationChangedEvent to track specialization updates
/// </summary>
public class WorkerSpecializationChangedEventHandler : INotificationHandler<WorkerSpecializationChangedEvent>
{
    private readonly ILogger<WorkerSpecializationChangedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public WorkerSpecializationChangedEventHandler(
        ILogger<WorkerSpecializationChangedEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(WorkerSpecializationChangedEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;
        var oldSpecialization = notification.OldSpecialization;
        var newSpecialization = notification.NewSpecialization;

        _logger.LogInformation("Processing WorkerSpecializationChangedEvent for worker {WorkerEmail}: {OldSpecialization} -> {NewSpecialization}",
            worker.ContactInfo.EmailAddress,
            oldSpecialization ?? "None",
            newSpecialization);

        try
        {
            // Notify worker of specialization update confirmation
            await _notificationService.NotifyWorkerOfSpecializationChangeAsync(
                worker, oldSpecialization, newSpecialization, cancellationToken);

            // Notify administrators of capability changes for workforce planning
            await _notificationService.NotifyAdministratorsOfWorkerSpecializationChangeAsync(
                worker, oldSpecialization, newSpecialization, cancellationToken);

            _logger.LogInformation("Successfully processed WorkerSpecializationChangedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WorkerSpecializationChangedEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
            throw;
        }
    }
}
