using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.ScheduleServiceWork;

/// <summary>
/// Command to schedule service work for a tenant request, assigning a worker and setting a date.
/// Used in the work assignment and scheduling workflow.
/// </summary>
public class ScheduleServiceWorkCommand : ICommand
{
    public Guid TenantRequestId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string WorkerEmail { get; set; } = default!;
    public string WorkOrderNumber { get; set; } = default!;
}