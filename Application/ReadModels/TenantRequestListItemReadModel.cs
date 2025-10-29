namespace RentalRepairs.Application.ReadModels;

/// <summary>
/// Read model for tenant request list items - flattened data without circular dependencies.
/// Optimized for list displays, grids, and search results with minimal data transfer.
/// </summary>
public class TenantRequestListItemReadModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public Domain.Enums.TenantRequestStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    
    // Tenant information (denormalized)
    public Guid TenantId { get; set; }
    public string TenantFullName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string TenantUnit { get; set; } = string.Empty;
    
    // Property information (denormalized)
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyPhone { get; set; } = string.Empty;
    
    // Superintendent information (denormalized)
    public string SuperintendentFullName { get; set; } = string.Empty;
    public string SuperintendentEmail { get; set; } = string.Empty;
    
    // Worker assignment information
    public string? AssignedWorkerEmail { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? CompletionNotes { get; set; }
    public bool? WorkCompletedSuccessfully { get; set; }
    
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
}