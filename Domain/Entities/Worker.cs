using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Entities;

/// <summary>
/// Worker entity representing a maintenance worker in the rental repairs system.
/// Contains business logic for work assignments, specializations, and availability.
/// Phase 2: Now uses WorkerSpecialization enum for type safety.
/// </summary>
public class Worker : BaseEntity, IAggregateRoot
{
    #region Private Fields

    private readonly List<WorkAssignment> _assignments = new();

    #endregion

    #region Constructors

    protected Worker()
    {
        // For EF Core
        ContactInfo = null!;
    }

    public Worker(PersonContactInfo contactInfo) : base()
    {
        ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
        IsActive = true;
        AddDomainEvent(new WorkerRegisteredEvent(this));
    }

    #endregion

    #region Properties

    public PersonContactInfo ContactInfo { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public WorkerSpecialization Specialization { get; private set; } = WorkerSpecialization.GeneralMaintenance;
    public string? Notes { get; private set; }

    public IReadOnlyCollection<WorkAssignment> Assignments => _assignments.AsReadOnly();

    #endregion

    #region Public Business Methods

    public void SetSpecialization(WorkerSpecialization specialization)
    {
        WorkerSpecialization oldSpecialization = Specialization;
        Specialization = specialization;

        if (oldSpecialization != specialization)
        {
            AddDomainEvent(new WorkerSpecializationChangedEvent(this, oldSpecialization, specialization));
        }
    }

    public void AddNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return;
        }

