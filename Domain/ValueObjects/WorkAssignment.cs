using RentalRepairs.Domain.Common;
using System.Text.RegularExpressions;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a work assignment.
/// Provides comprehensive validation and factory methods for creating modified copies.
/// </summary>
public sealed class WorkAssignment : ValueObject
{
    #region Private Fields

    private static readonly Regex _workOrderRegex = new(@"^[A-Z0-9\-]{3,20}$", RegexOptions.Compiled);

    #endregion

    #region Constructors

    public WorkAssignment(string workOrderNumber, DateTime scheduledDate, string? notes = null)
    {
        WorkOrderNumber = ValidateAndNormalizeWorkOrderNumber(workOrderNumber);
        ScheduledDate = ValidateScheduledDate(scheduledDate);
        Notes = ValidateAndNormalizeNotes(notes);
        AssignedDate = DateTime.UtcNow;
        IsCompleted = false;
        CompletedDate = null;
        CompletedSuccessfully = null;
        CompletionNotes = null;
    }

    // Private constructor for creating completed work assignments
    private WorkAssignment(
        string workOrderNumber, 
        DateTime scheduledDate, 
        string? notes,
        DateTime assignedDate,
        bool isCompleted,
        DateTime? completedDate,
        bool? completedSuccessfully,
        string? completionNotes)
    {
        WorkOrderNumber = workOrderNumber;
        ScheduledDate = scheduledDate;
        Notes = notes;
        AssignedDate = assignedDate;
        IsCompleted = isCompleted;
        CompletedDate = completedDate;
        CompletedSuccessfully = completedSuccessfully;
        CompletionNotes = completionNotes;
    }

    #endregion

    #region Properties

    /// <summary>Gets the work order number (immutable)</summary>
    public string WorkOrderNumber { get; private init; }
    
    /// <summary>Gets the scheduled date (immutable)</summary>
    public DateTime ScheduledDate { get; private init; }
    
    /// <summary>Gets the assignment notes (immutable, optional)</summary>
    public string? Notes { get; private init; }
    
    /// <summary>Gets the date when the work was assigned (immutable)</summary>
    public DateTime AssignedDate { get; private init; }
    
    /// <summary>Gets whether the work is completed (immutable)</summary>
    public bool IsCompleted { get; private init; }
    
    /// <summary>Gets the completion date (immutable, null if not completed)</summary>
    public DateTime? CompletedDate { get; private init; }
    
    /// <summary>Gets whether the work was completed successfully (immutable, null if not completed)</summary>
    public bool? CompletedSuccessfully { get; private init; }
    
    /// <summary>Gets the completion notes (immutable, null if not completed)</summary>
    public string? CompletionNotes { get; private init; }

    #endregion

    #region Public Methods

    /// <summary>Creates a new instance with updated scheduled date</summary>
    public WorkAssignment WithScheduledDate(DateTime scheduledDate)
        => new(WorkOrderNumber, scheduledDate, Notes);

    /// <summary>Creates a new instance with updated notes</summary>
    public WorkAssignment WithNotes(string? notes)
        => new(WorkOrderNumber, ScheduledDate, notes);

    /// <summary>Creates a new completed work assignment</summary>
    public WorkAssignment Complete(bool successful, string? completionNotes = null)
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Work assignment is already completed");
        }

        return new WorkAssignment(
            WorkOrderNumber,
            ScheduledDate,
            Notes,
            AssignedDate,
            isCompleted: true,
            completedDate: DateTime.UtcNow,
            completedSuccessfully: successful,
            completionNotes: ValidateAndNormalizeCompletionNotes(completionNotes));
    }

    /// <summary>Checks if this assignment overlaps with the specified date and duration</summary>
    public bool OverlapsWith(DateTime date, TimeSpan duration)
    {
        DateTime assignmentStart = ScheduledDate.Date;
        DateTime assignmentEnd = assignmentStart.AddDays(1);
        DateTime requestStart = date;
        DateTime requestEnd = date.Add(duration);

        return requestStart < assignmentEnd && requestEnd > assignmentStart;
    }

    /// <summary>Gets the number of days until the scheduled date (negative if past)</summary>
    public int DaysUntilScheduled() => (ScheduledDate.Date - DateTime.Today).Days;

    /// <summary>Checks if the assignment is scheduled for today</summary>
    public bool IsScheduledForToday() => ScheduledDate.Date == DateTime.Today;

    /// <summary>Checks if the assignment is overdue</summary>
    public bool IsOverdue() => !IsCompleted && ScheduledDate < DateTime.UtcNow;

    /// <summary>Gets the duration since assignment (or until completion if completed)</summary>
    public TimeSpan GetDuration() => (CompletedDate ?? DateTime.UtcNow) - AssignedDate;

    #endregion

    #region Private Validation Methods

    private static string ValidateAndNormalizeWorkOrderNumber(string workOrderNumber)
    {
        if (string.IsNullOrWhiteSpace(workOrderNumber))
        {
            throw new ArgumentException("Work order number cannot be empty");
        }

        string normalized = workOrderNumber.Trim().ToUpperInvariant();

        if (normalized.Length < 3)
        {
            throw new ArgumentException("Work order number must be at least 3 characters long");
        }

        if (normalized.Length > 20)
        {
            throw new ArgumentException("Work order number cannot exceed 20 characters");
        }

        if (!_workOrderRegex.IsMatch(normalized))
        {
            throw new ArgumentException("Work order number format is invalid (alphanumeric with hyphens only)");
        }

        return normalized;
    }

    private static DateTime ValidateScheduledDate(DateTime scheduledDate)
    {
        if (scheduledDate == default)
        {
            throw new ArgumentException("Scheduled date cannot be default value");
        }

        // Remove the past date validation to allow Entity Framework to load historical data
        // Business logic should handle validation in the application layer when creating new assignments

        // Reasonable upper bound - within next year
        if (scheduledDate > DateTime.UtcNow.AddYears(1))
        {
            throw new ArgumentException("Scheduled date cannot be more than 1 year in the future");
        }

        return scheduledDate;
    }

    private static string? ValidateAndNormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        string trimmed = notes.Trim();

        if (trimmed.Length > 500)
        {
            throw new ArgumentException("Notes cannot exceed 500 characters");
        }

        return trimmed;
    }

    private static string? ValidateAndNormalizeCompletionNotes(string? completionNotes)
    {
        if (string.IsNullOrWhiteSpace(completionNotes))
        {
            return null;
        }

        string trimmed = completionNotes.Trim();

        if (trimmed.Length > 1000)
        {
            throw new ArgumentException("Completion notes cannot exceed 1000 characters");
        }

        return trimmed;
    }

    #endregion

    #region Equality and Hash Code

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return WorkOrderNumber;
        yield return ScheduledDate;
        yield return AssignedDate;
    }

    public override string ToString()
    {
        string status = IsCompleted 
            ? CompletedSuccessfully == true ? "Completed Successfully" : "Completed with Issues"
            : IsOverdue() ? "Overdue" : "Pending";
            
        return $"Work Order {WorkOrderNumber} scheduled for {ScheduledDate:yyyy-MM-dd} - {status}";
    }

    #endregion
}
