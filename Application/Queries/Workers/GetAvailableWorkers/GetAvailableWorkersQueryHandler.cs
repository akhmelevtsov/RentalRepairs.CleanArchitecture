using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs.Workers;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Application.Queries.Workers.GetAvailableWorkers;

/// <summary>
/// Enhanced query handler with booking visibility and smart worker ordering.
/// Phase 3: Now uses WorkerSpecialization enum for type-safe filtering.
/// </summary>
public class GetAvailableWorkersQueryHandler : IQueryHandler<GetAvailableWorkersQuery, List<WorkerAssignmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetAvailableWorkersQueryHandler> _logger;

    public GetAvailableWorkersQueryHandler(
        IApplicationDbContext context,
        ILogger<GetAvailableWorkersQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<WorkerAssignmentDto>> Handle(
        GetAvailableWorkersQuery request,
        CancellationToken cancellationToken)
    {
        var targetDate = request.ServiceDate;

        _logger.LogInformation(
            "Getting available workers for date {ServiceDate}, specialization={Specialization}, emergency={IsEmergency}",
            targetDate, request.RequiredSpecialization, request.IsEmergencyRequest);

        // Phase 3: Load workers with enum-based specialization filtering
        var workersQuery = _context.Workers
            .Include(w => w.Assignments)
            .Where(w => w.IsActive);

        // ? Phase 3: Filter by enum specialization
        if (request.RequiredSpecialization.HasValue)
        {
            var requiredSpec = request.RequiredSpecialization.Value;

            // General Maintenance workers can handle any work
            workersQuery = workersQuery.Where(w =>
                w.Specialization == requiredSpec ||
                w.Specialization == WorkerSpecialization.GeneralMaintenance);
        }

        var workers = await workersQuery.ToListAsync(cancellationToken);

        _logger.LogInformation("Loaded {WorkerCount} active workers", workers.Count);

        if (workers.Count == 0)
        {
            _logger.LogWarning(
                "No active workers found for specialization {RequiredSpecialization}",
                request.RequiredSpecialization);
            return new List<WorkerAssignmentDto>();
        }

        // Calculate booking data for each worker using Domain methods
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(request.LookAheadDays);

        var workerSummaries = workers
            .Select(w => WorkerAvailabilitySummary.CreateFromWorker(
                w,
                startDate,
                endDate,
                targetDate,
                request.IsEmergencyRequest))
            .ToList();

        _logger.LogInformation("Created {SummaryCount} worker availability summaries", workerSummaries.Count);

        // Order by availability score (lower = better) and take top N
        var orderedWorkers = workerSummaries
            .OrderBy(s => s.AvailabilityScore)
            .ThenBy(s => s.CurrentWorkload)
            .ThenBy(s => s.WorkerName)
            .Take(request.MaxWorkers)
            .ToList();

        _logger.LogInformation(
            "Selected top {SelectedCount} workers. Best score: {BestScore}, Worst score: {WorstScore}",
            orderedWorkers.Count,
            orderedWorkers.FirstOrDefault()?.AvailabilityScore ?? 0,
            orderedWorkers.LastOrDefault()?.AvailabilityScore ?? 0);

        // Map to DTO with booking data
        var result = orderedWorkers.Select(s => new WorkerAssignmentDto
        {
            WorkerId = s.WorkerId,
            WorkerName = s.WorkerName,
            WorkerEmail = s.WorkerEmail,
            Specialization = s.Specialization, // Enum
            IsAvailable = s.IsActive,
            CurrentWorkload = s.CurrentWorkload,
            NextAvailableDate = s.NextFullyAvailableDate,
            BookedDates = s.BookedDates.ToList(),
            PartiallyBookedDates = s.PartiallyBookedDates.ToList(),
            AvailabilityScore = s.AvailabilityScore
        }).ToList();

        _logger.LogInformation("Returning {ResultCount} worker assignments", result.Count);

        return result;
    }
}