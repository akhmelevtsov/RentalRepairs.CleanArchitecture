namespace RentalRepairs.Application.ReadModels;

/// <summary>
/// Read model for detailed tenant request view - complete information without circular dependencies.
/// Optimized for detail pages, comprehensive displays, and full request information views.
/// </summary>
public class TenantRequestDetailsReadModel
{
    public string Id { get; set; } = string.Empty; // Changed to string for display purposes
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public Domain.Enums.TenantRequestStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? ClosureNotes { get; set; }
    
    // Tenant information (denormalized)
    public Guid TenantId { get; set; }
    public string TenantFullName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string TenantUnit { get; set; } = string.Empty;
    
    // Property information (denormalized)
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyPhone { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string PropertyNoReplyEmail { get; set; } = string.Empty;
    
    // Superintendent information (denormalized)
    public string SuperintendentFullName { get; set; } = string.Empty;
    public string SuperintendentEmail { get; set; } = string.Empty;
    
    // Worker assignment information
    public string? AssignedWorkerEmail { get; set; }
    public string? AssignedWorkerName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? CompletionNotes { get; set; }
    public bool? WorkCompletedSuccessfully { get; set; }
    
    // Request changes history
    public List<TenantRequestChangeReadModel> RequestChanges { get; set; } = new();
    
    // Derived properties
    public bool IsEmergency => UrgencyLevel?.Equals("Emergency", StringComparison.OrdinalIgnoreCase) == true 
                              || UrgencyLevel?.Equals("High", StringComparison.OrdinalIgnoreCase) == true;
    
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

    // ? UI logic properties
    public bool CanBeScheduled => Status == Domain.Enums.TenantRequestStatus.Submitted;
    public bool CanBeCompleted => Status == Domain.Enums.TenantRequestStatus.Scheduled;
    public bool CanBeClosed => Status == Domain.Enums.TenantRequestStatus.Done || Status == Domain.Enums.TenantRequestStatus.Declined;
}