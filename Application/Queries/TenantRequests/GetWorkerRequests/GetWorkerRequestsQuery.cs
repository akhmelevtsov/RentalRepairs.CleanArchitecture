using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Models;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Application.Queries.TenantRequests.GetWorkerRequests;

/// <summary>
/// Query to retrieve requests assigned to a specific worker with pagination.
/// Used for worker dashboards and workload management.
/// </summary>
public class GetWorkerRequestsQuery : IQuery<PagedResult<TenantRequestDto>>
{
    public string WorkerEmail { get; set; } = default!;
    public TenantRequestStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public GetWorkerRequestsQuery(string workerEmail)
    {
        WorkerEmail = workerEmail;
    }
}