using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Interfaces;

/// <summary>
/// Consolidated Worker Service Interface  
/// Absorbs worker assignment functionality for better cohesion
/// Simple CRUD operations should still use CQRS directly via IMediator.
/// </summary>
public interface IWorkerService
{
    /// <summary>
    /// Business logic: Checks if a specific worker is available on a given date.
    /// </summary>
    Task<bool> IsWorkerAvailableAsync(string workerEmail, DateTime serviceDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Gets distinct specializations from all active workers.
    /// </summary>
    Task<List<string>> GetWorkerSpecializationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Gets workers available for a specific type of work.
    /// Consolidated from IWorkerAssignmentService.
    /// </summary>
    Task<List<WorkerOptionDto>> GetAvailableWorkersForRequestAsync(
        Guid requestId,
        string requiredSpecialization,
        DateTime preferredDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Validates and assigns worker to a request with business rules.
    /// Consolidated from IWorkerAssignmentService.
    /// </summary>
    Task<WorkerAssignmentResult> AssignWorkerToRequestAsync(
        AssignWorkerRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Gets assignment context with available workers and scheduling options.
    /// Consolidated from IWorkerAssignmentService.
    /// </summary>
    Task<WorkerAssignmentContextDto> GetAssignmentContextAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supporting DTOs for consolidated worker service
/// </summary>
public class WorkerOptionDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public DateTime? NextAvailableDate { get; set; }
    public int ActiveAssignmentsCount { get; set; }
}

public class WorkerAssignmentResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid? WorkOrderId { get; set; }
}

public class AssignWorkerRequestDto
{
    public Guid RequestId { get; set; }
    public string WorkerEmail { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class WorkerAssignmentContextDto
{
    public TenantRequestDto Request { get; set; } = new();
    public List<WorkerOptionDto> AvailableWorkers { get; set; } = new();
    public List<DateTime> SuggestedDates { get; set; } = new();
    public bool IsEmergencyRequest { get; set; }
}