using RentalRepairs.Domain.Common;
using System.Text.RegularExpressions;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing service work schedule information.
/// Provides comprehensive validation and factory methods for creating modified copies.
/// </summary>
public sealed class ServiceWorkScheduleInfo : ValueObject
{
    private static readonly Regex _emailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _workOrderRegex = new(
        @"^[A-Z0-9\-]{3,20}$",
        RegexOptions.Compiled);

    // Parameterless constructor for EF Core
    private ServiceWorkScheduleInfo()
    {
        WorkerEmail = string.Empty;
        WorkOrderNumber = string.Empty;
    }

    public ServiceWorkScheduleInfo(
        DateTime serviceDate,
        string workerEmail,
        string workOrderNumber,
        int workOrderSequence)
    {
        ServiceDate = ValidateServiceDate(serviceDate);
        WorkerEmail = ValidateAndNormalizeWorkerEmail(workerEmail);
        WorkOrderNumber = ValidateAndNormalizeWorkOrderNumber(workOrderNumber);
        WorkOrderSequence = ValidateWorkOrderSequence(workOrderSequence);
    }

    /// <summary>Gets the scheduled service date (immutable)</summary>
    public DateTime ServiceDate { get; private init; }

    /// <summary>Gets the assigned worker's email address (immutable, normalized)</summary>
    public string WorkerEmail { get; private init; }

    /// <summary>Gets the work order number (immutable, normalized)</summary>
    public string WorkOrderNumber { get; private init; }

    /// <summary>Gets the work order sequence number (immutable)</summary>
    public int WorkOrderSequence { get; private init; }

    /// <summary>Creates a new instance with updated service date</summary>
    public ServiceWorkScheduleInfo WithServiceDate(DateTime serviceDate)
    {
        return new ServiceWorkScheduleInfo(serviceDate, WorkerEmail, WorkOrderNumber, WorkOrderSequence);
    }

    /// <summary>Creates a new instance with updated worker email</summary>
    public ServiceWorkScheduleInfo WithWorkerEmail(string workerEmail)
    {
        return new ServiceWorkScheduleInfo(ServiceDate, workerEmail, WorkOrderNumber, WorkOrderSequence);
    }

    /// <summary>Creates a new instance with updated work order number</summary>
    public ServiceWorkScheduleInfo WithWorkOrderNumber(string workOrderNumber)
    {
        return new ServiceWorkScheduleInfo(ServiceDate, WorkerEmail, workOrderNumber, WorkOrderSequence);
    }

    /// <summary>Creates a new instance with updated sequence number</summary>
    public ServiceWorkScheduleInfo WithWorkOrderSequence(int workOrderSequence)
    {
        return new ServiceWorkScheduleInfo(ServiceDate, WorkerEmail, WorkOrderNumber, workOrderSequence);
    }

    /// <summary>Creates a new instance for rescheduling with a new date and incremented sequence</summary>
    public ServiceWorkScheduleInfo Reschedule(DateTime newServiceDate)
    {
        return new ServiceWorkScheduleInfo(newServiceDate, WorkerEmail, WorkOrderNumber, WorkOrderSequence + 1);
    }

    /// <summary>Gets the service date formatted as a short date string</summary>
    public string GetFormattedServiceDate()
    {
        return ServiceDate.ToString("yyyy-MM-dd");
    }

    /// <summary>Gets the service date formatted as a full date and time string</summary>
    public string GetFormattedServiceDateTime()
    {
        return ServiceDate.ToString("yyyy-MM-dd HH:mm");
    }

    /// <summary>Checks if the service is scheduled for today</summary>
    public bool IsScheduledForToday()
    {
        return ServiceDate.Date == DateTime.Today;
    }

    /// <summary>Checks if the service is overdue</summary>
    public bool IsOverdue()
    {
        return ServiceDate < DateTime.UtcNow;
    }

    /// <summary>Gets the number of days until the service date (negative if overdue)</summary>
    public int DaysUntilService()
    {
        return (ServiceDate.Date - DateTime.Today).Days;
    }

    private static DateTime ValidateServiceDate(DateTime serviceDate)
    {
        if (serviceDate == default)
        {
            throw new ArgumentException("Service date cannot be default value");
        }

        // Allow scheduling for today or future dates
        if (serviceDate.Date < DateTime.Today)
        {
            throw new ArgumentException("Service date cannot be in the past");
        }

        // Reasonable upper bound - within next 2 years
        if (serviceDate > DateTime.UtcNow.AddYears(2))
        {
            throw new ArgumentException("Service date cannot be more than 2 years in the future");
        }

        return serviceDate;
    }

    private static string ValidateAndNormalizeWorkerEmail(string workerEmail)
    {
        if (string.IsNullOrWhiteSpace(workerEmail))
        {
            throw new ArgumentException("Worker email cannot be empty");
        }

        string normalized = workerEmail.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
        {
            throw new ArgumentException("Worker email cannot exceed 254 characters");
        }

        if (!_emailRegex.IsMatch(normalized))
        {
            throw new ArgumentException("Worker email format is invalid");
        }

        return normalized;
    }

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

    private static int ValidateWorkOrderSequence(int workOrderSequence)
    {
        if (workOrderSequence < 1)
        {
            throw new ArgumentException("Work order sequence must be greater than 0");
        }

        if (workOrderSequence > 999)
        {
            throw new ArgumentException("Work order sequence cannot exceed 999");
        }

        return workOrderSequence;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ServiceDate;
        yield return WorkerEmail;
        yield return WorkOrderNumber;
        yield return WorkOrderSequence;
    }

    public override string ToString()
    {
        return
            $"Work Order {WorkOrderNumber} (#{WorkOrderSequence}) scheduled for {GetFormattedServiceDate()} with {WorkerEmail}";
    }
}
