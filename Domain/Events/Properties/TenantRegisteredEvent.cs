using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.Properties;

/// <summary>
/// Domain event raised when a new tenant is registered to a property.
/// </summary>
public class TenantRegisteredEvent : BaseEvent
{
    public TenantRegisteredEvent(Tenant tenant, Property property)
    {
        Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        Property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public Tenant Tenant { get; }
    public Property Property { get; }
}
