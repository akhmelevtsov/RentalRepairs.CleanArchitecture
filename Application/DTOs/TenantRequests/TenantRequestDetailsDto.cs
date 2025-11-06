namespace RentalRepairs.Application.DTOs.TenantRequests;

/// <summary>
/// DTO for detailed tenant request view - complete information.
/// Used for detail pages, full request displays, and comprehensive reporting.
/// </summary>
public class TenantRequestDetailsDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusDisplayName { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;

    // All dates
    public DateTime CreatedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? ClosedDate { get; set; }

    // Complete tenant information
    public Guid TenantId { get; set; }
    public string TenantFullName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string TenantUnit { get; set; } = string.Empty;

    // Complete property information
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyPhone { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;

    // Superintendent information
    public string SuperintendentFullName { get; set; } = string.Empty;
    public string SuperintendentEmail { get; set; } = string.Empty;

    // Work assignment details
    public string? AssignedWorkerEmail { get; set; }
    public string? AssignedWorkerName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? CompletionNotes { get; set; }
    public string? ClosureNotes { get; set; }
    public bool? WorkCompletedSuccessfully { get; set; }

    // Change history
    public List<TenantRequestChangeDto> RequestChanges { get; set; } = new();

    // UI properties
    public bool IsEmergency => UrgencyLevel?.Equals("Emergency", StringComparison.OrdinalIgnoreCase) == true;
    public bool CanBeScheduled => Status == "Submitted";
    public bool CanBeCompleted => Status == "Scheduled";
    public bool CanBeClosed => Status == "Done" || Status == "Declined";
}