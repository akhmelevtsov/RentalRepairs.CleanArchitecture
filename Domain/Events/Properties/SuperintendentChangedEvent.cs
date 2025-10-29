using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Events.Properties;

/// <summary>
/// Domain event raised when a property's superintendent is changed.
/// </summary>
public class SuperintendentChangedEvent : BaseEvent
{
    public SuperintendentChangedEvent(Property property, PersonContactInfo oldSuperintendent, PersonContactInfo newSuperintendent)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        OldSuperintendent = oldSuperintendent ?? throw new ArgumentNullException(nameof(oldSuperintendent));
        NewSuperintendent = newSuperintendent ?? throw new ArgumentNullException(nameof(newSuperintendent));
    }

    public Property Property { get; }
    public PersonContactInfo OldSuperintendent { get; }
    public PersonContactInfo NewSuperintendent { get; }
}
