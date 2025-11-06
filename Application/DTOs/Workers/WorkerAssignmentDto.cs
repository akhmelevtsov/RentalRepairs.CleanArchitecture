namespace RentalRepairs.Application.DTOs.Workers;

/// <summary>
/// DTO for worker assignment operations.
/// Used for assignment workflows, availability checking, and worker selection.
/// Enhanced with booking visibility data for UI calendar integration.
/// </summary>
public class WorkerAssignmentDto
{
    public Guid WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string WorkerEmail { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public bool IsAvailable { get; set; }
    public int CurrentWorkload { get; set; }
    public DateTime? NextAvailableDate { get; set; }

    /// <summary>
    /// List of dates where worker is fully booked (2/2 slots).
    /// UI should disable these dates for non-emergency requests.
    /// </summary>
    public List<DateTime> BookedDates { get; set; } = new();

    /// <summary>
    /// List of dates where worker is partially booked (1/2 slots).
    /// UI should show warning for these dates.
    /// </summary>
    public List<DateTime> PartiallyBookedDates { get; set; } = new();

    /// <summary>
    /// Availability score for ordering (lower = better).
    /// </summary>
    public int AvailabilityScore { get; set; }
}