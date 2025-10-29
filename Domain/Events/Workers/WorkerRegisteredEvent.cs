using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when a new worker is registered in the system.
/// </summary>
public class WorkerRegisteredEvent : BaseEvent
{
    public WorkerRegisteredEvent(Worker worker)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
    }

    public Worker Worker { get; }
}
