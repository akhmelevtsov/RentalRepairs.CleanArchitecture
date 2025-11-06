using RentalRepairs.Application.DTOs;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Application.Interfaces;

/// <summary>
/// Worker Service Interface for operations requiring orchestration.
/// Simple CRUD operations use CQRS directly via IMediator.
/// Phase 3: Now uses WorkerSpecialization enum for type safety.
/// </summary>
public interface IWorkerService
{
    /// <summary>
    /// Gets workers available for a specific type of work with emergency support and booking visibility.
    /// Implements fallback strategy: specialized ? general maintenance ? any available.
    /// </summary>
    Task<List<WorkerOptionDto>> GetAvailableWorkersForRequestAsync(
        Guid requestId,
        WorkerSpecialization requiredSpecialization,
        DateTime preferredDate,
        bool isEmergencyRequest = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment context with available workers and scheduling options.
    /// Orchestrates query, specialization detection, and date generation.
    /// </summary>
    Task<WorkerAssignmentContextDto> GetAssignmentContextAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Worker option DTO with booking visibility data for UI calendar integration.
/// Phase 3: Now uses WorkerSpecialization enum.
/// </summary>
public class WorkerOptionDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public WorkerSpecialization Specialization { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime? NextAvailableDate { get; set; }
    public int ActiveAssignmentsCount { get; set; }

    /// <summary>
    /// List of dates where worker is fully booked (2/2 slots filled).
    /// UI should disable/dim these dates for non-emergency requests.
    /// </summary>
    public List<DateTime> BookedDates { get; set; } = new();

    /// <summary>
    /// List of dates where worker is partially booked (1/2 slots filled).
    /// UI should show warning indicator for these dates.
    /// </summary>
    public List<DateTime> PartiallyBookedDates { get; set; } = new();

    /// <summary>
    /// Availability score for ordering workers (lower = better availability).
    /// Formula: (DaysUntilNextAvailable * 100) + CurrentWorkload
    /// </summary>
    public int AvailabilityScore { get; set; }
}

/// <summary>
/// Assignment context DTO with request details, available workers, and suggested dates.
/// Phase 3: Now includes determined specialization.
/// </summary>
public class WorkerAssignmentContextDto
{
    public TenantRequestDto Request { get; set; } = new();
    public List<WorkerOptionDto> AvailableWorkers { get; set; } = new();
    public List<DateTime> SuggestedDates { get; set; } = new();
    public bool IsEmergencyRequest { get; set; }
    public WorkerSpecialization RequiredSpecialization { get; set; }
}