        Notes = string.IsNullOrWhiteSpace(Notes)
            ? notes
            : $"{Notes}\n{DateTime.UtcNow:yyyy-MM-dd}: {notes}";
    }

    public void Deactivate(string reason = "")
    {
        IsActive = false;
        AddNotes($"Worker deactivated. Reason: {reason}");
        AddDomainEvent(new WorkerDeactivatedEvent(this, reason));
    }

    public void Activate()
    {
        IsActive = true;
        AddNotes("Worker reactivated");
        AddDomainEvent(new WorkerActivatedEvent(this));
    }

    /// <summary>
    /// Validates if this worker can be assigned to a tenant request for scheduling.
    /// </summary>
    /// <returns>True if the worker can be assigned to requests.</returns>
    public bool CanBeAssignedToRequest()
    {
        return IsActive;
    }

    /// <summary>
    /// Checks if the worker is available for work on the specified date.
    /// Updated to check capacity limit (max 2 assignments per day).
    /// </summary>
    /// <param name="workDate">The date to check availability for.</param>
    /// <param name="duration">Optional duration (not used - kept for backward compatibility).</param>
    /// <returns>True if the worker is available.</returns>
    public bool IsAvailableForWork(DateTime workDate, TimeSpan? duration = null)
    {
        if (!IsActive)
        {
            return false;
        }

        if (workDate < DateTime.UtcNow.Date)
        {
            return false;
        }

        // Check capacity for the date
        DateTime startOfDay = workDate.Date;
        DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var assignmentsOnDate = _assignments.Where(a =>
            a.ScheduledDate >= startOfDay &&
            a.ScheduledDate <= endOfDay &&
            !a.IsCompleted).ToList();

        // Business Rule: Limit to 2 assignments per day
        if (assignmentsOnDate.Count >= 2)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Assigns work to this worker with capacity validation.
    /// </summary>
    /// <param name="workOrderNumber">The work order number.</param>
    /// <param name="scheduledDate">The scheduled date for the work.</param>
    /// <param name="notes">Optional notes for the assignment.</param>
    public void AssignToWork(string workOrderNumber, DateTime scheduledDate, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(workOrderNumber))
        {
            throw new ArgumentException("Work order number cannot be empty", nameof(workOrderNumber));
        }

        // FIX: Compare dates only to allow scheduling for today or future dates
        if (scheduledDate.Date < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Scheduled date must be today or in the future", nameof(scheduledDate));
        }

        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot assign work to inactive worker");
        }

        // Check capacity: Limit to 2 assignments per day
        DateTime startOfDay = scheduledDate.Date;
        DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var assignmentsOnDate = _assignments.Where(a =>
            a.ScheduledDate >= startOfDay &&
            a.ScheduledDate <= endOfDay &&
            !a.IsCompleted).ToList();

        // Business Rule: Limit to 2 assignments per day
        if (assignmentsOnDate.Count >= 2)
        {
            throw new InvalidOperationException(
                $"Worker already has {assignmentsOnDate.Count} assignments on {scheduledDate:yyyy-MM-dd}. Maximum is 2 per day.");
        }

        // Create and add the assignment
        var newAssignment = new WorkAssignment(workOrderNumber, scheduledDate, notes);
        _assignments.Add(newAssignment);

        AddDomainEvent(new WorkerAssignedEvent(this, newAssignment));
    }

    /// <summary>
    /// Completes work for the specified work order.
    /// </summary>
    /// <param name="workOrderNumber">The work order number to complete.</param>
    /// <param name="successful">Whether the work was completed successfully.</param>
    /// <param name="completionNotes">Optional completion notes.</param>
    public void CompleteWork(string workOrderNumber, bool successful, string? completionNotes = null)
    {
        WorkAssignment? assignment = _assignments.FirstOrDefault(a => a.WorkOrderNumber == workOrderNumber);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Work order '{workOrderNumber}' not found for this worker");
        }

        // Create a new completed assignment and replace the old one
        WorkAssignment completedAssignment = assignment.Complete(successful, completionNotes);

        _assignments.Remove(assignment);
        _assignments.Add(completedAssignment);

        AddDomainEvent(new WorkCompletedEvent(this, completedAssignment, successful, completionNotes));
    }

    /// <summary>
    /// Gets the count of upcoming work assignments.
    /// </summary>
    /// <param name="fromDate">The starting date to count from.</param>
    /// <param name="daysAhead">The number of days ahead to include.</param>
    /// <returns>The count of upcoming assignments.</returns>
    public int GetUpcomingWorkloadCount(DateTime fromDate, int daysAhead = 7)
    {
        DateTime endDate = fromDate.AddDays(daysAhead);

        return _assignments.Count(a =>
            a.ScheduledDate >= fromDate &&
            a.ScheduledDate <= endDate &&
            !a.IsCompleted);
    }

    #endregion

    #region Worker Assignment Validation

    /// <summary>
    /// Domain method to validate if this worker can be assigned to a request.
    /// Follows Option 2: Rich Domain Entity Approach for business rule validation.
    /// Phase 2: Now uses SpecializationDeterminationService for validation.
    /// </summary>
    /// <param name="scheduledDate">The proposed scheduled date for the work</param>
    /// <param name="workOrderNumber">The work order number</param>
    /// <param name="requiredSpecialization">Optional required specialization</param>
    /// <param name="specializationService">Domain service for specialization validation</param>
    public void ValidateCanBeAssignedToRequest(
        DateTime scheduledDate,
        string workOrderNumber,
        WorkerSpecialization requiredSpecialization,
        Services.SpecializationDeterminationService specializationService)
    {
        // Business rule: Worker must be active
        if (!IsActive)
        {
            throw new WorkerNotAvailableException(ContactInfo.EmailAddress, "Worker is inactive");
        }

        // Business rule: Scheduled date must be today or in the future
        // FIX: Compare dates only to allow scheduling for today
        if (scheduledDate.Date < DateTime.UtcNow.Date)
        {
            throw new InvalidAssignmentParametersException(
                nameof(scheduledDate),
                scheduledDate,
                "Scheduled date must be today or in the future");
        }

        // Business rule: Work order number is required
        if (string.IsNullOrWhiteSpace(workOrderNumber))
        {
            throw new InvalidAssignmentParametersException(
                nameof(workOrderNumber),
                workOrderNumber,
                "Work order number is required");
        }

        // Business rule: Worker must be available on the scheduled date
        if (!IsAvailableForWork(scheduledDate))
        {
            throw new WorkerNotAvailableException(
                ContactInfo.EmailAddress,
                $"Worker is not available on {scheduledDate:yyyy-MM-dd}");
        }

        // Business rule: Worker must have required specialization
        if (!specializationService.CanHandleWork(Specialization, requiredSpecialization))
        {
            throw new WorkerNotAvailableException(
                ContactInfo.EmailAddress,
                $"Worker does not have required specialization: {requiredSpecialization}");
        }
    }

    #endregion

    #region Booking Availability Methods - Phase 1 Enhancement

    /// <summary>
    /// Gets dates where worker is fully booked (2+ assignments) within date range.
    /// Emergency requests can override this limit.
    /// </summary>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <param name="includeEmergencyOverride">If true, emergency requests are not counted toward booking limit</param>
    /// <returns>List of dates where worker has 2 or more non-completed assignments</returns>
    public List<DateTime> GetBookedDatesInRange(DateTime startDate, DateTime endDate,
        bool includeEmergencyOverride = false)
    {
        var bookedDates = new List<DateTime>();

        if (!IsActive)
        {
            return bookedDates;
        }

        // Iterate through each date in range
        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            DateTime dayStart = date;
            DateTime dayEnd = date.AddDays(1).AddTicks(-1);

            int assignmentCount = _assignments.Count(a =>
                a.ScheduledDate >= dayStart &&
                a.ScheduledDate <= dayEnd &&
                !a.IsCompleted);

            // For emergency override, we allow 3+ assignments (emergency can override the 2-per-day limit)
            int maxAssignments = includeEmergencyOverride ? 3 : 2;

            if (assignmentCount >= maxAssignments)
            {
                bookedDates.Add(date);
            }
        }

        return bookedDates;
    }

    /// <summary>
    /// Gets dates where worker has 1 of 2 slots filled (partial availability) within date range.
    /// </summary>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <returns>List of dates where worker has exactly 1 non-completed assignment</returns>
    public List<DateTime> GetPartiallyBookedDatesInRange(DateTime startDate, DateTime endDate)
    {
        var partialDates = new List<DateTime>();

        if (!IsActive)
        {
            return partialDates;
        }

        // Iterate through each date in range
        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            DateTime dayStart = date;
            DateTime dayEnd = date.AddDays(1).AddTicks(-1);

            int assignmentCount = _assignments.Count(a =>
                a.ScheduledDate >= dayStart &&
                a.ScheduledDate <= dayEnd &&
                !a.IsCompleted);

            // Exactly 1 assignment = partial availability
            if (assignmentCount == 1)
            {
                partialDates.Add(date);
            }
        }

        return partialDates;
    }

    /// <summary>
    /// Returns availability score for a specific date: 0 = fully booked, 1 = partially available, 2 = fully available.
    /// For emergency requests, allows 3 assignments per day (override the normal 2-per-day limit).
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <param name="isEmergencyRequest">If true, emergency bypass rules apply</param>
    /// <returns>0 (fully booked), 1 (partially available), or 2 (fully available)</returns>
    public int GetAvailabilityScoreForDate(DateTime date, bool isEmergencyRequest = false)
    {
        if (!IsActive)
        {
            return 0;
        }

        if (date < DateTime.Today)
        {
            return 0;
        }

        DateTime dayStart = date.Date;
        DateTime dayEnd = date.AddDays(1).AddTicks(-1);

        int assignmentCount = _assignments.Count(a =>
            a.ScheduledDate >= dayStart &&
            a.ScheduledDate <= dayEnd &&
            !a.IsCompleted);

        // Emergency requests can override the 2-per-day limit
        if (isEmergencyRequest)
        {
            // For emergency: 0-2 assignments = fully/partially available, 3+ = not available
            if (assignmentCount >= 3)
            {
                return 0; // Fully booked even with emergency override
            }

            // Return 2 if 0 assignments, 1 if 1-2 assignments (still allows emergency as 3rd)
            if (assignmentCount == 0)
            {
                return 2; // Fully available
            }

            return 1; // Partial availability (can still accept emergency as 2nd or 3rd)
        }

        // Normal requests: 0 assignments = 2 (fully available), 1 assignment = 1 (partial), 2+ = 0 (booked)
        return Math.Max(0, 2 - assignmentCount);
    }

    /// <summary>
    /// Finds the next date where worker is fully available (0 assignments).
    /// Used for ordering workers by availability.
    /// </summary>
    /// <param name="startDate">Starting date to search from</param>
    /// <param name="maxDaysAhead">Maximum number of days to search ahead (default 60)</param>
    /// <returns>Next fully available date, or null if none found within range</returns>
    public DateTime? GetNextFullyAvailableDate(DateTime startDate, int maxDaysAhead = 60)
    {
        if (!IsActive)
        {
            return null;
        }

        DateTime searchStart = startDate.Date < DateTime.Today ? DateTime.Today : startDate.Date;
        DateTime searchEnd = searchStart.AddDays(maxDaysAhead);

        for (DateTime date = searchStart; date <= searchEnd; date = date.AddDays(1))
        {
            int score = GetAvailabilityScoreForDate(date, false);
            if (score == 2) // Fully available (0 assignments)
            {
                return date;
            }
        }

        return null; // No fully available date found within range
    }

    /// <summary>
    /// Calculates an overall availability score for ordering workers.
    /// Lower score = better availability (sooner available date + lower workload).
    /// </summary>
    /// <param name="referenceDate">The reference date for calculation (typically today or preferred service date)</param>
    /// <returns>Availability score where lower values indicate better availability</returns>
    public int CalculateAvailabilityScore(DateTime referenceDate)
    {
        if (!IsActive)
        {
            return int.MaxValue; // Inactive workers get worst score
        }

        // Find next fully available date
        DateTime? nextAvailable = GetNextFullyAvailableDate(referenceDate, 60);

        // Calculate days until next available date
        int daysUntilAvailable = nextAvailable.HasValue
            ? (nextAvailable.Value - referenceDate.Date).Days
            : 999; // If no availability in 60 days, use high penalty

        // Get current workload
        int currentWorkload = GetUpcomingWorkloadCount(DateTime.UtcNow, 30);

        // Score formula: (days_until_available * 100) + current_workload
        // This prioritizes workers who are available sooner, then by lower workload
        return daysUntilAvailable * 100 + currentWorkload;
    }

    #endregion
}
