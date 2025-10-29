using MediatR;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestStatusSummary;

/// <summary>
/// Analytics query for tenant request status summary.
/// Provides aggregated status counts for dashboard and reporting purposes.
/// 
/// Note: Other analytics queries (pending, overdue, by urgency, by property, date range) 
/// have been removed as they are efficiently handled by GetTenantRequestsQuery with appropriate filters.
/// </summary>
public record GetTenantRequestStatusSummaryQuery : IRequest<Dictionary<string, int>>
{
}
