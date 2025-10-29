namespace RentalRepairs.Application.DTOs.TenantRequests;

/// <summary>
/// DTO for tenant request creation - input fields only.
/// Used for API endpoints and form submissions when creating new requests.
/// </summary>
public class CreateTenantRequestDto
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = "Normal";
}