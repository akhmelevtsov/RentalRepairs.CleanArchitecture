namespace RentalRepairs.Application.DTOs.TenantRequests;

/// <summary>
/// DTO for tenant request summary in lists - minimal fields for performance.
/// Used for list views, grids, and search results where minimal data is needed.
/// </summary>
public class TenantRequestSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusDisplayName { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }

    // Flattened tenant info
    public string TenantFullName { get; set; } = string.Empty;
    public string TenantUnit { get; set; } = string.Empty;

    // Flattened property info
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;

    // UI properties
    public bool IsEmergency => UrgencyLevel?.Equals("Emergency", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsOverdue => ScheduledDate.HasValue && ScheduledDate < DateTime.UtcNow && Status == "Scheduled";
}