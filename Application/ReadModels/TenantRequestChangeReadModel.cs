namespace RentalRepairs.Application.ReadModels;

/// <summary>
/// Read model for tenant request change history.
/// Tracks status changes and workflow progression for audit and display purposes.
/// </summary>
public class TenantRequestChangeReadModel
{
    public Guid Id { get; set; }
    public Domain.Enums.TenantRequestStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ChangeDate { get; set; }
    public int WorkOrderSequence { get; set; }
    
    public string StatusDisplayName => Status switch
    {
        Domain.Enums.TenantRequestStatus.Draft => "Draft",
        Domain.Enums.TenantRequestStatus.Submitted => "Submitted",
        Domain.Enums.TenantRequestStatus.Scheduled => "Scheduled",
        Domain.Enums.TenantRequestStatus.Done => "Completed",
        Domain.Enums.TenantRequestStatus.Failed => "Failed",
        Domain.Enums.TenantRequestStatus.Declined => "Declined",
        Domain.Enums.TenantRequestStatus.Closed => "Closed",
        _ => Status.ToString()
    };
}