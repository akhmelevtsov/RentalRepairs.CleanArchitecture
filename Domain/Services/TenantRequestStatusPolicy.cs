using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service that centralizes all status-based business rules for tenant requests.
/// This moves business logic from Application layer to Domain layer for better separation.
/// Single source of truth for all status-related business decisions.
/// </summary>
public class TenantRequestStatusPolicy
{
    // Domain business rules - centralized from Application layer
    private static readonly Dictionary<TenantRequestStatus, List<TenantRequestStatus>> AllowedStatusTransitions = new()
    {
        [TenantRequestStatus.Draft] = new() { TenantRequestStatus.Submitted },
        [TenantRequestStatus.Submitted] = new() { TenantRequestStatus.Scheduled, TenantRequestStatus.Declined },
        [TenantRequestStatus.Scheduled] = new() { TenantRequestStatus.Done, TenantRequestStatus.Failed },
        [TenantRequestStatus.Failed] = new() { TenantRequestStatus.Scheduled },
        [TenantRequestStatus.Done] = new() { TenantRequestStatus.Closed },
        [TenantRequestStatus.Declined] = new() { TenantRequestStatus.Closed }
    };
    
    private static readonly Dictionary<TenantRequestStatus, string> StatusDisplayNames = new()
    {
        [TenantRequestStatus.Draft] = "Draft",
        [TenantRequestStatus.Submitted] = "Submitted",
        [TenantRequestStatus.Scheduled] = "Scheduled",
        [TenantRequestStatus.Done] = "Completed",
        [TenantRequestStatus.Failed] = "Failed",
        [TenantRequestStatus.Declined] = "Declined",
        [TenantRequestStatus.Closed] = "Closed"
    };
    
    private static readonly Dictionary<TenantRequestStatus, string> StatusCssClasses = new()
    {
        [TenantRequestStatus.Draft] = "badge-secondary",
        [TenantRequestStatus.Submitted] = "badge-info",
        [TenantRequestStatus.Scheduled] = "badge-primary",
        [TenantRequestStatus.Done] = "badge-success",
        [TenantRequestStatus.Failed] = "badge-warning",
        [TenantRequestStatus.Declined] = "badge-danger",
        [TenantRequestStatus.Closed] = "badge-dark"
    };
    
    private static readonly Dictionary<TenantRequestStatus, int> StatusPriorities = new()
    {
        [TenantRequestStatus.Failed] = 1,      // Highest priority - needs attention
        [TenantRequestStatus.Submitted] = 2,   // Needs assignment
        [TenantRequestStatus.Scheduled] = 3,   // Active work
        [TenantRequestStatus.Draft] = 4,       // Pending submission
        [TenantRequestStatus.Done] = 5,        // Awaiting closure
        [TenantRequestStatus.Declined] = 6,    // Awaiting closure
        [TenantRequestStatus.Closed] = 7       // Complete
    };

    /// <summary>
    /// Business rule: Determines if a request can be edited in the given status.
    /// Moved from Application layer.
    /// </summary>
    public bool CanEditInStatus(TenantRequestStatus status)
    {
        // Business rule: Can edit if not in final states
        return status is not (TenantRequestStatus.Done or TenantRequestStatus.Closed or TenantRequestStatus.Declined);
    }

    /// <summary>
    /// Business rule: Determines if a request can be cancelled in the given status.
    /// Moved from Application layer.
    /// </summary>
    public bool CanCancelInStatus(TenantRequestStatus status)
    {
        // Business rule: Can cancel if not yet worked on or completed
        return status is TenantRequestStatus.Draft or TenantRequestStatus.Submitted;
    }

    /// <summary>
    /// Business rule: Determines if a worker can be assigned to a request in the given status.
    /// Moved from Application layer.
    /// </summary>
    public bool CanAssignWorkerInStatus(TenantRequestStatus status)
    {
        // Business rule: Can assign worker to submitted requests
        return status is TenantRequestStatus.Submitted;
    }

    /// <summary>
    /// Business rule: Determines if work can be scheduled for a request in the given status.
    /// Moved from Application layer.
    /// </summary>
    public bool CanScheduleWorkInStatus(TenantRequestStatus status)
    {
        // Business rule: Can schedule from submitted or reschedule from failed
        return status is TenantRequestStatus.Submitted or TenantRequestStatus.Failed;
    }

    /// <summary>
    /// Business rule: Determines if work can be completed for a request in the given status.
    /// Moved from Application layer.
    /// </summary>
    public bool CanCompleteWorkInStatus(TenantRequestStatus status)
    {
        // Business rule: Can complete work that is scheduled
        return status is TenantRequestStatus.Scheduled;
    }

    /// <summary>
    /// Business rule: Determines if a request can be declined in the given status.
    /// Moved from Application layer.
    /// </summary>
    public bool CanDeclineInStatus(TenantRequestStatus status)
    {
        // Business rule: Can decline submitted requests
        return status is TenantRequestStatus.Submitted;
    }

    /// <summary>
    /// Business rule: Determines if a request can be closed in the given status.
    /// Moved from Application layer.
    /// </summary>
    public bool CanCloseInStatus(TenantRequestStatus status)
    {
        // Business rule: Can close completed or declined requests
        return status is TenantRequestStatus.Done or TenantRequestStatus.Declined;
    }

    /// <summary>
    /// Validates if a status transition is allowed using domain business rules.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public bool IsValidStatusTransition(TenantRequestStatus fromStatus, TenantRequestStatus toStatus)
    {
        return AllowedStatusTransitions.ContainsKey(fromStatus) && 
               AllowedStatusTransitions[fromStatus].Contains(toStatus);
    }

