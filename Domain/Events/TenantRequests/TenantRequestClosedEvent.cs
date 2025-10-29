using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when a tenant request is closed.
/// </summary>
public class TenantRequestClosedEvent : BaseEvent
{
    public TenantRequestClosedEvent(TenantRequest tenantRequest, string closureNotes)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
        ClosureNotes = closureNotes ?? string.Empty;
    }

    public TenantRequest TenantRequest { get; }
    public string ClosureNotes { get; }
}
