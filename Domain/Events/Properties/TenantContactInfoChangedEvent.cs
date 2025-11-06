using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Events.Properties;

/// <summary>
/// Domain event raised when a tenant's contact information is changed.
/// </summary>
public class TenantContactInfoChangedEvent : BaseEvent
{
    public TenantContactInfoChangedEvent(Tenant tenant, PersonContactInfo oldContactInfo,
        PersonContactInfo newContactInfo)
    {
        Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        OldContactInfo = oldContactInfo ?? throw new ArgumentNullException(nameof(oldContactInfo));
        NewContactInfo = newContactInfo ?? throw new ArgumentNullException(nameof(newContactInfo));
    }

    public Tenant Tenant { get; }
    public PersonContactInfo OldContactInfo { get; }
    public PersonContactInfo NewContactInfo { get; }
}
