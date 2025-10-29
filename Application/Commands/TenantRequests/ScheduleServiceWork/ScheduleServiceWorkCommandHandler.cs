using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace RentalRepairs.Application.Commands.TenantRequests.ScheduleServiceWork;

/// <summary>
/// Enhanced command handler for scheduling service work with business rule validation
/// Now integrates UnitSchedulingService for proper conflict resolution and emergency overrides
/// </summary>
public class ScheduleServiceWorkCommandHandler : IRequestHandler<ScheduleServiceWorkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly UnitSchedulingService _unitSchedulingService;
    private readonly ILogger<ScheduleServiceWorkCommandHandler> _logger;

    public ScheduleServiceWorkCommandHandler(
        IApplicationDbContext context,
        UnitSchedulingService unitSchedulingService,
        ILogger<ScheduleServiceWorkCommandHandler> logger)
    {
        _context = context;
        _unitSchedulingService = unitSchedulingService;
        _logger = logger;
    }

    public async Task Handle(ScheduleServiceWorkCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing schedule service work command for request {RequestId} with worker {WorkerEmail}",
            request.TenantRequestId, request.WorkerEmail);

        // Get tenant request using direct EF Core
        var tenantRequest = await _context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRequestId, cancellationToken);

        if (tenantRequest == null)
        {
            throw new InvalidOperationException($"Tenant request with ID {request.TenantRequestId} not found");
        }

        // Get worker using direct EF Core - INCLUDE ASSIGNMENTS
        var worker = await _context.Workers
            .Include(w => w.Assignments) // Load the assignments collection
            .FirstOrDefaultAsync(w => w.ContactInfo.EmailAddress == request.WorkerEmail, cancellationToken);

        if (worker == null)
        {
            throw new InvalidOperationException($"Worker with email {request.WorkerEmail} not found");
        }

        // Get property for validation
        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == tenantRequest.PropertyId, cancellationToken);

        // Determine required specialization
        var requiredSpecialization = Worker.DetermineRequiredSpecialization(tenantRequest.Title, tenantRequest.Description);

        // Get existing assignments for validation
        var existingAssignments = await GetExistingAssignmentsAsync(request.ScheduledDate, cancellationToken);

        // Use domain service to validate assignment
        var validationResult = _unitSchedulingService.ValidateWorkerAssignment(
            request.TenantRequestId,
            property?.Code ?? "Unknown",
            tenantRequest.TenantUnit,
            request.ScheduledDate,
            request.WorkerEmail,
            worker.Specialization ?? "General Maintenance",
            requiredSpecialization,
            tenantRequest.IsEmergency,
            existingAssignments);

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Worker assignment validation failed: {validationResult.ErrorMessage}");
        }

        // Handle emergency overrides if needed
        if (tenantRequest.IsEmergency && validationResult.AssignmentsToCancelForEmergency.Any())
        {
            await ProcessEmergencyOverrides(validationResult.AssignmentsToCancelForEmergency, cancellationToken);
            _logger.LogInformation("Emergency request {RequestId} cancelled {CancelCount} normal assignments",
                request.TenantRequestId, validationResult.AssignmentsToCancelForEmergency.Count);
        }

        // Log emergency conflicts if they exist
        if (validationResult.HasEmergencyConflicts)
        {
            _logger.LogWarning("Emergency request {RequestId} has conflicts with {ConflictCount} other emergency requests in same unit/date",
                request.TenantRequestId, validationResult.EmergencyConflicts.Count);
        }

        // Use rich domain model methods for assignment
        worker.AssignToWork(request.WorkOrderNumber, request.ScheduledDate);

        // Pass both email and name to the scheduling method
        var workerName = worker.ContactInfo.GetFullName();
        tenantRequest.Schedule(request.ScheduledDate, worker.ContactInfo.EmailAddress, request.WorkOrderNumber, workerName);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully scheduled work for request {RequestId} with worker {WorkerEmail}",
            request.TenantRequestId, request.WorkerEmail);
    }

    /// <summary>
    /// Get existing assignments for validation
    /// </summary>
    private async Task<List<ExistingAssignment>> GetExistingAssignmentsAsync(DateTime date, CancellationToken cancellationToken)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var scheduledRequests = await _context.TenantRequests
            .Where(tr => tr.ScheduledDate.HasValue &&
                        tr.ScheduledDate >= startOfDay &&
                        tr.ScheduledDate < endOfDay &&
                        tr.Status == Domain.Enums.TenantRequestStatus.Scheduled)
            .ToListAsync(cancellationToken);

        var assignments = new List<ExistingAssignment>();

        foreach (var r in scheduledRequests)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == r.PropertyId, cancellationToken);

            assignments.Add(new ExistingAssignment
            {
                TenantRequestId = r.Id,
                PropertyCode = property?.Code ?? "Unknown",
                UnitNumber = r.TenantUnit,
                WorkerEmail = r.AssignedWorkerEmail ?? "",
                WorkerSpecialization = "Unknown", // We'd need to join with worker data for full validation
                WorkOrderNumber = r.WorkOrderNumber ?? "",
                ScheduledDate = r.ScheduledDate ?? DateTime.MinValue,
                Status = r.Status.ToString(),
                IsEmergency = r.IsEmergency
            });
        }

        return assignments;
    }

    /// <summary>
    /// Process emergency overrides by using the domain's FailDueToEmergencyOverride method
    /// </summary>
    private async Task ProcessEmergencyOverrides(List<ExistingAssignment> assignmentsToCancel, CancellationToken cancellationToken)
    {
        var overrideResult = _unitSchedulingService.ProcessEmergencyOverride(assignmentsToCancel);

        foreach (var cancelledRequestId in overrideResult.CancelledRequestIds)
        {
            var requestToCancel = await _context.TenantRequests
                .FirstOrDefaultAsync(tr => tr.Id == cancelledRequestId, cancellationToken);

            if (requestToCancel != null)
            {
                // Use the proper domain method for emergency overrides
                requestToCancel.FailDueToEmergencyOverride("Cancelled due to emergency request override");

                _logger.LogInformation("Emergency override: Failed request {RequestId} to allow rescheduling", cancelledRequestId);
            }
        }
    }
}
