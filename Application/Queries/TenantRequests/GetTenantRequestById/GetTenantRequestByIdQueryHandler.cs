using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;

/// <summary>
/// Enhanced query handler that can return request with business context.
/// Supports both simple TenantRequestDto and enriched TenantRequestDetailsDto.
/// </summary>
public class GetTenantRequestByIdQueryHandler : IRequestHandler<GetTenantRequestByIdQuery, TenantRequestDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly RequestAuthorizationPolicy _authorizationPolicy;
    private readonly TenantRequestStatusPolicy _statusPolicy;
    private readonly ILogger<GetTenantRequestByIdQueryHandler> _logger;

    public GetTenantRequestByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        RequestAuthorizationPolicy authorizationPolicy,
        TenantRequestStatusPolicy statusPolicy,
        ILogger<GetTenantRequestByIdQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _authorizationPolicy = authorizationPolicy;
        _statusPolicy = statusPolicy;
        _logger = logger;
    }

    public async Task<TenantRequestDto?> Handle(GetTenantRequestByIdQuery request, CancellationToken cancellationToken)
    {
        // Fetch base data from database
        var tenantRequest = await _context.TenantRequests
            .Where(tr => tr.Id == request.Id)
            .Join(_context.Properties,
                tr => tr.PropertyId,
                p => p.Id,
                (tr, p) => new { TenantRequest = tr, Property = p })
            .Select(joined => new TenantRequestDto
            {
                Id = joined.TenantRequest.Id,
                Code = joined.TenantRequest.Code,
                Title = joined.TenantRequest.Title,
                Description = joined.TenantRequest.Description,
                Status = joined.TenantRequest.Status.ToString(),
                UrgencyLevel = joined.TenantRequest.UrgencyLevel,
                IsEmergency = joined.TenantRequest.IsEmergency,
                CreatedDate = joined.TenantRequest.CreatedAt,
                ScheduledDate = joined.TenantRequest.ScheduledDate,
                CompletedDate = joined.TenantRequest.CompletedDate,
                TenantId = joined.TenantRequest.TenantId,
                TenantFullName = joined.TenantRequest.TenantFullName,
                TenantEmail = joined.TenantRequest.TenantEmail,
                TenantUnit = joined.TenantRequest.TenantUnit,
                PropertyId = joined.TenantRequest.PropertyId,
                PropertyCode = joined.Property.Code,
                PropertyName = joined.Property.Name,
                PropertyPhone = joined.Property.PhoneNumber ?? "Not available",
                SuperintendentFullName = joined.Property.Superintendent != null
                    ? joined.Property.Superintendent.FirstName + " " + joined.Property.Superintendent.LastName
                    : "Not assigned",
                SuperintendentEmail = joined.Property.Superintendent != null
                    ? joined.Property.Superintendent.EmailAddress
                    : "Not available",
                AssignedWorkerEmail = joined.TenantRequest.AssignedWorkerEmail,
                AssignedWorkerName = joined.TenantRequest.AssignedWorkerName,
                WorkOrderNumber = joined.TenantRequest.WorkOrderNumber,
                CompletionNotes = joined.TenantRequest.CompletionNotes,
                ClosureNotes = joined.TenantRequest.ClosureNotes,
                WorkCompletedSuccessfully = joined.TenantRequest.WorkCompletedSuccessfully,
                PreferredContactTime = joined.TenantRequest.PreferredContactTime
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantRequest == null) return null;

        // If query requests business context, enrich the DTO
        if (request.IncludeBusinessContext) return EnrichWithBusinessContext(tenantRequest);

        return tenantRequest;
    }

    /// <summary>
    /// Enriches a base DTO with business context (authorization, available actions).
    /// This is where TenantRequestService logic now lives.
    /// </summary>
    private TenantRequestDetailsDto EnrichWithBusinessContext(TenantRequestDto request)
    {
        // Parse status from DTO
        if (!Enum.TryParse<TenantRequestStatus>(request.Status, out var status))
        {
            _logger.LogError(
                "Invalid status {Status} for request {RequestId}",
                request.Status, request.Id);
            status = TenantRequestStatus.Draft; // Fallback
        }

        // Get user role from claims-based authentication
        var userRole = _currentUserService.IsAuthenticated
            ? _currentUserService.UserRole
            : null;

        if (string.IsNullOrEmpty(userRole))
            _logger.LogWarning(
                "User role not found for authenticated user {UserId} in request {RequestId}",
                _currentUserService.UserId,
                request.Id);

        // Build business context using domain services
        var availableActions = userRole != null
            ? _authorizationPolicy.GetAvailableActionsForRole(userRole, status)
            : new List<RequestAction>();

        var canEdit = userRole != null &&
                      _authorizationPolicy.CanRoleEditRequestInStatus(userRole, status);
        var canCancel = userRole != null &&
                        _authorizationPolicy.CanRoleCancelRequestInStatus(userRole, status);

        var allowedStatuses = _statusPolicy.GetAllowedNextStatuses(status);
        var nextAllowedStatus = allowedStatuses.Any()
            ? allowedStatuses.First().ToString()
            : null;

        // Create enriched DTO
        return new TenantRequestDetailsDto
        {
            // Copy all base properties
            Id = request.Id,
            Code = request.Code,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            UrgencyLevel = request.UrgencyLevel,
            IsEmergency = request.IsEmergency,
            CreatedDate = request.CreatedDate,
            ScheduledDate = request.ScheduledDate,
            CompletedDate = request.CompletedDate,
            TenantId = request.TenantId,
            TenantFullName = request.TenantFullName,
            TenantEmail = request.TenantEmail,
            TenantUnit = request.TenantUnit,
            PropertyId = request.PropertyId,
            PropertyCode = request.PropertyCode,
            PropertyName = request.PropertyName,
            PropertyPhone = request.PropertyPhone,
            SuperintendentFullName = request.SuperintendentFullName,
            SuperintendentEmail = request.SuperintendentEmail,
            AssignedWorkerEmail = request.AssignedWorkerEmail,
            AssignedWorkerName = request.AssignedWorkerName,
            WorkOrderNumber = request.WorkOrderNumber,
            CompletionNotes = request.CompletionNotes,
            ClosureNotes = request.ClosureNotes,
            WorkCompletedSuccessfully = request.WorkCompletedSuccessfully,
            PreferredContactTime = request.PreferredContactTime,

            // Add business context
            CanEdit = canEdit,
            CanCancel = canCancel,
            CanAssignWorker = availableActions.Contains(RequestAction.AssignWorker),
            AvailableActions = availableActions.Select(a => a.ToString()).ToList(),
            NextAllowedStatus = nextAllowedStatus
        };
    }
}