using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when tenant information in a request is updated.
/// </summary>
public class TenantRequestTenantInfoUpdatedEvent : BaseEvent
{
    public TenantRequestTenantInfoUpdatedEvent(TenantRequest tenantRequest)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
    }

    public TenantRequest TenantRequest { get; }
}
