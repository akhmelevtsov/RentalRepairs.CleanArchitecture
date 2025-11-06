using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Rich value object representing worker availability data for assignment UI.
/// Encapsulates booking information, availability metrics, and worker details.
/// Immutable by design following value object patterns.
/// Phase 2: Now uses WorkerSpecialization enum.
/// </summary>
public sealed class WorkerAvailabilitySummary : ValueObject
{
    /// <summary>
    /// Worker's unique identifier
    /// </summary>
    public Guid WorkerId { get; init; }

    /// <summary>
    /// Worker's email address (primary contact)
    /// </summary>
    public string WorkerEmail { get; init; } = string.Empty;

    /// <summary>
    /// Worker's full name for display
    /// </summary>
    public string WorkerName { get; init; } = string.Empty;

    /// <summary>
    /// Worker's specialization enum
    /// </summary>
    public WorkerSpecialization Specialization { get; init; }

    /// <summary>
    /// Next date where worker has 0 assignments (fully available).
    /// Null if no fully available date found within search range.
    /// </summary>
    public DateTime? NextFullyAvailableDate { get; init; }

    /// <summary>
    /// Current workload count (upcoming assignments in next 30 days)
    /// </summary>
    public int CurrentWorkload { get; init; }

    /// <summary>
    /// List of dates where worker is fully booked (2/2 slots filled).
    /// Dates in this list should be disabled in UI for non-emergency requests.
    /// </summary>
    public IReadOnlyList<DateTime> BookedDates { get; init; } = new List<DateTime>();

    /// <summary>
    /// List of dates where worker is partially booked (1/2 slots filled).
    /// Dates in this list should show warning indicator in UI.
    /// </summary>
    public IReadOnlyList<DateTime> PartiallyBookedDates { get; init; } = new List<DateTime>();

    /// <summary>
    /// Availability score for ordering workers (lower = better availability).
    /// Formula: (DaysUntilNextAvailable * 100) + CurrentWorkload
    /// </summary>
    public int AvailabilityScore { get; init; }

    /// <summary>
    /// Total number of active (non-completed) assignments
    /// </summary>
    public int ActiveAssignmentsCount { get; init; }

    /// <summary>
    /// Indicates if worker is active and available for new assignments
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Private constructor for EF Core and factory methods
    /// </summary>
    private WorkerAvailabilitySummary() { }

    /// <summary>
    /// Factory method to create availability summary from worker entity
    /// </summary>
    /// <param name="worker">Worker entity to extract availability from</param>
    /// <param name="startDate">Start date for booking data range</param>
    /// <param name="endDate">End date for booking data range</param>
    /// <param name="referenceDate">Reference date for availability score calculation</param>
    /// <param name="includeEmergencyOverride">If true, emergency override rules apply</param>
    /// <returns>Immutable worker availability summary</returns>
    public static WorkerAvailabilitySummary CreateFromWorker(
        Entities.Worker worker,
        DateTime startDate,
        DateTime endDate,
        DateTime referenceDate,
        bool includeEmergencyOverride = false)
    {
        if (worker == null)
        {
            throw new ArgumentNullException(nameof(worker));
        }

        return new WorkerAvailabilitySummary
        {
            WorkerId = worker.Id,
            WorkerEmail = worker.ContactInfo.EmailAddress,
            WorkerName = worker.ContactInfo.GetFullName(),
            Specialization = worker.Specialization,
            NextFullyAvailableDate = worker.GetNextFullyAvailableDate(referenceDate),
            CurrentWorkload = worker.GetUpcomingWorkloadCount(DateTime.UtcNow, 30),
            BookedDates = worker.GetBookedDatesInRange(startDate, endDate, includeEmergencyOverride),
            PartiallyBookedDates = worker.GetPartiallyBookedDatesInRange(startDate, endDate),
            AvailabilityScore = worker.CalculateAvailabilityScore(referenceDate),
            ActiveAssignmentsCount = worker.Assignments.Count(a => !a.IsCompleted),
            IsActive = worker.IsActive
        };
    }

    /// <summary>
    /// Checks if worker is available on a specific date
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <param name="allowPartial">If true, partial availability (1/2 slots) is considered available</param>
    /// <returns>True if worker has availability on the date</returns>
    public bool IsAvailableOnDate(DateTime date, bool allowPartial = true)
    {
        DateTime checkDate = date.Date;

        // Fully booked = not available
        if (BookedDates.Any(d => d.Date == checkDate))
        {
            return false;
        }

        // Partially booked = available if allowPartial is true
        if (PartiallyBookedDates.Any(d => d.Date == checkDate))
        {
            return allowPartial;
        }

        // Not in either list = fully available
        return true;
    }

    /// <summary>
    /// Gets availability status text for a specific date
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>Human-readable availability status</returns>
    public string GetAvailabilityStatusForDate(DateTime date)
    {
        DateTime checkDate = date.Date;

        if (BookedDates.Any(d => d.Date == checkDate))
        {
            return "Fully Booked (2/2 slots)";
        }

        if (PartiallyBookedDates.Any(d => d.Date == checkDate))
        {
            return "Limited Availability (1/2 slots)";
        }

        return "Fully Available (0/2 slots)";
    }

    /// <summary>
    /// Gets availability indicator for UI display
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>Indicator: "✓" (available), "⚠" (partial), "✗" (booked)</returns>
    public string GetAvailabilityIndicator(DateTime date)
    {
        DateTime checkDate = date.Date;

        if (BookedDates.Any(d => d.Date == checkDate))
        {
            return "✗";
        }

        if (PartiallyBookedDates.Any(d => d.Date == checkDate))
        {
            return "⚠";
        }

        return "✓";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return WorkerId;
        yield return WorkerEmail;
        yield return AvailabilityScore;

        foreach (DateTime date in BookedDates.OrderBy(d => d))
        {
            yield return date;
        }

        foreach (DateTime date in PartiallyBookedDates.OrderBy(d => d))
        {
            yield return date;
        }
    }

    public override string ToString()
    {
        string availability = NextFullyAvailableDate.HasValue
            ? $"Next available: {NextFullyAvailableDate.Value:yyyy-MM-dd}"
            : "No availability in next 60 days";

        return $"{WorkerName} ({Specialization.GetDisplayName()}) - {availability} - Workload: {CurrentWorkload}";
    }
}
