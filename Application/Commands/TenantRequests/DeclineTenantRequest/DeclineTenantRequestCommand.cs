using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.DeclineTenantRequest;

/// <summary>
/// Command to decline a tenant request with a specific reason.
/// Used when a request cannot be fulfilled or is deemed inappropriate.
/// </summary>
public class DeclineTenantRequestCommand : ICommand
{
    public Guid TenantRequestId { get; set; }
    public string Reason { get; set; } = default!;
}