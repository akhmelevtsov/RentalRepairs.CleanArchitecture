using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Models;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Workers.GetWorkerRequests;

/// <summary>
/// Query to retrieve paginated requests assigned to a specific worker.
/// Used for worker dashboards and workload tracking.
/// </summary>
public class GetWorkerRequestsQuery : IQuery<PagedResult<TenantRequestDto>>
{
    public string WorkerEmail { get; set; } = default!;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public GetWorkerRequestsQuery(string workerEmail)
    {
        WorkerEmail = workerEmail;
    }
}