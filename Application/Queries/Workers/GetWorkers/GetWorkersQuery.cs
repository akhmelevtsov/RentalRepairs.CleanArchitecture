using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Workers.GetWorkers;

/// <summary>
/// Query to retrieve workers with optional filtering by specialization and status.
/// Used for worker management lists and general worker browsing.
/// </summary>
public class GetWorkersQuery : IQuery<List<WorkerDto>>
{
    public string? Specialization { get; set; }
    public bool? IsActive { get; set; }
}