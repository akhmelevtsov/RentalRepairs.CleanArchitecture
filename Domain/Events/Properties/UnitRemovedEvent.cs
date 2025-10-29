using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.Properties;

/// <summary>
/// Domain event raised when a unit is removed from a property.
/// </summary>
public class UnitRemovedEvent : BaseEvent
{
    public UnitRemovedEvent(Property property, string unitNumber)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        UnitNumber = unitNumber ?? throw new ArgumentNullException(nameof(unitNumber));
    }

    public Property Property { get; }
    public string UnitNumber { get; }
}
