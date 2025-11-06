using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when a worker's specialization is changed.
/// Phase 2: Now uses WorkerSpecialization enum.
/// </summary>
public class WorkerSpecializationChangedEvent : BaseEvent
{
    public WorkerSpecializationChangedEvent(
        Worker worker,
        WorkerSpecialization oldSpecialization,
        WorkerSpecialization newSpecialization)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        OldSpecialization = oldSpecialization;
        NewSpecialization = newSpecialization;
    }

    public Worker Worker { get; }
    public WorkerSpecialization OldSpecialization { get; }
    public WorkerSpecialization NewSpecialization { get; }
}
