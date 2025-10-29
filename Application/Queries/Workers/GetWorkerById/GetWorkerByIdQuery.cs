using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Workers.GetWorkerById;

/// <summary>
/// Query to retrieve a specific worker by their unique identifier.
/// Used for worker profile pages and individual worker operations.
/// </summary>
public class GetWorkerByIdQuery : IQuery<WorkerDto>
{
    public Guid WorkerId { get; set; }

    public GetWorkerByIdQuery(Guid workerId)
    {
        WorkerId = workerId;
    }
}