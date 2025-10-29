using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when property information in a request is updated.
/// </summary>
public class TenantRequestPropertyInfoUpdatedEvent : BaseEvent
{
    public TenantRequestPropertyInfoUpdatedEvent(TenantRequest tenantRequest)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
    }

    public TenantRequest TenantRequest { get; }
}
