using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Application.EventHandlers.Workers;

/// <summary>
/// Handles WorkerSpecializationChangedEvent to track specialization updates.
/// Phase 2: Now uses SpecializationDeterminationService for enum display names.
/// </summary>
public class WorkerSpecializationChangedEventHandler : INotificationHandler<WorkerSpecializationChangedEvent>
{
    private readonly ILogger<WorkerSpecializationChangedEventHandler> _logger;
    private readonly INotifyPartiesService _notificationService;
    private readonly SpecializationDeterminationService _specializationService;

    public WorkerSpecializationChangedEventHandler(
        ILogger<WorkerSpecializationChangedEventHandler> logger,
        INotifyPartiesService notificationService,
        SpecializationDeterminationService specializationService)
    {
        _logger = logger;
        _notificationService = notificationService;
        _specializationService = specializationService;
    }

    public async Task Handle(WorkerSpecializationChangedEvent notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;
        var oldSpecialization = notification.OldSpecialization;
        var newSpecialization = notification.NewSpecialization;

        var oldSpecName = _specializationService.GetDisplayName(oldSpecialization);
        var newSpecName = _specializationService.GetDisplayName(newSpecialization);

        _logger.LogInformation(
            "Processing WorkerSpecializationChangedEvent for worker {WorkerEmail}: {OldSpecialization} -> {NewSpecialization}",
            worker.ContactInfo.EmailAddress,
            oldSpecName,
            newSpecName);

        try
        {
            // Notify worker of specialization update confirmation
            await _notificationService.NotifyWorkerOfSpecializationChangeAsync(
                worker, oldSpecName, newSpecName, cancellationToken);

            // Notify administrators of capability changes for workforce planning
            await _notificationService.NotifyAdministratorsOfWorkerSpecializationChangeAsync(
                worker, oldSpecName, newSpecName, cancellationToken);

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