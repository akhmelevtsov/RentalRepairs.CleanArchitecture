using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.Properties;

/// <summary>
/// Domain event raised when a new property is registered in the system.
/// </summary>
public class PropertyRegisteredEvent : BaseEvent
{
    public PropertyRegisteredEvent(Property property)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public Property Property { get; }
}
