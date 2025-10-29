using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Queries.Workers.GetAvailableWorkers;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using RentalRepairs.Application.Common.Exceptions;
using RentalRepairs.Application.Commands.TenantRequests.ScheduleServiceWork;

namespace RentalRepairs.Application.Services;

/// <summary>
/// Consolidated Worker Service
/// Absorbs functionality from IWorkerAssignmentService for better cohesion
/// Contains business logic operations while simple CRUD uses CQRS directly.
/// Simplified implementation to demonstrate consolidation pattern.
/// </summary>
public class WorkerService : IWorkerService
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkerService> _logger;

    public WorkerService(IMediator mediator, ILogger<WorkerService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Business logic: Checks if a specific worker is available on a given date.
    /// </summary>
    public async Task<bool> IsWorkerAvailableAsync(string workerEmail, DateTime serviceDate, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simplified business logic - in production would check actual availability
            await Task.CompletedTask;
            
            // Mock business rules: Worker is available if not weekend and in the future
            return serviceDate > DateTime.Now && 
                   serviceDate.DayOfWeek != DayOfWeek.Saturday && 
                   serviceDate.DayOfWeek != DayOfWeek.Sunday;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking worker availability for {WorkerEmail} on {ServiceDate}", workerEmail, serviceDate);
            return false;
        }
    }

    /// <summary>
    /// Business logic: Gets distinct specializations from all active workers.
    /// </summary>
    public async Task<List<string>> GetWorkerSpecializationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simplified implementation - return standard specializations
            await Task.CompletedTask;
            
            return new List<string>
            {
                "Plumber",
                "Electrician",
                "HVAC",
                "Carpenter",
                "Painter",
                "Locksmith",
                "General Maintenance",
                "Appliance Repair"
            }.OrderBy(s => s).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker specializations");
            return new List<string>();
        }
    }

    /// <summary>
    /// Business logic: Gets workers available for a specific type of work.
    /// Consolidated from IWorkerAssignmentService.
    /// </summary>
    public async Task<List<WorkerOptionDto>> GetAvailableWorkersForRequestAsync(
        Guid requestId,
        string requiredSpecialization,
        DateTime preferredDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.CompletedTask;
            
            // Generate mock workers based on specialization
            var mockWorkers = GenerateMockWorkersForSpecialization(requiredSpecialization, preferredDate);
            
            return mockWorkers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available workers for request {RequestId}", requestId);
            return new List<WorkerOptionDto>();
        }
    }

    /// <summary>
    /// Business logic: Validates and assigns worker to a request with business rules.
    /// Consolidated from IWorkerAssignmentService.
    /// </summary>
    public async Task<WorkerAssignmentResult> AssignWorkerToRequestAsync(
        AssignWorkerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Business validation
            var validationResult = await ValidateWorkerAssignment(request, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            _logger.LogInformation("Assigning worker {WorkerEmail} to request {RequestId} for {ScheduledDate}", 
                request.WorkerEmail, request.RequestId, request.ScheduledDate);

            // Actually assign the worker using the MediatR command instead of just simulating
            var scheduleCommand = new ScheduleServiceWorkCommand
            {
                TenantRequestId = request.RequestId,
                WorkerEmail = request.WorkerEmail,
                ScheduledDate = request.ScheduledDate,
                WorkOrderNumber = request.WorkOrderNumber
            };

            await _mediator.Send(scheduleCommand, cancellationToken);

            _logger.LogInformation("Successfully assigned worker {WorkerEmail} to request {RequestId}", 
                request.WorkerEmail, request.RequestId);

            return new WorkerAssignmentResult
            {
                IsSuccess = true,
                SuccessMessage = $"Work successfully assigned to {request.WorkerEmail} for {request.ScheduledDate:yyyy-MM-dd}",
                WorkOrderId = Guid.NewGuid() // This could be returned from the command in a real implementation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning worker for request {RequestId}", request.RequestId);
            return new WorkerAssignmentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to assign worker: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Business logic: Gets assignment context with available workers and scheduling options.
    /// Consolidated from IWorkerAssignmentService.
    /// </summary>
    public async Task<WorkerAssignmentContextDto> GetAssignmentContextAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting assignment context for request {RequestId}", requestId);
            
            // Get actual request data from database instead of mock data
            var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
            if (request == null)
            {
                throw new NotFoundException($"Tenant request with ID {requestId} not found");
            }

            _logger.LogInformation("Loaded request details - Title: '{Title}', Description: '{Description}', Status: '{Status}'", 
                request.Title, request.Description, request.Status);

            // Determine required specialization based on actual request description
            var requiredSpecialization = DetermineRequiredSpecialization(request.Description);
            
            _logger.LogInformation("Required specialization determined as: '{RequiredSpecialization}'", requiredSpecialization);
            
            // Get suggested dates (next 7 days, excluding weekends)
            var suggestedDates = GenerateSuggestedDates();
            
            // Get real available workers from database instead of mock data
            var availableWorkers = await GetRealAvailableWorkersAsync(requiredSpecialization, suggestedDates.First(), cancellationToken);

            _logger.LogInformation("Found {WorkerCount} available workers for assignment context", availableWorkers.Count);

            return new WorkerAssignmentContextDto
            {
                Request = request,
                AvailableWorkers = availableWorkers,
                SuggestedDates = suggestedDates,
                IsEmergencyRequest = request.UrgencyLevel.Contains("Emergency", StringComparison.OrdinalIgnoreCase) ||
                                   request.UrgencyLevel.Contains("Critical", StringComparison.OrdinalIgnoreCase)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment context for request {RequestId}", requestId);
            throw;
        }
    }

    #region Private Helper Methods

    private List<WorkerOptionDto> GenerateMockWorkersForSpecialization(string specialization, DateTime preferredDate)
    {
        var workers = new List<WorkerOptionDto>();
        
        // Generate 3-5 mock workers for the specialization
        var workerNames = new[] { "John Smith", "Jane Doe", "Mike Johnson", "Sarah Williams", "Robert Brown" };
        
        for (int i = 0; i < 3; i++)
        {
            workers.Add(new WorkerOptionDto
            {
                Id = Guid.NewGuid(),
                Email = $"{specialization.ToLower()}.{workerNames[i].Split(' ')[1].ToLower()}@workers.com",
                FullName = workerNames[i],
                Specialization = specialization,
                IsAvailable = true,
                NextAvailableDate = preferredDate,
                ActiveAssignmentsCount = i + 1
            });
        }
        
        return workers.OrderBy(w => w.ActiveAssignmentsCount).ToList();
    }

    private async Task<List<WorkerOptionDto>> GetRealAvailableWorkersAsync(
        string requiredSpecialization,
        DateTime preferredDate,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting real available workers for specialization '{RequiredSpecialization}' on date {PreferredDate}", 
                requiredSpecialization, preferredDate);
            
            // Get real workers from database using existing CQRS query
            var query = new GetAvailableWorkersQuery(preferredDate)
            {
                RequiredSpecialization = requiredSpecialization
            };
            
            var workerAssignments = await _mediator.Send(query, cancellationToken);
            
            _logger.LogInformation("Query returned {WorkerCount} worker assignments for specialization '{RequiredSpecialization}'", 
                workerAssignments.Count, requiredSpecialization);
            
            // If no specialized workers found, try General Maintenance workers as fallback
            if (workerAssignments.Count == 0 && requiredSpecialization != "General Maintenance")
            {
                _logger.LogInformation("No workers found for '{RequiredSpecialization}', trying General Maintenance as fallback", 
                    requiredSpecialization);
                
                var fallbackQuery = new GetAvailableWorkersQuery(preferredDate)
                {
                    RequiredSpecialization = "General Maintenance"
                };
                
                workerAssignments = await _mediator.Send(fallbackQuery, cancellationToken);
                _logger.LogInformation("Fallback query returned {WorkerCount} General Maintenance workers", 
                    workerAssignments.Count);
            }
            
            // If still no workers, try any available workers (no specialization filter)
            if (workerAssignments.Count == 0)
            {
                _logger.LogInformation("No specialized workers found, trying any available workers");
                
                var anyWorkersQuery = new GetAvailableWorkersQuery(preferredDate)
                {
                    RequiredSpecialization = null // No specialization filter
                };
                
                workerAssignments = await _mediator.Send(anyWorkersQuery, cancellationToken);
                _logger.LogInformation("Any workers query returned {WorkerCount} workers", workerAssignments.Count);
            }
            
            // Convert WorkerAssignmentDto to WorkerOptionDto format
            var result = workerAssignments.Select(w => new WorkerOptionDto
            {
                Id = w.WorkerId,
                Email = w.WorkerEmail,
                FullName = w.WorkerName,
                Specialization = w.Specialization ?? "General",
                IsAvailable = w.IsAvailable,
                NextAvailableDate = w.NextAvailableDate ?? preferredDate,
                ActiveAssignmentsCount = w.CurrentWorkload
            }).ToList();
            
            _logger.LogInformation("Converted to {ResultCount} WorkerOptionDto objects", result.Count);
            
            if (result.Count > 0)
            {
                foreach (var worker in result)
                {
                    _logger.LogInformation("Worker: {WorkerName} ({WorkerEmail}) - Specialization: {Specialization}", 
                        worker.FullName, worker.Email, worker.Specialization);
                }
            }
            else
            {
                _logger.LogWarning("No workers found in database at all - database may be empty or all workers are inactive");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real available workers for specialization {Specialization}", requiredSpecialization);
            
            // Fallback to empty list if database query fails - don't use mock data
            _logger.LogWarning("Falling back to empty worker list due to database error for request");
            return new List<WorkerOptionDto>();
        }
    }

    private async Task<WorkerAssignmentResult> ValidateWorkerAssignment(
        AssignWorkerRequestDto request,
        CancellationToken cancellationToken)
    {
        // Validate required fields
        if (request.RequestId == Guid.Empty)
            return new WorkerAssignmentResult { IsSuccess = false, ErrorMessage = "Request ID is required" };

        if (string.IsNullOrWhiteSpace(request.WorkerEmail))
            return new WorkerAssignmentResult { IsSuccess = false, ErrorMessage = "Worker email is required" };

        if (request.ScheduledDate <= DateTime.Now)
            return new WorkerAssignmentResult { IsSuccess = false, ErrorMessage = "Scheduled date must be in the future" };

        if (string.IsNullOrWhiteSpace(request.WorkOrderNumber))
            return new WorkerAssignmentResult { IsSuccess = false, ErrorMessage = "Work order number is required" };

        // Check worker availability
        var isAvailable = await IsWorkerAvailableAsync(request.WorkerEmail, request.ScheduledDate, cancellationToken);
        if (!isAvailable)
            return new WorkerAssignmentResult { IsSuccess = false, ErrorMessage = "Worker is not available on the selected date" };

        return new WorkerAssignmentResult { IsSuccess = true };
    }

    private string DetermineRequiredSpecialization(string description)
    {
        var desc = description.ToLowerInvariant();
        
        _logger.LogInformation("Determining specialization for description: '{Description}' (normalized: '{NormalizedDescription}')", 
            description, desc);
        
        if (desc.Contains("plumb") || desc.Contains("leak") || desc.Contains("water") || desc.Contains("toilet") || desc.Contains("faucet"))
        {
            _logger.LogInformation("Determined specialization: Plumber");
            return "Plumber";
        }
        
        if (desc.Contains("electric") || desc.Contains("outlet") || desc.Contains("light") || desc.Contains("wire"))
        {
            _logger.LogInformation("Determined specialization: Electrician");
            return "Electrician";
        }
        
        if (desc.Contains("heat") || desc.Contains("air") || desc.Contains("hvac") || desc.Contains("temperature"))
        {
            _logger.LogInformation("Determined specialization: HVAC");
            return "HVAC";
        }
        
        if (desc.Contains("lock") || desc.Contains("key") || desc.Contains("door"))
        {
            _logger.LogInformation("Determined specialization: Locksmith");
            return "Locksmith";
        }
        
        if (desc.Contains("paint") || desc.Contains("wall") || desc.Contains("ceiling"))
        {
            _logger.LogInformation("Determined specialization: Painter");
            return "Painter";
        }
        
        if (desc.Contains("appliance") || desc.Contains("refrigerator") || desc.Contains("stove") || desc.Contains("washer"))
        {
            _logger.LogInformation("Determined specialization: Appliance Repair");
            return "Appliance Repair";
        }

        _logger.LogInformation("Determined specialization: General Maintenance (default)");
        return "General Maintenance";
    }

    private List<DateTime> GenerateSuggestedDates()
    {
        var dates = new List<DateTime>();
        var currentDate = DateTime.Today.AddDays(1); // Start tomorrow
        
        while (dates.Count < 7)
        {
            // Skip weekends for non-emergency work
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                dates.Add(currentDate);
            }
            currentDate = currentDate.AddDays(1);
        }
        
        return dates;
    }

    #endregion
}