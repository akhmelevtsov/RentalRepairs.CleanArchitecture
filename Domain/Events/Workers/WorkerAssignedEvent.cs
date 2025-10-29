using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when a worker is assigned to work.
/// </summary>
public class WorkerAssignedEvent : BaseEvent
{
    public WorkerAssignedEvent(Worker worker, WorkAssignment assignment)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
    }

    public Worker Worker { get; }
    public WorkAssignment Assignment { get; }
}
