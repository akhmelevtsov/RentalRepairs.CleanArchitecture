using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequests;

/// <summary>
/// Query to retrieve tenant requests with comprehensive filtering and sorting.
/// Used for request list pages, management interfaces, and filtered searches.
/// </summary>
public class GetTenantRequestsQuery : IQuery<List<TenantRequestDto>>
{
    public Guid? PropertyId { get; set; }
    public Guid? TenantId { get; set; }
    public string? WorkerEmail { get; set; }
    public TenantRequestStatus? Status { get; set; }
    public string? UrgencyLevel { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool PendingOnly { get; set; }
    public bool OverdueOnly { get; set; }
    public bool? IsEmergencyOnly { get; set; }

    // Sorting and pagination
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}