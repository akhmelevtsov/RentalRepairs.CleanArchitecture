using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when a tenant request is submitted for review.
/// </summary>
public class TenantRequestSubmittedEvent : BaseEvent
{
    public TenantRequestSubmittedEvent(TenantRequest tenantRequest)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
    }

    public TenantRequest TenantRequest { get; }
}
