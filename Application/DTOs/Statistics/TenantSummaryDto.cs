namespace RentalRepairs.Application.DTOs.Statistics;

/// <summary>
/// DTO for tenant summary statistics providing insights about tenant activity and engagement.
/// Used for tenant management dashboard and reporting.
/// </summary>
public class TenantSummaryDto
{
    public int TotalTenants { get; set; }
    public int TenantsWithActiveRequests { get; set; }
    public int TenantsWithPendingRequests { get; set; }
    public int TenantsWithScheduledWork { get; set; }
    public double AverageRequestsPerTenant { get; set; }
    public int TotalRequestsAllTenants { get; set; }
    public int NewTenantsThisMonth { get; set; }
}