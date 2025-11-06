using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace RentalRepairs.Application.Commands.TenantRequests.ScheduleServiceWork;

/// <summary>
/// Enhanced command handler for scheduling service work with business rule validation.
/// Phase 3: Now uses SpecializationDeterminationService for type-safe specialization handling.
/// Determines required specialization DURING SCHEDULING (not when tenant creates request).
/// </summary>
public class ScheduleServiceWorkCommandHandler : IRequestHandler<ScheduleServiceWorkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly UnitSchedulingService _unitSchedulingService;
    private readonly SpecializationDeterminationService _specializationService;
    private readonly ILogger<ScheduleServiceWorkCommandHandler> _logger;

    public ScheduleServiceWorkCommandHandler(
        IApplicationDbContext context,
        UnitSchedulingService unitSchedulingService,
        SpecializationDeterminationService specializationService,
        ILogger<ScheduleServiceWorkCommandHandler> logger)
    {
        _context = context;
        _unitSchedulingService = unitSchedulingService;
        _specializationService = specializationService;
        _logger = logger;
    }

    public async Task Handle(ScheduleServiceWorkCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing schedule service work command for request {RequestId} with worker {WorkerEmail}",
            request.TenantRequestId, request.WorkerEmail);

        // Get tenant request
        var tenantRequest = await _context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRequestId, cancellationToken);

        if (tenantRequest == null)
            throw new InvalidOperationException($"Tenant request with ID {request.TenantRequestId} not found");

        // Get worker with assignments
        var worker = await _context.Workers
            .Include(w => w.Assignments)
            .FirstOrDefaultAsync(w => w.ContactInfo.EmailAddress == request.WorkerEmail, cancellationToken);

        if (worker == null)
            throw new InvalidOperationException($"Worker with email {request.WorkerEmail} not found");

        // ✅ Phase 3: Determine required specialization from request description using domain service
        var requiredSpecialization = _specializationService.DetermineRequiredSpecialization(
            tenantRequest.Title,
            tenantRequest.Description);

        _logger.LogInformation(
            "Determined required specialization: {Specialization} for request {RequestId} (Title: '{Title}')",
            requiredSpecialization, request.TenantRequestId, tenantRequest.Title);

        // Get property for validation
        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == tenantRequest.PropertyId, cancellationToken);

        // Get existing assignments for validation
        var existingAssignments = await GetExistingAssignmentsAsync(request.ScheduledDate, cancellationToken);

        // ✅ Phase 2: Temporary workaround - UnitSchedulingService still uses strings
        // TODO: Update UnitSchedulingService to use enums in next phase
        var workerSpecString = _specializationService.GetDisplayName(worker.Specialization);
        var requiredSpecString = _specializationService.GetDisplayName(requiredSpecialization);

        var validationResult = _unitSchedulingService.ValidateWorkerAssignment(
            request.TenantRequestId,
            property?.Code ?? "Unknown",
            tenantRequest.TenantUnit,
            request.ScheduledDate,
            request.WorkerEmail,
            workerSpecString, // Temporary string conversion
            requiredSpecString, // Temporary string conversion
            tenantRequest.IsEmergency,
            existingAssignments);

        if (!validationResult.IsValid)
            throw new InvalidOperationException(
                $"Worker assignment validation failed: {validationResult.ErrorMessage}");

        // Handle emergency overrides
        if (tenantRequest.IsEmergency && validationResult.AssignmentsToCancelForEmergency.Any())
        {
            await ProcessEmergencyOverrides(validationResult.AssignmentsToCancelForEmergency, cancellationToken);
            _logger.LogInformation(
                "Emergency request {RequestId} cancelled {CancelCount} normal assignments",
                request.TenantRequestId, validationResult.AssignmentsToCancelForEmergency.Count);
        }

        // Log emergency conflicts
        if (validationResult.HasEmergencyConflicts)
            _logger.LogWarning(
                "Emergency request {RequestId} has conflicts with {ConflictCount} other emergency requests",
                request.TenantRequestId, validationResult.EmergencyConflicts.Count);

// Assign work to worker
        worker.AssignToWork(request.WorkOrderNumber, request.ScheduledDate);

        // Schedule the tenant request
        var workerName = worker.ContactInfo.GetFullName();
        tenantRequest.Schedule(
            request.ScheduledDate,
            worker.ContactInfo.EmailAddress,
            request.WorkOrderNumber,
            workerName);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully scheduled work for request {RequestId} with worker {WorkerEmail} (specialization: {Specialization})",
            request.TenantRequestId, request.WorkerEmail, requiredSpecialization);
    }

    private async Task<List<ExistingAssignment>> GetExistingAssignmentsAsync(
        DateTime date,
        CancellationToken cancellationToken)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var scheduledRequests = await _context.TenantRequests
            .Where(tr => tr.ScheduledDate.HasValue &&
                         tr.ScheduledDate >= startOfDay &&
                         tr.ScheduledDate < endOfDay &&
                         tr.Status == TenantRequestStatus.Scheduled)
            .ToListAsync(cancellationToken);

        var assignments = new List<ExistingAssignment>();

        foreach (var r in scheduledRequests)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == r.PropertyId, cancellationToken);

            // ✅ Phase 3 FIX: Get worker specialization as string for UnitSchedulingService compatibility
            var workerSpecialization = "Unknown";
            if (!string.IsNullOrEmpty(r.AssignedWorkerEmail))
            {
                var worker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.ContactInfo.EmailAddress == r.AssignedWorkerEmail, cancellationToken);

                if (worker != null) workerSpecialization = _specializationService.GetDisplayName(worker.Specialization);
            }

            assignments.Add(new ExistingAssignment
            {
                TenantRequestId = r.Id,
                PropertyCode = property?.Code ?? "Unknown",
                UnitNumber = r.TenantUnit,
                WorkerEmail = r.AssignedWorkerEmail ?? "",
                WorkerSpecialization = workerSpecialization,
                WorkOrderNumber = r.WorkOrderNumber ?? "",
                ScheduledDate = r.ScheduledDate ?? DateTime.MinValue,
                Status = r.Status.ToString(),
                IsEmergency = r.IsEmergency
            });
        }

        return assignments;
    }

    private async Task ProcessEmergencyOverrides(
        List<ExistingAssignment> assignmentsToCancel,
        CancellationToken cancellationToken)
    {
        var overrideResult = _unitSchedulingService.ProcessEmergencyOverride(assignmentsToCancel);

        foreach (var cancelledRequestId in overrideResult.CancelledRequestIds)
        {
            var requestToCancel = await _context.TenantRequests
                .FirstOrDefaultAsync(tr => tr.Id == cancelledRequestId, cancellationToken);

            if (requestToCancel != null)
            {
                requestToCancel.FailDueToEmergencyOverride("Cancelled due to emergency request override");

                _logger.LogInformation(
                    "Emergency override: Failed request {RequestId} to allow rescheduling",
                    cancelledRequestId);
            }
        }
    }
}