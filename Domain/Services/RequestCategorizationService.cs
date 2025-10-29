using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Specifications.TenantRequests;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for categorizing and filtering tenant requests based on business rules.
/// Moves request categorization logic from Application layer to Domain layer.
/// Provides business-driven request grouping and filtering capabilities.
/// </summary>
public class RequestCategorizationService
{
    private readonly ITenantRequestRepository _tenantRequestRepository;

    public RequestCategorizationService(ITenantRequestRepository tenantRequestRepository)
    {
        _tenantRequestRepository = tenantRequestRepository ?? throw new ArgumentNullException(nameof(tenantRequestRepository));
    }

    /// <summary>
    /// Business rule: Categorizes requests as pending based on status.
    /// Moved from Application layer RequestManagementService.
    /// </summary>
    public async Task<List<TenantRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var spec = new TenantRequestByMultipleStatusSpecification(TenantRequestStatus.Draft, TenantRequestStatus.Submitted);
        return (await _tenantRequestRepository.GetBySpecificationAsync(spec, cancellationToken)).ToList();
    }

    /// <summary>
    /// Business rule: Categorizes requests as emergency based on urgency level and emergency flag.
    /// Moved from Application layer RequestManagementService.
    /// </summary>
    public async Task<List<TenantRequest>> GetEmergencyRequestsAsync(CancellationToken cancellationToken = default)
    {
        var spec = new TenantRequestEmergencySpecification();
        IEnumerable<TenantRequest> emergencyByFlag = await _tenantRequestRepository.GetBySpecificationAsync(spec, cancellationToken);
        
        var criticalSpec = new TenantRequestsByUrgencySpecification("Critical");
        IEnumerable<TenantRequest> criticalRequests = await _tenantRequestRepository.GetBySpecificationAsync(criticalSpec, cancellationToken);
        
        var highSpec = new TenantRequestsByUrgencySpecification("High");
        IEnumerable<TenantRequest> highRequests = await _tenantRequestRepository.GetBySpecificationAsync(highSpec, cancellationToken);

        // Combine all emergency/high-priority requests
        var allEmergencyRequests = emergencyByFlag
            .Concat(criticalRequests)
            .Concat(highRequests)
            .Where(r => !IsCompletedStatus(r.Status))
            .Distinct()
            .ToList();

        return allEmergencyRequests;
    }

    /// <summary>
    /// Business rule: Categorizes requests as overdue based on scheduled date.
    /// Moved from Application layer RequestManagementService.
    /// </summary>
    public async Task<List<TenantRequest>> GetOverdueRequestsAsync(CancellationToken cancellationToken = default)
    {
        var spec = new TenantRequestOverdueSpecification(3); // 3 days threshold for overdue requests
        var overdueRequests = (await _tenantRequestRepository.GetBySpecificationAsync(spec, cancellationToken))
            .Where(r => !IsCompletedStatus(r.Status))
            .ToList();

        return overdueRequests;
    }

    /// <summary>
    /// Business rule: Gets requests requiring attention based on various criteria.
    /// Combines failed requests and old submitted requests.
    /// </summary>
    public async Task<List<TenantRequest>> GetRequestsRequiringAttentionAsync(CancellationToken cancellationToken = default)
    {
        var failedSpec = new TenantRequestByStatusSpecification(TenantRequestStatus.Failed);
        IEnumerable<TenantRequest> failedRequests = await _tenantRequestRepository.GetBySpecificationAsync(failedSpec, cancellationToken);

        var submittedSpec = new TenantRequestByStatusSpecification(TenantRequestStatus.Submitted);
        IEnumerable<TenantRequest> submittedRequests = await _tenantRequestRepository.GetBySpecificationAsync(submittedSpec, cancellationToken);

        // Business rule: Submitted requests older than 2 days need attention
        IEnumerable<TenantRequest> oldSubmittedRequests = submittedRequests.Where(r => 
            DateTime.UtcNow.Subtract(r.CreatedAt).TotalDays >= 2);

        return failedRequests.Concat(oldSubmittedRequests).Distinct().ToList();
    }

    /// <summary>
    /// Business rule: Categorizes requests by urgency level.
    /// Provides business-driven request prioritization.
    /// </summary>
    public async Task<Dictionary<string, List<TenantRequest>>> GetRequestsByUrgencyAsync(CancellationToken cancellationToken = default)
    {
        string[] urgencyLevels = new[] { "Emergency", "Critical", "High", "Normal", "Low" };
        var result = new Dictionary<string, List<TenantRequest>>();
        
        foreach (string? urgency in urgencyLevels)
        {
            var spec = new TenantRequestsByUrgencySpecification(urgency);
            IEnumerable<TenantRequest> requests = await _tenantRequestRepository.GetBySpecificationAsync(spec, cancellationToken);
            result[urgency] = requests.Where(r => !IsCompletedStatus(r.Status)).ToList();
        }

        return result;
    }

    /// <summary>
    /// Business rule: Categorizes requests by status category for management views.
    /// Provides organized request grouping for different user roles.
    /// </summary>
    public async Task<Dictionary<StatusCategory, List<TenantRequest>>> GetRequestsByStatusCategoryAsync(CancellationToken cancellationToken = default)
    {
        List<TenantRequest> allActiveRequests = await GetAllActiveRequestsAsync(cancellationToken);
        
        var result = new Dictionary<StatusCategory, List<TenantRequest>>();
        
        foreach (StatusCategory category in Enum.GetValues<StatusCategory>())
        {
            result[category] = allActiveRequests
                .Where(r => GetStatusCategory(r.Status) == category)
                .ToList();
        }

        return result;
    }

    /// <summary>
    /// Business rule: Gets all active requests (not completed or closed).
    /// Base filter for other categorization methods.
    /// </summary>
    public async Task<List<TenantRequest>> GetAllActiveRequestsAsync(CancellationToken cancellationToken = default)
    {
        TenantRequestStatus[] activeStatuses = new[]
        {
            TenantRequestStatus.Draft,
            TenantRequestStatus.Submitted,
            TenantRequestStatus.Scheduled,
            TenantRequestStatus.Failed
        };

        var spec = new TenantRequestByMultipleStatusSpecification(activeStatuses);
        IEnumerable<TenantRequest> requests = await _tenantRequestRepository.GetBySpecificationAsync(spec, cancellationToken);
        return requests.ToList();
    }

    /// <summary>
    /// Business rule: Filters requests based on multiple criteria.
    /// Consolidated filtering logic moved from Application layer.
    /// </summary>
    public List<TenantRequest> ApplyBusinessFilters(
        List<TenantRequest> requests,
        RequestFilterCriteria? criteria = null)
    {
        if (criteria == null)
        {
            return requests;
        }

        IEnumerable<TenantRequest> filtered = requests.AsEnumerable();

        // Filter by status
        if (criteria.Status.HasValue)
        {
            filtered = filtered.Where(r => r.Status == criteria.Status.Value);
        }

        // Filter by urgency level
        if (!string.IsNullOrEmpty(criteria.UrgencyLevel) && criteria.UrgencyLevel != "All")
        {
            filtered = filtered.Where(r => r.UrgencyLevel.Equals(criteria.UrgencyLevel, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by date range
        if (criteria.FromDate.HasValue)
        {
            filtered = filtered.Where(r => r.CreatedAt >= criteria.FromDate.Value);
        }

        if (criteria.ToDate.HasValue)
        {
            filtered = filtered.Where(r => r.CreatedAt <= criteria.ToDate.Value);
        }

        // Filter by emergency flag
        if (criteria.EmergencyOnly == true)
        {
            filtered = filtered.Where(r => r.IsEmergency);
        }

        // Filter by property
        if (criteria.PropertyId.HasValue)
        {
            filtered = filtered.Where(r => r.PropertyId == criteria.PropertyId.Value);
        }

        return filtered.ToList();
    }

    #region Private Helper Methods

    /// <summary>
    /// Business logic: Determines if a status represents a completed request.
    /// </summary>
    private static bool IsCompletedStatus(TenantRequestStatus status)
    {
        return status is TenantRequestStatus.Done or TenantRequestStatus.Closed or TenantRequestStatus.Declined;
    }

    /// <summary>
    /// Business logic: Gets the category of a status for logical grouping.
    /// </summary>
    private static StatusCategory GetStatusCategory(TenantRequestStatus status)
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

    #endregion
}

/// <summary>
/// Criteria for filtering requests with business rules.
/// Consolidates filtering logic from Application layer.
/// </summary>
public class RequestFilterCriteria
{
    public TenantRequestStatus? Status { get; set; }
    public string? UrgencyLevel { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? EmergencyOnly { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? TenantId { get; set; }
    public string? AssignedWorkerEmail { get; set; }
}

/// <summary>
/// Statistics about categorized requests for reporting.
/// Business logic for request analytics.
/// </summary>
public class RequestCategoryStatistics
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int EmergencyRequests { get; set; }
    public int OverdueRequests { get; set; }
    public int RequiringAttention { get; set; }
    public Dictionary<string, int> RequestsByUrgency { get; set; } = new();
    public Dictionary<StatusCategory, int> RequestsByCategory { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
