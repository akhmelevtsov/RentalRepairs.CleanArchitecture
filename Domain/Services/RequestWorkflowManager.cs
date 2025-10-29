using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Common;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for complex request workflow management and cross-cutting business rules.
/// PHASE 2 MIGRATION: Moves workflow business logic from Application layer to Domain layer.
/// Manages complex request lifecycle and cross-aggregate coordination.
/// </summary>
public class RequestWorkflowManager
{
    private readonly TenantRequestStatusPolicy _statusPolicy;
    private readonly RequestAuthorizationPolicy _authorizationPolicy;
    private readonly TenantRequestUrgencyPolicy _urgencyPolicy;

    public RequestWorkflowManager(
        TenantRequestStatusPolicy statusPolicy,
        RequestAuthorizationPolicy authorizationPolicy,
        TenantRequestUrgencyPolicy urgencyPolicy)
    {
        _statusPolicy = statusPolicy ?? throw new ArgumentNullException(nameof(statusPolicy));
        _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        _urgencyPolicy = urgencyPolicy ?? throw new ArgumentNullException(nameof(urgencyPolicy));
    }

    /// <summary>
    /// Business rule: Validates and executes a request state transition.
    /// Combines status validation, authorization, and business rule enforcement.
    /// </summary>
    public WorkflowTransitionResult ExecuteTransition(
        TenantRequest request,
        TenantRequestStatus toStatus,
        string userRole,
        string? reason = null,
        Dictionary<string, object>? metadata = null)
    {
        // Validate status transition
        if (!_statusPolicy.IsValidStatusTransition(request.Status, toStatus))
        {
            return WorkflowTransitionResult.Failure(
                $"Invalid status transition from {request.Status} to {toStatus}",
                WorkflowTransitionFailureReason.InvalidStatusTransition);
        }

        // Validate authorization
        if (!_authorizationPolicy.CanRoleTransitionStatus(userRole, request.Status, toStatus))
        {
            return WorkflowTransitionResult.Failure(
                $"User role '{userRole}' is not authorized to transition from {request.Status} to {toStatus}",
                WorkflowTransitionFailureReason.InsufficientPermissions);
        }

        // Execute transition based on target status
        try
        {
            WorkflowTransitionResult result = ExecuteStatusSpecificTransition(request, toStatus, reason, metadata);
            
            if (result.IsSuccess)
            {
                // Record the transition for audit
                RecordWorkflowTransition(request, toStatus, userRole, reason);
            }

            return result;
        }
        catch (TenantRequestDomainException ex)
        {
            return WorkflowTransitionResult.Failure(ex.Message, WorkflowTransitionFailureReason.BusinessRuleViolation);
        }
    }

    /// <summary>
    /// Business rule: Determines next recommended actions for a request.
    /// Provides intelligent workflow guidance based on request state and context.
    /// </summary>
    public List<WorkflowRecommendation> GetRecommendedNextActions(TenantRequest request, string userRole)
    {
        var recommendations = new List<WorkflowRecommendation>();
        List<RequestAction> availableActions = _authorizationPolicy.GetAvailableActionsForRole(userRole, request.Status);

        foreach (RequestAction action in availableActions)
        {
            WorkflowRecommendation? recommendation = CreateActionRecommendation(request, action, userRole);
            if (recommendation != null)
            {
                recommendations.Add(recommendation);
            }
        }

        // Sort by priority (most important actions first)
        return recommendations.OrderBy(r => r.Priority).ToList();
    }

    /// <summary>
    /// Business rule: Validates request workflow integrity.
    /// Checks for workflow violations and inconsistencies.
    /// </summary>
    public WorkflowIntegrityResult ValidateWorkflowIntegrity(TenantRequest request)
    {
        var issues = new List<string>();

        // Check basic status consistency
        if (!IsStatusConsistent(request))
        {
            issues.Add("Request status is inconsistent with other properties");
        }

        // Check urgency level consistency
        if (!IsUrgencyLevelConsistent(request))
        {
            issues.Add("Urgency level is inconsistent with request details");
        }

        // Check assignment consistency
        if (!IsAssignmentConsistent(request))
        {
            issues.Add("Worker assignment is inconsistent with request status");
        }

        // Check timing constraints
        List<string> timingIssues = ValidateTimingConstraints(request);
        issues.AddRange(timingIssues);

        return new WorkflowIntegrityResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }

