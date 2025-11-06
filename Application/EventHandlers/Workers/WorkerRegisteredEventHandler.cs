using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Application.EventHandlers.Workers;

/// <summary>
/// Handles WorkerRegisteredEvent to process new worker registrations.
/// Phase 2: Now uses SpecializationDeterminationService for enum display names.
/// </summary>
public class WorkerRegisteredEventHandler : INotificationHandler<WorkerRegisteredEvent>
{
    private readonly ILogger<WorkerRegisteredEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;
    private readonly SpecializationDeterminationService _specializationService;

    public WorkerRegisteredEventHandler(
        ILogger<WorkerRegisteredEventHandler> logger,
        INotifyPartiesService notificationService,
        SpecializationDeterminationService specializationService)
    {
        _logger = logger;
        _notificationService = notificationService;
        _specializationService = specializationService;
    }

    public async Task Handle(WorkerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;
        var specializationName = _specializationService.GetDisplayName(worker.Specialization);

        _logger.LogInformation(
            "Processing WorkerRegisteredEvent for worker {WorkerEmail} with specialization {Specialization}",
            worker.ContactInfo.EmailAddress,
            specializationName);

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