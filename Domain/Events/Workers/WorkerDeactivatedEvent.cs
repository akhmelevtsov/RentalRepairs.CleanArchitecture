using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when a worker is deactivated.
/// </summary>
public class WorkerDeactivatedEvent : BaseEvent
{
    public WorkerDeactivatedEvent(Worker worker, string reason)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        Reason = reason ?? string.Empty;
    }

    public Worker Worker { get; }
    public string Reason { get; }
}