    /// <summary>
    /// Business rule: Determines if a request should be escalated.
    /// Implements escalation criteria based on various factors.
    /// </summary>
    public EscalationRecommendation EvaluateEscalationNeed(TenantRequest request)
    {
        var reasons = new List<string>();
        EscalationUrgency urgencyLevel = EscalationUrgency.None;

        // Time-based escalation
        TimeSpan timeInStatus = DateTime.UtcNow - request.CreatedAt;
        int expectedResolutionHours = _urgencyPolicy.GetExpectedResolutionHours(request.UrgencyLevel);
        
        if (timeInStatus.TotalHours > expectedResolutionHours)
        {
            reasons.Add($"Request has exceeded expected resolution time of {expectedResolutionHours} hours");
            urgencyLevel = EscalationUrgency.Medium;
        }

        // Status-based escalation
        if (request.Status == TenantRequestStatus.Failed)
        {
            reasons.Add("Request has failed and needs attention");
            urgencyLevel = EscalationUrgency.High;
        }

        // Emergency escalation
        if (request.IsEmergency && request.Status == TenantRequestStatus.Submitted)
        {
            TimeSpan emergencyWaitTime = DateTime.UtcNow - request.CreatedAt;
            if (emergencyWaitTime.TotalHours > 1) // Emergency should be assigned within 1 hour
            {
                reasons.Add("Emergency request has not been assigned within 1 hour");
                urgencyLevel = EscalationUrgency.Critical;
            }
        }

        // Multiple attempts escalation
        if (CountStatusTransitions(request, TenantRequestStatus.Failed) >= 2)
        {
            reasons.Add("Request has failed multiple times");
            urgencyLevel = EscalationUrgency.High;
        }

        return new EscalationRecommendation
        {
            ShouldEscalate = reasons.Any(),
            Urgency = urgencyLevel,
            Reasons = reasons,
            RecommendedAction = GetEscalationAction(urgencyLevel)
        };
    }

    /// <summary>
    /// Business rule: Gets workflow metrics for analytics and monitoring.
    /// Provides insights into request processing efficiency.
    /// </summary>
    public WorkflowMetrics CalculateWorkflowMetrics(TenantRequest request)
    {
        DateTime createdAt = request.CreatedAt;
        DateTime now = DateTime.UtcNow;

        return new WorkflowMetrics
        {
            TotalProcessingTime = now - createdAt,
            TimeInCurrentStatus = now - GetLastStatusChangeDate(request),
            StatusTransitionCount = CountTotalStatusTransitions(request),
            IsOverdue = IsRequestOverdue(request),
            EfficiencyScore = CalculateEfficiencyScore(request)
        };
    }

    #region Private Helper Methods

    private WorkflowTransitionResult ExecuteStatusSpecificTransition(
        TenantRequest request,
        TenantRequestStatus toStatus,
        string? reason,
        Dictionary<string, object>? metadata)
    {
        return toStatus switch
        {
            TenantRequestStatus.Submitted => ExecuteSubmitTransition(request),
            TenantRequestStatus.Declined => ExecuteDeclineTransition(request, reason),
            TenantRequestStatus.Scheduled => ExecuteScheduleTransition(request, metadata),
            TenantRequestStatus.Done => ExecuteCompleteTransition(request, reason),
            TenantRequestStatus.Failed => ExecuteFailTransition(request, reason),
            TenantRequestStatus.Closed => ExecuteCloseTransition(request, reason),
            _ => WorkflowTransitionResult.Failure($"Unsupported transition to {toStatus}", WorkflowTransitionFailureReason.InvalidStatusTransition)
        };
    }

    private WorkflowTransitionResult ExecuteSubmitTransition(TenantRequest request)
    {
        request.SubmitForReview();
        return WorkflowTransitionResult.Success("Request submitted for review");
    }

    private WorkflowTransitionResult ExecuteDeclineTransition(TenantRequest request, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return WorkflowTransitionResult.Failure("Reason is required for declining a request", WorkflowTransitionFailureReason.MissingRequiredData);
        }

