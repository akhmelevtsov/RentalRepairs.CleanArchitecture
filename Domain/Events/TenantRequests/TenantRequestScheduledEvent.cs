using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Events.TenantRequests;

/// <summary>
/// Domain event raised when a tenant request is scheduled for work.
/// </summary>
public class TenantRequestScheduledEvent : BaseEvent
{
    public TenantRequestScheduledEvent(TenantRequest tenantRequest, ServiceWorkScheduleInfo scheduleInfo)
    {
        TenantRequest = tenantRequest ?? throw new ArgumentNullException(nameof(tenantRequest));
        ScheduleInfo = scheduleInfo ?? throw new ArgumentNullException(nameof(scheduleInfo));
    }

    public TenantRequest TenantRequest { get; }
    public ServiceWorkScheduleInfo ScheduleInfo { get; }
}
