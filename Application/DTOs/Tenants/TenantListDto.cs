namespace RentalRepairs.Application.DTOs.Tenants;

/// <summary>
/// DTO for individual tenant information in lists.
/// Used for tenant list views, management interfaces, and summary displays.
/// </summary>
public class TenantListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public int ActiveRequestsCount { get; set; }
    public int TotalRequestsCount { get; set; }
    public DateTime? LastRequestDate { get; set; }

    // UI properties
    public bool HasActiveRequests => ActiveRequestsCount > 0;
}