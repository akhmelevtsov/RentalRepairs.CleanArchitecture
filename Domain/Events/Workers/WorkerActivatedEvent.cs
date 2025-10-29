using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when a worker is activated.
/// </summary>
public class WorkerActivatedEvent : BaseEvent
{
    public WorkerActivatedEvent(Worker worker)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
    }

    public Worker Worker { get; }
}
