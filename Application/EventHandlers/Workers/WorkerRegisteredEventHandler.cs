using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application.EventHandlers.Workers;

/// <summary>
/// Handles WorkerRegisteredEvent to process new worker registrations
/// </summary>
public class WorkerRegisteredEventHandler : INotificationHandler<WorkerRegisteredEvent>
{
    private readonly ILogger<WorkerRegisteredEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;

    public WorkerRegisteredEventHandler(
        ILogger<WorkerRegisteredEventHandler> logger,
        INotifyPartiesService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(WorkerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;

        _logger.LogInformation("Processing WorkerRegisteredEvent for worker {WorkerEmail} with specialization {Specialization}",
            worker.ContactInfo.EmailAddress,
            worker.Specialization ?? "Not specified");

        try
        {
            // Send welcome notification to new worker
            await _notificationService.NotifyWorkerOfRegistrationAsync(worker, cancellationToken);

            // Notify system administrators of new worker registration
            await _notificationService.NotifyAdministratorsOfNewWorkerAsync(worker, cancellationToken);

            _logger.LogInformation("Successfully processed WorkerRegisteredEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WorkerRegisteredEvent for worker {WorkerEmail}",
                worker.ContactInfo.EmailAddress);
            throw;
        }
    }
}
