using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when a tenant request is declined.
/// </summary>
public class TenantRequestDeclinedEvent : BaseEvent
{
    public TenantRequestDeclinedEvent(TenantRequest tenantRequest, string reason)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
        Reason = reason ?? string.Empty;
    }

    public TenantRequest TenantRequest { get; }
    public string Reason { get; }
}
