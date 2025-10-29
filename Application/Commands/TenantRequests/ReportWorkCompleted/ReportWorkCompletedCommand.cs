using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.ReportWorkCompleted;

/// <summary>
/// Command to report work completion status for a tenant request.
/// Used by workers or supervisors to indicate whether work was completed successfully.
/// </summary>
public class ReportWorkCompletedCommand : ICommand
{
    public Guid TenantRequestId { get; set; }
    public bool CompletedSuccessfully { get; set; }
    public string CompletionNotes { get; set; } = default!;
}