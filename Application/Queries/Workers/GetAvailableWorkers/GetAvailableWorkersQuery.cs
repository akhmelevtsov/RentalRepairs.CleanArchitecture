using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Application.Queries.Workers.GetAvailableWorkers;

/// <summary>
/// Query to get available workers for a specific service date.
/// Enhanced with specialization filtering, emergency support, and booking visibility.
/// Phase 3: Now uses WorkerSpecialization enum for type safety.
/// </summary>
public class GetAvailableWorkersQuery : IQuery<List<WorkerAssignmentDto>>
{
    public DateTime ServiceDate { get; }
    public WorkerSpecialization? RequiredSpecialization { get; set; }
    public bool IsEmergencyRequest { get; set; }
    public int MaxWorkers { get; set; } = 10;
    public int LookAheadDays { get; set; } = 30;

    public GetAvailableWorkersQuery(DateTime serviceDate)
    {
        ServiceDate = serviceDate;
    }
}

/// <summary>
/// Worker assignment DTO with availability and booking information.
/// Phase 3: Now uses WorkerSpecialization enum.
/// </summary>
public class WorkerAssignmentDto
{
    public Guid WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string WorkerEmail { get; set; } = string.Empty;
    public WorkerSpecialization Specialization { get; set; }
    public bool IsAvailable { get; set; }
    public int CurrentWorkload { get; set; }
    public DateTime? NextAvailableDate { get; set; }

    /// <summary>
    /// Dates where worker is fully booked (2/2 slots filled).
    /// </summary>
    public List<DateTime> BookedDates { get; set; } = new();

    /// <summary>
    /// Dates where worker is partially booked (1/2 slots filled).
    /// </summary>
    public List<DateTime> PartiallyBookedDates { get; set; } = new();

    /// <summary>
    /// Availability score (lower = better).
    /// </summary>
    public int AvailabilityScore { get; set; }
}