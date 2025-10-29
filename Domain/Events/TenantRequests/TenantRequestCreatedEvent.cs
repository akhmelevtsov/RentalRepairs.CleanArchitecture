using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when a new tenant request is created.
/// </summary>
public class TenantRequestCreatedEvent : BaseEvent
{
    public TenantRequestCreatedEvent(TenantRequest tenantRequest)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
    }

    public TenantRequest TenantRequest { get; }
}
