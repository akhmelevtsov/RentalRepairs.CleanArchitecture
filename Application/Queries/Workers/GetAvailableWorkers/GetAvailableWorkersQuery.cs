using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs.Workers;

namespace RentalRepairs.Application.Queries.Workers.GetAvailableWorkers;

/// <summary>
/// Query to retrieve workers available for assignment on a specific service date.
/// Used for work assignment workflows and availability checking.
/// </summary>
public class GetAvailableWorkersQuery : IQuery<List<WorkerAssignmentDto>>
{
    public DateTime ServiceDate { get; set; }
    public string? RequiredSpecialization { get; set; }

    public GetAvailableWorkersQuery(DateTime serviceDate)
    {
        ServiceDate = serviceDate;
    }
}