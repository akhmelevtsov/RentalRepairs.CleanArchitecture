using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs.Workers;

namespace RentalRepairs.Application.Queries.Workers.GetAvailableWorkers;

/// <summary>
/// Query handler using read model for complex scenarios
/// </summary>
public class GetAvailableWorkersQueryHandler : IQueryHandler<GetAvailableWorkersQuery, List<WorkerAssignmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetAvailableWorkersQueryHandler> _logger;

    public GetAvailableWorkersQueryHandler(IApplicationDbContext context, ILogger<GetAvailableWorkersQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<WorkerAssignmentDto>> Handle(GetAvailableWorkersQuery request, CancellationToken cancellationToken)
    {
        var targetDate = request.ServiceDate;

        _logger.LogInformation("Getting available workers for date {ServiceDate} with specialization {RequiredSpecialization}", 
            targetDate, request.RequiredSpecialization ?? "Any");

        // First check total workers count for debugging
        var totalWorkers = await _context.Workers.CountAsync(cancellationToken);
        var activeWorkers = await _context.Workers.CountAsync(w => w.IsActive, cancellationToken);
        
        _logger.LogInformation("Database has {TotalWorkers} total workers, {ActiveWorkers} active workers", 
            totalWorkers, activeWorkers);

        // Execute the actual database query
        var availableWorkers = await _context.Workers
            .Where(w => w.IsActive)
            .Where(w => string.IsNullOrEmpty(request.RequiredSpecialization) || 
                       w.Specialization == request.RequiredSpecialization)
            .Select(w => new WorkerAssignmentDto
            {
                WorkerId = w.Id,
                WorkerName = w.ContactInfo.FirstName + " " + w.ContactInfo.LastName,
                WorkerEmail = w.ContactInfo.EmailAddress,
                Specialization = w.Specialization,
                IsAvailable = true, // Already filtered for availability
                CurrentWorkload = 0 // Simplified for now
            })
            .OrderBy(w => w.CurrentWorkload) // Order by workload for load balancing
            .ThenBy(w => w.WorkerName)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {WorkerCount} available workers matching criteria", availableWorkers.Count);

        if (availableWorkers.Count == 0 && !string.IsNullOrEmpty(request.RequiredSpecialization))
        {
            // Check if any workers have this specialization at all
            var workersWithSpecialization = await _context.Workers
                .Where(w => w.Specialization == request.RequiredSpecialization)
                .CountAsync(cancellationToken);
            
            _logger.LogWarning("No available workers found for specialization '{RequiredSpecialization}'. " +
                              "Workers with this specialization: {WorkersWithSpecialization} (active and inactive)", 
                              request.RequiredSpecialization, workersWithSpecialization);
        }

        return availableWorkers;
    }
}
