using MediatR;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using RentalRepairs.Application.Commands.TenantRequests.CreateAndSubmitTenantRequest;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Services.Models;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Repositories;

namespace RentalRepairs.Application.Services;

/// <summary>
/// FIXED: Pure orchestration service for tenant request operations.
/// Follows proper DDD architecture with clean separation of concerns:
/// - Application layer handles data orchestration and coordination
/// - Domain services provide pure business logic
/// - No business logic in Application layer
/// - No repository dependencies in Domain services
/// </summary>
public class TenantRequestService : ITenantRequestService
{
    private readonly IMediator _mediator;
    private readonly TenantRequestPolicyService _policyService; // ? Pure domain service
    private readonly ITenantRequestRepository _tenantRequestRepository; // ? Application orchestration
    private readonly ITenantRepository _tenantRepository; // ? Application orchestration
    private readonly UserRoleService _userRoleService; // ? Application service for role management
    private readonly ILogger<TenantRequestService> _logger;

    public TenantRequestService(
        IMediator mediator,
        TenantRequestPolicyService policyService,
        ITenantRequestRepository tenantRequestRepository,
        ITenantRepository tenantRepository,
        UserRoleService userRoleService,
        ILogger<TenantRequestService> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
        _tenantRequestRepository = tenantRequestRepository ?? throw new ArgumentNullException(nameof(tenantRequestRepository));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _userRoleService = userRoleService ?? throw new ArgumentNullException(nameof(userRoleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FIXED: Application orchestration - loads data then delegates to pure domain service.
    /// </summary>
    public async Task<bool> IsWorkflowTransitionAllowedAsync(
        Guid tenantRequestId,
        string fromStatus,
        string toStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse enum values (Application responsibility)
            if (!Enum.TryParse<TenantRequestStatus>(toStatus, out var toStatusEnum))
            {
                _logger.LogWarning("Invalid status value: {ToStatus}", toStatus);
                return false;
            }

            // Data loading (Application responsibility)
            var request = await _tenantRequestRepository.GetByIdAsync(tenantRequestId, cancellationToken);
            if (request == null)
            {
                return false;
            }

            // Business logic delegation (Domain responsibility)
            var result = _policyService.ValidateWorkflowTransition(
                request, 
                toStatusEnum, 
                "SystemUser"); // Simplified for this context

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking workflow transition for request {RequestId} from {FromStatus} to {ToStatus}", 
                tenantRequestId, fromStatus, toStatus);
            return false;
        }
    }

    /// <summary>
    /// FIXED: Application orchestration - loads data then delegates to pure domain service.
    /// </summary>
    public async Task<bool> IsUserAuthorizedForRequestAsync(
        Guid tenantRequestId,
        string userEmail,
        string action,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Data loading (Application responsibility)
            var request = await _tenantRequestRepository.GetByIdAsync(tenantRequestId, cancellationToken);
            if (request == null)
            {
                return false;
            }

            // Business logic mapping (Application responsibility)
            var domainAction = MapActionToDomainAction(action);
            var userRole = _userRoleService.DetermineUserRole(userEmail); // ? FIXED: Use synchronous method

            // Business logic delegation (Domain responsibility)
            var result = _policyService.ValidateUserAuthorization(request, userRole, domainAction);

            return result.IsAuthorized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authorization for user {UserEmail} on request {RequestId} for action {Action}", 
                userEmail, tenantRequestId, action);
            return false;
        }
    }

    /// <summary>
    /// FIXED: Application orchestration - loads data then delegates to pure domain service.
    /// </summary>
    public async Task<TenantRequestDetailsDto> GetRequestDetailsWithContextAsync(
        Guid requestId,
        string? userEmail = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Data loading via CQRS (Application responsibility)
            var request = await _mediator.Send(new GetTenantRequestByIdQuery(requestId), cancellationToken);
            if (request == null)
                throw new InvalidOperationException($"Request {requestId} not found");

            // Load related data (Application responsibility)
            var domainRequest = await _tenantRequestRepository.GetByIdAsync(requestId, cancellationToken);
            if (domainRequest == null)
                throw new InvalidOperationException($"Domain request {requestId} not found");

            var tenant = await _tenantRepository.GetByIdAsync(domainRequest.TenantId, cancellationToken);

            // Business context via pure domain service (Domain responsibility)
            var userRole = !string.IsNullOrEmpty(userEmail) ? _userRoleService.DetermineUserRole(userEmail) : null;
            var businessContext = _policyService.GenerateBusinessContext(domainRequest, tenant, userRole);

            // Data transformation (Application responsibility)
            return MapToDetailsDto(request, businessContext, userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting request details for {RequestId}", requestId);
            throw;
        }
    }

    /// <summary>
    /// FIXED: Application orchestration - loads data, validates with domain service, executes via CQRS.
    /// </summary>
    public async Task<TenantRequestSubmissionResult> ValidateAndSubmitRequestAsync(
        SubmitTenantRequestDto request,
        string tenantEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Data loading (Application responsibility)
            var allTenants = await _tenantRepository.GetAllAsync(cancellationToken);
            var tenant = allTenants.FirstOrDefault(t => 
                t.ContactInfo.EmailAddress.Equals(tenantEmail, StringComparison.OrdinalIgnoreCase));

            if (tenant == null)
            {
                return new TenantRequestSubmissionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Tenant not found",
                    ValidationErrors = new List<string> { "Tenant not found" }
                };
            }

            // Business validation via pure domain service (Domain responsibility)
            var submissionRequest = new RequestSubmissionRequest
            {
                Title = request.Title,
                Description = request.Description,
                UrgencyLevel = request.UrgencyLevel,
                PropertyCode = request.PropertyCode,
                UnitNumber = request.UnitNumber,
                TenantEmail = tenantEmail,
                PreferredContactTime = request.PreferredContactTime
            };

            var validationResult = _policyService.ValidateRequestSubmission(submissionRequest, tenant);
            if (!validationResult.IsValid)
            {
                return new TenantRequestSubmissionResult
                {
                    IsSuccess = false,
                    ErrorMessage = validationResult.ErrorMessage,
                    ValidationErrors = new List<string> { validationResult.ErrorMessage ?? "Validation failed" }
                };
            }

            // Execution via CQRS (Application responsibility)
            var command = new CreateAndSubmitTenantRequestCommand
            {
                Title = request.Title,
                Description = request.Description,
                UrgencyLevel = request.UrgencyLevel,
                PropertyCode = request.PropertyCode,
                TenantEmail = tenantEmail,
                UnitNumber = request.UnitNumber,
                PreferredContactTime = request.PreferredContactTime
            };

            var commandResult = await _mediator.Send(command, cancellationToken);

            // Result mapping (Application responsibility)
            return new TenantRequestSubmissionResult
            {
                IsSuccess = commandResult.IsSuccess,
                RequestId = commandResult.RequestId,
                ErrorMessage = commandResult.ErrorMessage,
                ValidationErrors = commandResult.ValidationErrors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting request for tenant {TenantEmail}", tenantEmail);
            return new TenantRequestSubmissionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// FIXED: Application orchestration - generates strategy with domain service, loads data via CQRS.
    /// </summary>
    public async Task<List<TenantRequestSummaryDto>> GetRequestsForUserAsync(
        string userEmail,
        string userRole,
        RequestFilterOptions? filters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Business filtering strategy via pure domain service (Domain responsibility)
            var filterCriteria = MapToRequestFilterCriteria(filters);
            var filteringStrategy = _policyService.GenerateFilteringStrategy(userRole, userEmail, filterCriteria);

            // Data loading via CQRS with domain-driven filtering (Application orchestration)
            var requests = await LoadFilteredRequestsAsync(filteringStrategy, cancellationToken);

            // Data transformation (Application responsibility)
            return requests.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting requests for user {UserEmail} with role {UserRole}", userEmail, userRole);
            return new List<TenantRequestSummaryDto>();
        }
    }

    #region Private Helper Methods - Data Transformation (Application Layer Responsibility)

    private RequestAction MapActionToDomainAction(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "view" => RequestAction.Edit,
            "edit" => RequestAction.Edit,
            "assign" => RequestAction.AssignWorker,
            "complete" => RequestAction.CompleteWork,
            "cancel" => RequestAction.Cancel,
            "submit" => RequestAction.Submit,
            "decline" => RequestAction.Decline,
            "close" => RequestAction.Close,
            _ => RequestAction.Edit
        };
    }

    private string DetermineUserRoleFromEmail(string userEmail)
    {
        // ? FIXED: Delegate to proper application service instead of hardcoded logic
        return _userRoleService.DetermineUserRole(userEmail);
    }

    private TenantRequestDetailsDto MapToDetailsDto(
        TenantRequestDto request, 
        TenantRequestBusinessContext businessContext, // ? FIXED: Use correct type
        string? userEmail)
    {
        var detailsDto = new TenantRequestDetailsDto
        {
            // Base request information
            Id = request.Id,
            Code = request.Code,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status.ToString(),
            UrgencyLevel = request.UrgencyLevel,
            CreatedDate = request.CreatedDate,
            ScheduledDate = request.ScheduledDate,
            CompletedDate = request.CompletedDate,
            
            // Flattened information
            TenantId = request.TenantId,
            TenantFullName = request.TenantFullName,
            TenantEmail = request.TenantEmail,
            TenantUnit = request.TenantUnit,
            PropertyName = request.PropertyName,
            PropertyPhone = request.PropertyPhone,
            SuperintendentFullName = request.SuperintendentFullName,
            SuperintendentEmail = request.SuperintendentEmail,
            
            // Work assignment information
            AssignedWorkerEmail = request.AssignedWorkerEmail,
            AssignedWorkerName = request.AssignedWorkerName,
            WorkOrderNumber = request.WorkOrderNumber,
            WorkCompletedSuccessfully = request.WorkCompletedSuccessfully,
            CompletionNotes = request.CompletionNotes,
            ClosureNotes = request.ClosureNotes,
            PreferredContactTime = request.PreferredContactTime,

            // Business context from domain service
            CanEdit = businessContext.CanEdit,
            CanCancel = businessContext.CanCancel,
            CanAssignWorker = businessContext.AvailableActions.Contains(RequestAction.AssignWorker),
            AvailableActions = businessContext.AvailableActions.Select(a => a.ToString()).ToList(),
            NextAllowedStatus = GetNextAllowedStatusFromRecommendations(businessContext.WorkflowRecommendations)
        };

        return detailsDto;
    }

    private RentalRepairs.Domain.Services.Models.RequestFilterCriteria MapToRequestFilterCriteria(RequestFilterOptions? filters) // ? FIXED: Use fully qualified name to avoid ambiguity
    {
        if (filters == null) return new RentalRepairs.Domain.Services.Models.RequestFilterCriteria();

        var criteria = new RentalRepairs.Domain.Services.Models.RequestFilterCriteria
        {
            FromDate = filters.FromDate,
            ToDate = filters.ToDate
        };

        if (!string.IsNullOrEmpty(filters.Status) && 
            Enum.TryParse<TenantRequestStatus>(filters.Status, out var statusEnum))
        {
            criteria.Statuses = new List<TenantRequestStatus> { statusEnum };
        }

        if (!string.IsNullOrEmpty(filters.UrgencyLevel))
        {
            criteria.UrgencyLevels = new List<string> { filters.UrgencyLevel };
        }

        return criteria;
    }

    private TenantRequestSummaryDto MapToSummaryDto(TenantRequestDto request)
    {
        return new TenantRequestSummaryDto
        {
            Id = request.Id,
            Title = request.Title,
            Status = request.Status,
            UrgencyLevel = request.UrgencyLevel,
            CreatedDate = request.CreatedDate,
            ScheduledDate = request.ScheduledDate,
            PropertyName = request.PropertyName,
            TenantUnit = request.TenantUnit,
            IsEmergency = request.IsEmergency
        };
    }

    private string? GetNextAllowedStatusFromRecommendations(List<WorkflowRecommendation> recommendations)
    {
        var nextAction = recommendations
            .OrderBy(r => r.Priority)
            .FirstOrDefault();

        return nextAction?.Action.ToString();
    }

    private async Task<List<TenantRequestDto>> LoadFilteredRequestsAsync(
        RequestFilteringStrategy strategy, 
        CancellationToken cancellationToken)
    {
        var query = new Application.Queries.TenantRequests.GetTenantRequests.GetTenantRequestsQuery
        {
            PageSize = strategy.Criteria.MaxResults ?? 100,
            PageNumber = 1
        };

        var results = await _mediator.Send(query, cancellationToken);
        var filteredResults = results.ToList();

        // Apply domain-driven filtering
        if (!strategy.IncludeAllRequests)
        {
            if (!string.IsNullOrEmpty(strategy.FilterByTenantEmail))
            {
                filteredResults = filteredResults
                    .Where(r => r.TenantEmail?.Equals(strategy.FilterByTenantEmail, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(strategy.FilterByAssignedWorker))
            {
                filteredResults = filteredResults
                    .Where(r => r.AssignedWorkerEmail?.Equals(strategy.FilterByAssignedWorker, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            if (strategy.IncludeStatuses.Any())
            {
                var statusStrings = strategy.IncludeStatuses.Select(s => s.ToString()).ToList();
                filteredResults = filteredResults
                    .Where(r => statusStrings.Contains(r.Status.ToString()))
                    .ToList();
            }
        }

        return filteredResults;
    }

    #endregion
}