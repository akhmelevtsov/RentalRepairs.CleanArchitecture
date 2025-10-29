using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.CloseRequest;

/// <summary>
/// Command to close a tenant request with closure notes.
/// Used to finalize completed or declined requests and archive them.
/// </summary>
public class CloseRequestCommand : ICommand
{
    public Guid TenantRequestId { get; set; }
    public string ClosureNotes { get; set; } = default!;
}