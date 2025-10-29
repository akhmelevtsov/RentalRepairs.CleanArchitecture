using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Services.Models;

/// <summary>
/// Request authorization result with detailed information.
/// </summary>
public class RequestAuthorizationResult
{
    public bool IsAuthorized { get; }
    public string? ErrorMessage { get; }
    public List<string> Reasons { get; }

    private RequestAuthorizationResult(bool isAuthorized, string? errorMessage = null, List<string>? reasons = null)
    {
        IsAuthorized = isAuthorized;
        ErrorMessage = errorMessage;
        Reasons = reasons ?? new List<string>();
    }

    public static RequestAuthorizationResult Success(List<string>? reasons = null) => 
        new(true, null, reasons);

    public static RequestAuthorizationResult Failure(string errorMessage) => 
        new(false, errorMessage);
}

/// <summary>
/// Request submission data transfer object.
/// </summary>
public class RequestSubmissionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string? PreferredContactTime { get; set; }
}

/// <summary>
/// Result of request submission validation.
/// </summary>
public class RequestSubmissionValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public List<string> ValidationErrors { get; }

    private RequestSubmissionValidationResult(bool isValid, string? errorMessage = null, List<string>? validationErrors = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new List<string>();
    }

    public static RequestSubmissionValidationResult Success() => new(true);
    public static RequestSubmissionValidationResult Failure(string errorMessage) => new(false, errorMessage);
    public static RequestSubmissionValidationResult Failure(List<string> validationErrors) => new(false, null, validationErrors);
}

/// <summary>
/// Filter criteria for request queries.
/// </summary>
public class RequestFilterCriteria
{
    public List<TenantRequestStatus>? Statuses { get; set; }
    public List<string>? UrgencyLevels { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? PropertyCode { get; set; }
    public string? AssignedWorkerEmail { get; set; }
    public bool? IsEmergency { get; set; }
    public int? MaxResults { get; set; }
}

/// <summary>
/// Strategy for filtering requests based on business rules.
/// </summary>
public class RequestFilteringStrategy
{
    public string UserRole { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public RequestFilterCriteria Criteria { get; set; } = new();
    public string? FilterByTenantEmail { get; set; }
    public string? FilterByAssignedWorker { get; set; }
    public List<string> FilterByProperty { get; set; } = new();
    public List<TenantRequestStatus> IncludeStatuses { get; set; } = new();
    public bool IncludeAllRequests { get; set; }
    public List<RequestSortingRule> SortingRules { get; set; } = new();
    public List<PriorityBoost> PriorityBoosts { get; set; } = new();
}

/// <summary>
/// Sorting rule for request queries.
/// </summary>
public class RequestSortingRule
{
    public string Field { get; set; } = string.Empty;
    public SortOrder Order { get; set; }
}

/// <summary>
/// Priority boost rule for request ranking.
/// </summary>
public class PriorityBoost
{
    public string Condition { get; set; } = string.Empty;
    public double BoostFactor { get; set; }
}

/// <summary>
/// Sort order enumeration.
/// </summary>
public enum SortOrder
{
    Ascending,
    Descending
}

/// <summary>
/// Comprehensive performance analysis for a request.
/// </summary>
public class RequestPerformanceAnalysis
{
    public Guid RequestId { get; set; }
    public WorkflowMetrics Metrics { get; set; } = new();
    public double PerformanceScore { get; set; }
    public List<string> Bottlenecks { get; set; } = new();
    public List<string> ImprovementRecommendations { get; set; } = new();
    public EscalationRecommendation EscalationStatus { get; set; } = new();
    public ComplianceStatus ComplianceStatus { get; set; }
}

/// <summary>
/// Compliance status enumeration.
/// </summary>
public enum ComplianceStatus
{
    Compliant,
    Warning,
    NonCompliant
}
