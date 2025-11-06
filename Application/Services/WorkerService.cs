using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentalRepairs.Application.Common.Configuration;
using RentalRepairs.Application.Common.Exceptions;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using RentalRepairs.Application.Queries.Workers.GetAvailableWorkers;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Application.Services;

/// <summary>
/// Consolidated Worker Service.
/// Contains business logic for worker-related operations that require orchestration.
/// Simple operations use CQRS directly via IMediator.
/// Phase 3: Now uses SpecializationDeterminationService for type-safe specialization handling.
/// </summary>
public class WorkerService : IWorkerService
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkerService> _logger;
    private readonly WorkerServiceSettings _settings;
    private readonly SpecializationDeterminationService _specializationService;

    public WorkerService(
        IMediator mediator,
        ILogger<WorkerService> logger,
        IOptions<WorkerServiceSettings> settings,
        SpecializationDeterminationService specializationService)
    {
        _mediator = mediator;
        _logger = logger;
        _settings = settings.Value;
        _specializationService = specializationService;
    }

    /// <summary>
    /// Gets workers available for a specific type of work with emergency support and booking visibility.
    /// Implements fallback strategy: specialized ? general maintenance ? any available.
    /// Phase 3: Now uses enum-based specialization.
    /// </summary>
    public async Task<List<WorkerOptionDto>> GetAvailableWorkersForRequestAsync(
        Guid requestId,
        WorkerSpecialization requiredSpecialization,
        DateTime preferredDate,
        bool isEmergencyRequest = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting available workers for request {RequestId}: specialization={Specialization}, date={PreferredDate}, emergency={IsEmergency}",
            requestId, requiredSpecialization, preferredDate, isEmergencyRequest);

        // Try specialized workers first
        var query = new GetAvailableWorkersQuery(preferredDate)
        {
            RequiredSpecialization = requiredSpecialization,
            IsEmergencyRequest = isEmergencyRequest,
            MaxWorkers = _settings.MaxAvailableWorkers,
            LookAheadDays = _settings.BookingLookAheadDays
        };

        var workerAssignments = await _mediator.Send(query, cancellationToken);
        var attemptCounts = new { primary = workerAssignments.Count, fallback = 0, any = 0 };

        // Fallback 1: Try General Maintenance workers if no specialized workers found
        if (workerAssignments.Count == 0 && requiredSpecialization != WorkerSpecialization.GeneralMaintenance)
        {
            _logger.LogDebug(
                "No specialized workers found, trying General Maintenance fallback for request {RequestId}",
                requestId);

            var fallbackQuery = new GetAvailableWorkersQuery(preferredDate)
            {
                RequiredSpecialization = WorkerSpecialization.GeneralMaintenance,
                IsEmergencyRequest = isEmergencyRequest,
                MaxWorkers = _settings.MaxAvailableWorkers,
                LookAheadDays = _settings.BookingLookAheadDays
            };

            workerAssignments = await _mediator.Send(fallbackQuery, cancellationToken);
            attemptCounts = attemptCounts with { fallback = workerAssignments.Count };
        }

        // Fallback 2: Try any available workers if still no results
        if (workerAssignments.Count == 0)
        {
            _logger.LogDebug(
                "No General Maintenance workers found, trying any available workers for request {RequestId}",
                requestId);

            var anyWorkersQuery = new GetAvailableWorkersQuery(preferredDate)
            {
                RequiredSpecialization = null,
                IsEmergencyRequest = isEmergencyRequest,
                MaxWorkers = _settings.MaxAvailableWorkers,
                LookAheadDays = _settings.BookingLookAheadDays
            };

            workerAssignments = await _mediator.Send(anyWorkersQuery, cancellationToken);
            attemptCounts = attemptCounts with { any = workerAssignments.Count };
        }

        // Convert to WorkerOptionDto with booking data
        var result = workerAssignments.Select(w => new WorkerOptionDto
        {
            Id = w.WorkerId,
            Email = w.WorkerEmail,
            FullName = w.WorkerName,
            Specialization = w.Specialization,
            IsAvailable = w.IsAvailable,
            NextAvailableDate = w.NextAvailableDate ?? preferredDate,
            ActiveAssignmentsCount = w.CurrentWorkload,
            BookedDates = w.BookedDates,
            PartiallyBookedDates = w.PartiallyBookedDates,
            AvailabilityScore = w.AvailabilityScore
        }).ToList();

        _logger.LogInformation(
            "Found {WorkerCount} available workers for request {RequestId} (primary={Primary}, fallback={Fallback}, any={Any})",
            result.Count, requestId, attemptCounts.primary, attemptCounts.fallback, attemptCounts.any);

        return result;
    }

    /// <summary>
    /// Gets assignment context with available workers and scheduling options.
    /// Enhanced with emergency request detection and booking visibility.
    /// Phase 3: Now determines specialization using domain service.
    /// </summary>
    public async Task<WorkerAssignmentContextDto> GetAssignmentContextAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting assignment context for request {RequestId}", requestId);

        // Get request data from database
        var request = await _mediator.Send(
            new GetTenantRequestByIdQuery(requestId),
            cancellationToken);

        if (request == null) throw new NotFoundException($"Tenant request with ID {requestId} not found");

        // ? Phase 3: Use domain service to determine required specialization
        var requiredSpecialization = _specializationService.DetermineRequiredSpecialization(
            request.Title,
            request.Description);

        _logger.LogInformation(
            "Determined specialization: {Specialization} for request {RequestId} (Title: '{Title}')",
            requiredSpecialization, requestId, request.Title);

        // Get suggested dates
        var suggestedDates = GenerateSuggestedDates();

        // Detect if request is emergency
        var isEmergencyRequest = request.UrgencyLevel.Contains("Emergency", StringComparison.OrdinalIgnoreCase) ||
                                 request.UrgencyLevel.Contains("Critical", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug(
            "Request context: specialization={Specialization}, emergency={IsEmergency}, urgency={UrgencyLevel}",
            requiredSpecialization, isEmergencyRequest, request.UrgencyLevel);

        // Get workers with booking data and emergency handling
        var availableWorkers = await GetAvailableWorkersForRequestAsync(
            requestId,
            requiredSpecialization,
            suggestedDates.First(),
            isEmergencyRequest,
            cancellationToken);

        return new WorkerAssignmentContextDto
        {
            Request = request,
            AvailableWorkers = availableWorkers,
            SuggestedDates = suggestedDates,
            IsEmergencyRequest = isEmergencyRequest,
            RequiredSpecialization = requiredSpecialization
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Generates suggested weekday dates for worker assignment.
    /// </summary>
    private List<DateTime> GenerateSuggestedDates()
    {
        var dates = new List<DateTime>();
        var currentDate = DateTime.Today.AddDays(1); // Start tomorrow
        var targetCount = _settings.SuggestedDatesCount;

        while (dates.Count < targetCount)
        {
            // Skip weekends for non-emergency work
            if (currentDate.DayOfWeek != DayOfWeek.Saturday &&
                currentDate.DayOfWeek != DayOfWeek.Sunday)
                dates.Add(currentDate);

            currentDate = currentDate.AddDays(1);
        }

        return dates;
    }

    #endregion
}