    /// <summary>
    /// Gets all allowed next statuses from a given status.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public List<TenantRequestStatus> GetAllowedNextStatuses(TenantRequestStatus currentStatus)
    {
        return AllowedStatusTransitions.ContainsKey(currentStatus) 
            ? AllowedStatusTransitions[currentStatus] 
            : new List<TenantRequestStatus>();
    }

    /// <summary>
    /// Gets the business priority of a status (for ordering/sorting).
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public int GetStatusPriority(TenantRequestStatus status)
    {
        return StatusPriorities.ContainsKey(status) ? StatusPriorities[status] : int.MaxValue;
    }

    /// <summary>
    /// Determines if a status represents an active request.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public bool IsActiveStatus(TenantRequestStatus status)
    {
        // Business rule: Active means submitted or scheduled
        return status is TenantRequestStatus.Submitted or TenantRequestStatus.Scheduled;
    }

    /// <summary>
    /// Determines if a status represents a completed request.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public bool IsCompletedStatus(TenantRequestStatus status)
    {
        // Business rule: Completed means done or closed
        return status is TenantRequestStatus.Done or TenantRequestStatus.Closed;
    }

    /// <summary>
    /// Determines if a status represents a final state (no further transitions).
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public bool IsFinalStatus(TenantRequestStatus status)
    {
        // Business rule: Final states are closed and declined
        return status is TenantRequestStatus.Closed or TenantRequestStatus.Declined;
    }

    /// <summary>
    /// Gets user-friendly display name for a status.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public string GetStatusDisplayName(TenantRequestStatus status)
    {
        return StatusDisplayNames.ContainsKey(status) ? StatusDisplayNames[status] : status.ToString();
    }

    /// <summary>
    /// Gets CSS class for status display styling.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public string GetStatusCssClass(TenantRequestStatus status)
    {
        return StatusCssClasses.ContainsKey(status) ? StatusCssClasses[status] : "badge-secondary";
    }

    /// <summary>
    /// Parses string status to strongly-typed enum with validation.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public TenantRequestStatus ParseStatus(string statusString, TenantRequestStatus defaultStatus = TenantRequestStatus.Draft)
    {
        if (TryParseStatus(statusString, out TenantRequestStatus status))
        {
            return status;
        }
        return defaultStatus;
    }

    /// <summary>
    /// Safely tries to parse string status to enum.
    /// Moved from Application layer for centralized business logic.
    /// </summary>
    public bool TryParseStatus(string statusString, out TenantRequestStatus status)
    {
        if (string.IsNullOrWhiteSpace(statusString))
        {
            status = default;
            return false;
        }

        return Enum.TryParse<TenantRequestStatus>(statusString, true, out status);
    }

    /// <summary>
    /// Determines if a status requires immediate attention.
    /// Business logic for categorizing requests.
    /// </summary>
    public bool RequiresAttention(TenantRequestStatus status)
    {
        return status is TenantRequestStatus.Failed;
    }

    /// <summary>
    /// Gets the category of the status for logical grouping.
    /// Business logic moved from Application layer.
    /// </summary>
    public StatusCategory GetStatusCategory(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Draft => StatusCategory.Draft,
            TenantRequestStatus.Submitted => StatusCategory.Active,
            TenantRequestStatus.Scheduled => StatusCategory.InProgress,
            TenantRequestStatus.Done => StatusCategory.Completed,
            TenantRequestStatus.Failed => StatusCategory.InProgress,
            TenantRequestStatus.Declined => StatusCategory.Cancelled,
            TenantRequestStatus.Closed => StatusCategory.Completed,
            _ => StatusCategory.Draft
        };
    }

    /// <summary>
    /// Validates a status transition and provides detailed result.
    /// Enhanced business rule validation with detailed information.
    /// </summary>
    public StatusTransitionValidationResult ValidateStatusTransition(TenantRequestStatus fromStatus, TenantRequestStatus toStatus)
    {
        if (IsValidStatusTransition(fromStatus, toStatus))
        {
            return StatusTransitionValidationResult.Success();
        }

        List<TenantRequestStatus> allowedStatuses = GetAllowedNextStatuses(fromStatus);
        string message = $"Cannot transition from {GetStatusDisplayName(fromStatus)} to {GetStatusDisplayName(toStatus)}. " +
                     $"Allowed transitions: {string.Join(", ", allowedStatuses.Select(GetStatusDisplayName))}";

        return StatusTransitionValidationResult.Failure(message, allowedStatuses);
    }
}

/// <summary>
/// Status categories for business logic grouping.
/// Moved from Application layer.
/// </summary>
public enum StatusCategory
{
    Draft,
    Active,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Status transition validation result with detailed information.
/// Enhanced business rule validation result.
/// </summary>
public class StatusTransitionValidationResult
{
    public bool IsValid { get; }
    public string? ValidationMessage { get; }
    public List<TenantRequestStatus> AllowedStatuses { get; }
    
    public StatusTransitionValidationResult(bool isValid, string? validationMessage = null, List<TenantRequestStatus>? allowedStatuses = null)
    {
        IsValid = isValid;
        ValidationMessage = validationMessage;
        AllowedStatuses = allowedStatuses ?? new List<TenantRequestStatus>();
    }
    
    public static StatusTransitionValidationResult Success() => new(true);
    
    public static StatusTransitionValidationResult Failure(string message, List<TenantRequestStatus> allowedStatuses) => 
        new(false, message, allowedStatuses);
}
