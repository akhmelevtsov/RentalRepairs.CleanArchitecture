using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Services.Models;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Pure domain service for tenant request business policies.
/// Contains only business logic without infrastructure dependencies.
/// Replaces the repository-dependent TenantRequestBusinessService.
/// </summary>
public class TenantRequestPolicyService
{
    private readonly TenantRequestStatusPolicy _statusPolicy;
    private readonly RequestAuthorizationPolicy _authorizationPolicy;
    private readonly RequestWorkflowManager _workflowManager;
    private readonly TenantRequestUrgencyPolicy _urgencyPolicy;

    public TenantRequestPolicyService(
        TenantRequestStatusPolicy statusPolicy,
        RequestAuthorizationPolicy authorizationPolicy,
        RequestWorkflowManager workflowManager,
        TenantRequestUrgencyPolicy urgencyPolicy)
    {
        _statusPolicy = statusPolicy ?? throw new ArgumentNullException(nameof(statusPolicy));
        _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        _workflowManager = workflowManager ?? throw new ArgumentNullException(nameof(workflowManager));
        _urgencyPolicy = urgencyPolicy ?? throw new ArgumentNullException(nameof(urgencyPolicy));
    }

    /// <summary>
    /// Pure business logic: Validates workflow transition without data access.
    /// </summary>
    public WorkflowTransitionResult ValidateWorkflowTransition(
        TenantRequest request,
        TenantRequestStatus toStatus,
        string userRole,
        string? reason = null,
        Dictionary<string, object>? metadata = null)
    {
        if (request == null)
        {
            return WorkflowTransitionResult.Failure(
                "Request cannot be null",
                WorkflowTransitionFailureReason.InvalidData);
        }

        return _workflowManager.ExecuteTransition(request, toStatus, userRole, reason, metadata);
    }

    /// <summary>
    /// Pure business logic: Validates user authorization without data access.
    /// </summary>
    public RequestAuthorizationResult ValidateUserAuthorization(
        TenantRequest request,
        string userRole,
        RequestAction action)
    {
        if (request == null)
        {
            return RequestAuthorizationResult.Failure("Request cannot be null");
        }

        bool canPerformAction = _authorizationPolicy.CanRolePerformAction(userRole, action, request.Status);
        if (!canPerformAction)
        {
            return RequestAuthorizationResult.Failure(
                $"User role '{userRole}' is not authorized to perform action '{action}' on request in status '{request.Status}'");
        }

        return RequestAuthorizationResult.Success();
    }

    /// <summary>
    /// Pure business logic: Generates business context from loaded entities.
    /// </summary>
    public TenantRequestBusinessContext GenerateBusinessContext(
        TenantRequest request,
        Tenant? tenant = null,
        string? userRole = null)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var context = new TenantRequestBusinessContext
        {
            Request = request,
            Tenant = tenant,
            WorkflowMetrics = _workflowManager.CalculateWorkflowMetrics(request),
            WorkflowIntegrity = _workflowManager.ValidateWorkflowIntegrity(request),
            EscalationRecommendation = _workflowManager.EvaluateEscalationNeed(request)
        };

        // Add role-specific context
        if (!string.IsNullOrEmpty(userRole))
        {
            context.AvailableActions = _authorizationPolicy.GetAvailableActionsForRole(userRole, request.Status);
            context.WorkflowRecommendations = _workflowManager.GetRecommendedNextActions(request, userRole);
            context.CanEdit = _authorizationPolicy.CanRoleEditRequestInStatus(userRole, request.Status);
            context.CanCancel = _authorizationPolicy.CanRoleCancelRequestInStatus(userRole, request.Status);
        }

