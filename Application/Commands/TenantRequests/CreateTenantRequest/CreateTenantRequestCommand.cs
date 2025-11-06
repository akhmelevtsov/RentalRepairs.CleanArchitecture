using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests;

/// <summary>
/// Command to create a new tenant request with complete tenant and property information.
/// Used in the initial request submission workflow.
/// </summary>
public class CreateTenantRequestCommand : ICommand<Guid>
{
    public Guid TenantId { get; set; }
    public Guid PropertyId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string UrgencyLevel { get; set; } = default!;
    public string TenantEmail { get; set; } = default!;

    // Additional properties for creating complete tenant request
    public string TenantFullName { get; set; } = default!;
    public string TenantUnit { get; set; } = default!;
    public string PropertyName { get; set; } = default!;
    public string PropertyPhone { get; set; } = default!;
    public string SuperintendentFullName { get; set; } = default!;
    public string SuperintendentEmail { get; set; } = default!;
    public string? PreferredContactTime { get; set; }
}