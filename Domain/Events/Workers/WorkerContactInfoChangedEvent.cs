using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Events.Workers;

/// <summary>
/// Domain event raised when a worker's contact information is updated.
/// </summary>
public class WorkerContactInfoChangedEvent : BaseEvent
{
    public WorkerContactInfoChangedEvent(Worker worker, PersonContactInfo oldContactInfo, PersonContactInfo newContactInfo)
    {
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        OldContactInfo = oldContactInfo ?? throw new ArgumentNullException(nameof(oldContactInfo));
        NewContactInfo = newContactInfo ?? throw new ArgumentNullException(nameof(newContactInfo));
    }

    public Worker Worker { get; }
    public PersonContactInfo OldContactInfo { get; }
    public PersonContactInfo NewContactInfo { get; }
}
