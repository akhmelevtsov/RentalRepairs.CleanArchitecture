namespace RentalRepairs.Application.DTOs;

public class TenantRequestDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    
    // Status information
    public string Status { get; set; } = string.Empty;
    public string StatusDisplayName { get; set; } = string.Empty;
    
    public string UrgencyLevel { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    
    // Related entity information
    public Guid TenantId { get; set; }
    public string TenantFullName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string TenantUnit { get; set; } = string.Empty;
    
    // Property information
    public Guid PropertyId { get; set; } // ? Added missing PropertyId
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyPhone { get; set; } = string.Empty; // ? Added missing PropertyPhone
    
    // Superintendent information
    public string SuperintendentFullName { get; set; } = string.Empty; // ? Added missing
    public string SuperintendentEmail { get; set; } = string.Empty; // ? Added missing
    
    // Work scheduling information
    public string? AssignedWorkerEmail { get; set; }
    public string? AssignedWorkerName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public bool? WorkCompletedSuccessfully { get; set; }
    public string? CompletionNotes { get; set; }
    public string? ClosureNotes { get; set; }
    
    // Tenant preferences
    public string? PreferredContactTime { get; set; }
    
    // ? UI-optimized derived properties with proper setters for EF projections
    public bool IsEmergency { get; set; } // ? Changed to have setter
    
    public bool IsActive => Status == "Submitted" || Status == "Scheduled";
    public bool IsCompleted => Status == "Done" || Status == "Closed";
}

public class TenantRequestChangeDto
{
    public Guid Id { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public string StatusDisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = default!;
    public DateTime ChangeDate { get; set; }
    public string? ChangedByEmail { get; set; }
}