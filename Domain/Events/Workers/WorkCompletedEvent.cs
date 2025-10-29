using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when work is completed by a worker.
/// </summary>
public class WorkCompletedEvent : BaseEvent
{
    public WorkCompletedEvent(Worker worker, WorkAssignment assignment, bool successful, string? notes)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
        Successful = successful;
        Notes = notes;
    }

    public Worker Worker { get; }
    public WorkAssignment Assignment { get; }
    public bool Successful { get; }
    public string? Notes { get; }
}