        request.DeclineRequest(reason);
        return WorkflowTransitionResult.Success($"Request declined: {reason}");
    }

    private WorkflowTransitionResult ExecuteScheduleTransition(TenantRequest request, Dictionary<string, object>? metadata)
    {
        if (metadata == null || 
            !metadata.TryGetValue("scheduledDate", out object? scheduledDateObj) ||
            !metadata.TryGetValue("workerEmail", out object? workerEmailObj) ||
            !metadata.TryGetValue("workOrderNumber", out object? workOrderObj))
        {
            return WorkflowTransitionResult.Failure("Scheduling requires scheduledDate, workerEmail, and workOrderNumber", WorkflowTransitionFailureReason.MissingRequiredData);
        }

        if (scheduledDateObj is DateTime scheduledDate &&
            workerEmailObj is string workerEmail &&
            workOrderObj is string workOrderNumber)
        {
            string? workerName = metadata.TryGetValue("workerName", out object? workerNameObj) ? workerNameObj as string : null;
            request.ScheduleWork(scheduledDate, workerEmail, workOrderNumber, workerName);
            return WorkflowTransitionResult.Success($"Work scheduled for {scheduledDate:MMM dd, yyyy} with {workerEmail}");
        }

        return WorkflowTransitionResult.Failure("Invalid scheduling data provided", WorkflowTransitionFailureReason.InvalidData);
    }

    private WorkflowTransitionResult ExecuteCompleteTransition(TenantRequest request, string? reason)
    {
        request.ReportWorkCompleted(true, reason);
        return WorkflowTransitionResult.Success("Work completed successfully");
    }

    private WorkflowTransitionResult ExecuteFailTransition(TenantRequest request, string? reason)
    {
        request.ReportWorkCompleted(false, reason);
        return WorkflowTransitionResult.Success($"Work reported as failed: {reason}");
    }

    private WorkflowTransitionResult ExecuteCloseTransition(TenantRequest request, string? reason)
    {
        request.Close(reason ?? "Request closed");
        return WorkflowTransitionResult.Success("Request closed");
    }

    private void RecordWorkflowTransition(TenantRequest request, TenantRequestStatus toStatus, string userRole, string? reason)
    {
        // In a full implementation, this would record the transition for audit purposes
        // For now, we rely on domain events
        var transitionEvent = new TenantRequestStatusTransitionEvent(request, toStatus, userRole, reason);
        request.AddDomainEvent(transitionEvent);
    }

    private WorkflowRecommendation? CreateActionRecommendation(TenantRequest request, RequestAction action, string userRole)
    {
        int priority = GetActionPriority(action, request);
        string description = GetActionDescription(action, request);
        ActionUrgency urgency = GetActionUrgency(action, request);

        return new WorkflowRecommendation
        {
            Action = action,
            Priority = priority,
            Description = description,
            Urgency = urgency,
            EstimatedEffort = GetEstimatedEffort(action)
        };
    }

    private int GetActionPriority(RequestAction action, TenantRequest request)
    {
        return action switch
        {
            RequestAction.CompleteWork when request.IsEmergency => 1,
            RequestAction.AssignWorker when request.Status == TenantRequestStatus.Submitted => 2,
            RequestAction.Submit when request.Status == TenantRequestStatus.Draft => 3,
            _ => 5
        };
    }

    private string GetActionDescription(RequestAction action, TenantRequest request)
    {
        return action switch
        {
            RequestAction.Submit => "Submit request for review",
            RequestAction.AssignWorker => "Assign a worker to handle this request",
            RequestAction.Schedule => "Schedule work to be performed",
            RequestAction.CompleteWork => "Mark work as completed",
            RequestAction.Decline => "Decline this request",
            RequestAction.Close => "Close the request",
            _ => action.ToString()
        };
    }

    private ActionUrgency GetActionUrgency(RequestAction action, TenantRequest request)
    {
        if (request.IsEmergency)
        {
            return ActionUrgency.High;
        }

        return action switch
        {
            RequestAction.AssignWorker when request.Status == TenantRequestStatus.Submitted => ActionUrgency.Medium,
            RequestAction.CompleteWork when request.Status == TenantRequestStatus.Scheduled => ActionUrgency.Medium,
            _ => ActionUrgency.Low
        };
    }

    private TimeSpan GetEstimatedEffort(RequestAction action)
    {
        return action switch
        {
            RequestAction.Submit => TimeSpan.FromMinutes(5),
            RequestAction.AssignWorker => TimeSpan.FromMinutes(15),
            RequestAction.Schedule => TimeSpan.FromMinutes(10),
            RequestAction.CompleteWork => TimeSpan.FromHours(2),
            _ => TimeSpan.FromMinutes(5)
        };
    }

    private bool IsStatusConsistent(TenantRequest request)
    {
        // Check if status aligns with other properties
        return request.Status switch
        {
            TenantRequestStatus.Scheduled => !string.IsNullOrEmpty(request.AssignedWorkerEmail),
            TenantRequestStatus.Done => request.CompletedDate.HasValue,
            TenantRequestStatus.Failed => request.CompletedDate.HasValue,
            _ => true
        };
    }

    private bool IsUrgencyLevelConsistent(TenantRequest request)
    {
        return _urgencyPolicy.IsValidUrgencyLevel(request.UrgencyLevel);
    }

    private bool IsAssignmentConsistent(TenantRequest request)
    {
        if (request.Status == TenantRequestStatus.Scheduled)
        {
            return !string.IsNullOrEmpty(request.AssignedWorkerEmail) && 
                   !string.IsNullOrEmpty(request.WorkOrderNumber);
        }

        return true;
    }

    private List<string> ValidateTimingConstraints(TenantRequest request)
    {
        var issues = new List<string>();

        if (request.ScheduledDate.HasValue && request.ScheduledDate <= request.CreatedAt)
        {
            issues.Add("Scheduled date cannot be before creation date");
        }

        if (request.CompletedDate.HasValue && request.CompletedDate <= request.CreatedAt)
        {
            issues.Add("Completed date cannot be before creation date");
        }

        return issues;
    }

    private int CountStatusTransitions(TenantRequest request, TenantRequestStatus status)
    {
        // In a full implementation, this would count transitions from audit history
        // Simplified implementation
        return 0;
    }

    private int CountTotalStatusTransitions(TenantRequest request)
    {
        // In a full implementation, this would count all transitions from audit history
        // Simplified implementation
        return 1;
    }

    private DateTime GetLastStatusChangeDate(TenantRequest request)
    {
        // In a full implementation, this would get the last status change from audit history
        // Simplified implementation
        return request.CreatedAt;
    }

    private bool IsRequestOverdue(TenantRequest request)
    {
        int expectedHours = _urgencyPolicy.GetExpectedResolutionHours(request.UrgencyLevel);
        TimeSpan timeInProcess = DateTime.UtcNow - request.CreatedAt;
        return timeInProcess.TotalHours > expectedHours;
    }

    private double CalculateEfficiencyScore(TenantRequest request)
    {
        // Calculate efficiency based on time in process vs expected time
        int expectedHours = _urgencyPolicy.GetExpectedResolutionHours(request.UrgencyLevel);
        double actualHours = (DateTime.UtcNow - request.CreatedAt).TotalHours;
        
        if (actualHours <= expectedHours)
        {
            return 100.0; // Perfect efficiency
        }

        return Math.Max(0, 100.0 - ((actualHours - expectedHours) / expectedHours * 100));
    }

    private string GetEscalationAction(EscalationUrgency urgency)
    {
        return urgency switch
        {
            EscalationUrgency.Critical => "Immediate supervisor notification required",
            EscalationUrgency.High => "Assign to senior worker or supervisor",
            EscalationUrgency.Medium => "Review and prioritize assignment",
            _ => "Monitor for resolution"
        };
    }

    #endregion
}