        return context;
    }

    /// <summary>
    /// Pure business logic: Validates request submission business rules.
    /// </summary>
    public RequestSubmissionValidationResult ValidateRequestSubmission(
        RequestSubmissionRequest submissionRequest,
        Tenant tenant)
    {
        if (tenant == null)
        {
            return RequestSubmissionValidationResult.Failure("Tenant not found");
        }

        // Validate urgency level
        if (!_urgencyPolicy.IsUrgencyLevelAllowed(tenant, submissionRequest.UrgencyLevel))
        {
            return RequestSubmissionValidationResult.Failure(
                $"Urgency level '{submissionRequest.UrgencyLevel}' is not allowed for this tenant");
        }

        // Validate business constraints
        List<string> validationErrors = ValidateSubmissionBusinessRules(submissionRequest);
        if (validationErrors.Any())
        {
            return RequestSubmissionValidationResult.Failure(string.Join("; ", validationErrors));
        }

        return RequestSubmissionValidationResult.Success();
    }

    /// <summary>
    /// Pure business logic: Generates filtering strategy based on user role.
    /// </summary>
    public RequestFilteringStrategy GenerateFilteringStrategy(
        string userRole,
        string? userEmail = null,
        RentalRepairs.Domain.Services.Models.RequestFilterCriteria? criteria = null) // ✅ FIXED: Use fully qualified name
    {
        var strategy = new RequestFilteringStrategy
        {
            UserRole = userRole,
            UserEmail = userEmail,
            Criteria = criteria ?? new RentalRepairs.Domain.Services.Models.RequestFilterCriteria() // ✅ FIXED: Use fully qualified name
        };

        // Apply role-based filtering rules
        switch (userRole.ToLowerInvariant())
        {
            case "tenant":
                strategy.FilterByTenantEmail = userEmail;
                strategy.IncludeStatuses = GetTenantAllowedStatuses();
                break;

            case "worker":
                strategy.FilterByAssignedWorker = userEmail;
                strategy.IncludeStatuses = GetWorkerRelevantStatuses();
                break;

            case "propertysuperintendent":
                strategy.IncludeStatuses = GetSuperintendentAllowedStatuses();
                break;

            case "systemadmin":
                strategy.IncludeAllRequests = true;
                break;

            default:
                strategy.IncludeStatuses = new List<TenantRequestStatus>();
                break;
        }

        // Apply business-driven sorting
        strategy.SortingRules = GenerateSortingRules(userRole);
        strategy.PriorityBoosts = GeneratePriorityBoosts(userRole);

        return strategy;
    }

    /// <summary>
    /// Pure business logic: Analyzes request performance from loaded data.
    /// </summary>
    public RequestPerformanceAnalysis AnalyzeRequestPerformance(TenantRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        WorkflowMetrics metrics = _workflowManager.CalculateWorkflowMetrics(request);
        EscalationRecommendation escalationRecommendation = _workflowManager.EvaluateEscalationNeed(request);
        
        var analysis = new RequestPerformanceAnalysis
        {
            RequestId = request.Id,
            Metrics = metrics,
            PerformanceScore = CalculatePerformanceScore(metrics, request),
            Bottlenecks = IdentifyBottlenecks(request, metrics),
            ImprovementRecommendations = GenerateImprovementRecommendations(request, metrics),
            EscalationStatus = escalationRecommendation,
            ComplianceStatus = EvaluateComplianceStatus(request, metrics)
        };

        return analysis;
    }

    #region Private Helper Methods - Pure Business Logic

    private List<string> ValidateSubmissionBusinessRules(RequestSubmissionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title is required");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("Description is required");
        }

        if (request.Description?.Length > 2000)
        {
            errors.Add("Description cannot exceed 2000 characters");
        }

        if (string.IsNullOrWhiteSpace(request.PropertyCode))
        {
            errors.Add("Property code is required");
        }

        if (string.IsNullOrWhiteSpace(request.UnitNumber))
        {
            errors.Add("Unit number is required");
        }

        // Validate urgency level enum
        if (!Enum.TryParse<TenantRequestUrgency>(request.UrgencyLevel, out _))
        {
            errors.Add("Invalid urgency level");
        }

        return errors;
    }

    private List<TenantRequestStatus> GetTenantAllowedStatuses()
    {
        return new List<TenantRequestStatus>
        {
            TenantRequestStatus.Draft,
            TenantRequestStatus.Submitted,
            TenantRequestStatus.Scheduled,
            TenantRequestStatus.Done,
            TenantRequestStatus.Closed,
            TenantRequestStatus.Failed
        };
    }

    private List<TenantRequestStatus> GetWorkerRelevantStatuses()
    {
        return new List<TenantRequestStatus>
        {
            TenantRequestStatus.Scheduled,
            TenantRequestStatus.Done,
            TenantRequestStatus.Failed
        };
    }

    private List<TenantRequestStatus> GetSuperintendentAllowedStatuses()
    {
        return Enum.GetValues<TenantRequestStatus>().ToList();
    }

    private List<RequestSortingRule> GenerateSortingRules(string userRole)
    {
        return userRole.ToLowerInvariant() switch
        {
            "worker" => new List<RequestSortingRule>
            {
                new() { Field = "ScheduledDate", Order = SortOrder.Ascending },
                new() { Field = "UrgencyLevel", Order = SortOrder.Descending },
                new() { Field = "CreatedAt", Order = SortOrder.Ascending }
            },
            "propertysuperintendent" => new List<RequestSortingRule>
            {
                new() { Field = "UrgencyLevel", Order = SortOrder.Descending },
                new() { Field = "Status", Order = SortOrder.Ascending },
                new() { Field = "CreatedAt", Order = SortOrder.Ascending }
            },
            _ => new List<RequestSortingRule>
            {
                new() { Field = "CreatedAt", Order = SortOrder.Descending }
            }
        };
    }

    private List<PriorityBoost> GeneratePriorityBoosts(string userRole)
    {
        var boosts = new List<PriorityBoost>();

        if (userRole.Equals("worker", StringComparison.OrdinalIgnoreCase))
        {
            boosts.Add(new PriorityBoost { Condition = "IsEmergency", BoostFactor = 2.0 });
            boosts.Add(new PriorityBoost { Condition = "Status=Failed", BoostFactor = 1.5 });
        }

        return boosts;
    }

    private double CalculatePerformanceScore(WorkflowMetrics metrics, TenantRequest request)
    {
        double baseScore = metrics.EfficiencyScore;
        
        if (request.IsEmergency && metrics.TotalProcessingTime.TotalHours <= 2)
        {
            baseScore += 10;
        }

        if (metrics.StatusTransitionCount > 3)
        {
            baseScore -= (metrics.StatusTransitionCount - 3) * 5;
        }

        return Math.Max(0, Math.Min(100, baseScore));
    }

    private List<string> IdentifyBottlenecks(TenantRequest request, WorkflowMetrics metrics)
    {
        var bottlenecks = new List<string>();

        if (metrics.TimeInCurrentStatus.TotalDays > 3)
        {
            bottlenecks.Add($"Request has been in {request.Status} status for {metrics.TimeInCurrentStatus.TotalDays:F1} days");
        }

        if (request.Status == TenantRequestStatus.Submitted && metrics.TimeInCurrentStatus.TotalHours > 24)
        {
            bottlenecks.Add("Request assignment is delayed");
        }

        if (metrics.StatusTransitionCount > 5)
        {
            bottlenecks.Add("Excessive status transitions indicate process issues");
        }

        return bottlenecks;
    }

    private List<string> GenerateImprovementRecommendations(TenantRequest request, WorkflowMetrics metrics)
    {
        var recommendations = new List<string>();

        if (metrics.IsOverdue)
        {
            recommendations.Add("Consider escalating to supervisor for expedited processing");
        }

        if (request.Status == TenantRequestStatus.Failed && metrics.StatusTransitionCount >= 2)
        {
            recommendations.Add("Assign to senior worker with specialized skills");
        }

        if (metrics.TimeInCurrentStatus.TotalDays > 7)
        {
            recommendations.Add("Review resource allocation for this type of request");
        }

        return recommendations;
    }

    private ComplianceStatus EvaluateComplianceStatus(TenantRequest request, WorkflowMetrics metrics)
    {
        int expectedHours = _urgencyPolicy.GetExpectedResolutionHours(request.UrgencyLevel);
        double actualHours = metrics.TotalProcessingTime.TotalHours;

        if (actualHours <= expectedHours)
        {
            return ComplianceStatus.Compliant;
        }

        if (actualHours <= expectedHours * 1.5)
        {
            return ComplianceStatus.Warning;
        }

        return ComplianceStatus.NonCompliant;
    }

    #endregion
}
