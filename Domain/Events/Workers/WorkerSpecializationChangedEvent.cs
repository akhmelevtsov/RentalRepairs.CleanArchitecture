using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when a worker's specialization is changed.
/// </summary>
public class WorkerSpecializationChangedEvent : BaseEvent
{
    public WorkerSpecializationChangedEvent(Worker worker, string? oldSpecialization, string newSpecialization)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        OldSpecialization = oldSpecialization;
        NewSpecialization = newSpecialization ?? throw new ArgumentNullException(nameof(newSpecialization));
    }

    public Worker Worker { get; }
    public string? OldSpecialization { get; }
    public string NewSpecialization { get; }
}
