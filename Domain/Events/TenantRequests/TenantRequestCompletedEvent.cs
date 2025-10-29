using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when work on a tenant request is completed.
/// </summary>
public class TenantRequestCompletedEvent : BaseEvent
{
    public TenantRequestCompletedEvent(TenantRequest tenantRequest, string completionNotes)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
        CompletionNotes = completionNotes ?? string.Empty;
    }

    public TenantRequest TenantRequest { get; }
    public string CompletionNotes { get; }
}