/// <summary>
/// Result of a workflow transition operation.
/// </summary>
public class WorkflowTransitionResult
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public WorkflowTransitionFailureReason? FailureReason { get; }

    private WorkflowTransitionResult(bool isSuccess, string message, WorkflowTransitionFailureReason? failureReason = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        FailureReason = failureReason;
    }

    public static WorkflowTransitionResult Success(string message) => new(true, message);
    public static WorkflowTransitionResult Failure(string message, WorkflowTransitionFailureReason reason) => new(false, message, reason);
}

/// <summary>
/// Reasons for workflow transition failures.
/// </summary>
public enum WorkflowTransitionFailureReason
{
    InvalidStatusTransition,
    InsufficientPermissions,
    BusinessRuleViolation,
    MissingRequiredData,
    InvalidData
}

/// <summary>
/// Recommendation for workflow actions.
/// </summary>
public class WorkflowRecommendation
{
    public RequestAction Action { get; set; }
    public int Priority { get; set; }
    public string Description { get; set; } = string.Empty;
    public ActionUrgency Urgency { get; set; }
    public TimeSpan EstimatedEffort { get; set; }
}

/// <summary>
/// Urgency levels for workflow actions.
/// </summary>
public enum ActionUrgency
{
    Low,
    Medium,
    High
}

/// <summary>
/// Result of workflow integrity validation.
/// </summary>
public class WorkflowIntegrityResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Recommendation for request escalation.
/// </summary>
public class EscalationRecommendation
{
    public bool ShouldEscalate { get; set; }
    public EscalationUrgency Urgency { get; set; }
    public List<string> Reasons { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// Urgency levels for escalation.
/// </summary>
public enum EscalationUrgency
{
    None,
    Medium,
    High,
    Critical
}

/// <summary>
/// Metrics for workflow performance analysis.
/// </summary>
public class WorkflowMetrics
{
    public TimeSpan TotalProcessingTime { get; set; }
    public TimeSpan TimeInCurrentStatus { get; set; }
    public int StatusTransitionCount { get; set; }
    public bool IsOverdue { get; set; }
    public double EfficiencyScore { get; set; }
}

/// <summary>
/// Domain event for status transitions.
/// </summary>
public class TenantRequestStatusTransitionEvent : BaseEvent
{
    public TenantRequest Request { get; }
    public TenantRequestStatus ToStatus { get; }
    public string UserRole { get; }
    public string? Reason { get; }

    public TenantRequestStatusTransitionEvent(TenantRequest request, TenantRequestStatus toStatus, string userRole, string? reason)
    {
        Request = request;
        ToStatus = toStatus;
        UserRole = userRole;
        Reason = reason;
    }
}
