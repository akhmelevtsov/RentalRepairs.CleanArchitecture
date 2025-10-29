using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Exceptions;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Centralized domain policy service for tenant request submission business rules.
/// Eliminates code redundancy by consolidating all business rule validation in one place.
/// Supports configurable business rules from external configuration sources.
/// </summary>
public interface ITenantRequestSubmissionPolicy
{
    /// <summary>
    /// Validates all business rules for tenant request submission.
    /// </summary>
    void ValidateCanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency);

    /// <summary>
    /// Checks if tenant can submit requests without throwing exceptions.
    /// </summary>
    bool CanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency);

    /// <summary>
    /// Gets the next allowed submission time for the tenant.
    /// </summary>
    DateTime? GetNextAllowedSubmissionTime(Tenant tenant);

    /// <summary>
    /// Gets the remaining emergency requests for the current month.
    /// </summary>
    int GetRemainingEmergencyRequests(Tenant tenant);
}

/// <summary>
/// Default implementation of tenant request submission policy.
/// Uses configurable business rules instead of hard-coded constants.
/// </summary>
public class TenantRequestSubmissionPolicy : ITenantRequestSubmissionPolicy
{
    private readonly TenantRequestPolicyConfiguration _configuration;

    public TenantRequestSubmissionPolicy(TenantRequestPolicyConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Validates all business rules for tenant request submission.
    /// Throws domain exceptions for violations.
    /// </summary>
    public void ValidateCanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency)
    {
        if (tenant == null)
        {
            throw new ArgumentNullException(nameof(tenant));
        }

        // Business Rule 1: Maximum pending requests
        ValidateMaxPendingRequests(tenant);

        // Business Rule 2: Rate limiting between submissions (if enabled)
        if (_configuration.IsRateLimitingEnabled)
        {
            ValidateRateLimit(tenant);
        }

        // Business Rule 3: Emergency request limitations (if enabled)
        if (_configuration.IsEmergencyLimitingEnabled && urgency == TenantRequestUrgency.Emergency)
        {
            ValidateEmergencyRequestLimit(tenant);
        }
    }

    /// <summary>
    /// Checks if tenant can submit requests without throwing exceptions.
    /// </summary>
    public bool CanSubmitRequest(Tenant tenant, TenantRequestUrgency urgency)
    {
        try
        {
            ValidateCanSubmitRequest(tenant, urgency);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the next allowed submission time for the tenant.
    /// Returns null if no rate limiting or tenant can submit immediately.
    /// </summary>
    public DateTime? GetNextAllowedSubmissionTime(Tenant tenant)
    {
        if (!_configuration.IsRateLimitingEnabled)
        {
            return null;
        }

        TenantRequest? lastSubmission = tenant.Requests
            .Where(r => r.Status != TenantRequestStatus.Draft)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (lastSubmission == null)
        {
            return null;
        }

        DateTime nextAllowedTime = lastSubmission.CreatedAt.Add(_configuration.RateLimitTimeSpan);
        return nextAllowedTime > DateTime.UtcNow ? nextAllowedTime : null;
    }

    /// <summary>
    /// Gets the remaining emergency requests for the current period.
    /// </summary>
    public int GetRemainingEmergencyRequests(Tenant tenant)
    {
        if (!_configuration.IsEmergencyLimitingEnabled)
        {
            return int.MaxValue; // Unlimited if disabled
        }

        int emergencyRequestsInPeriod = tenant.Requests
            .Count(r => r.UrgencyLevel == "Emergency" && 
                       r.CreatedAt > DateTime.UtcNow.Subtract(_configuration.EmergencyLookbackTimeSpan));

        return Math.Max(0, _configuration.MaxEmergencyRequestsPerMonth - emergencyRequestsInPeriod);
    }

    #region Private Validation Methods

    /// <summary>
    /// Business rule: Tenant cannot have more than maximum pending requests.
    /// </summary>
    private void ValidateMaxPendingRequests(Tenant tenant)
    {
        int activeRequestsCount = tenant.Requests.Count(r => 
            r.Status is TenantRequestStatus.Submitted or TenantRequestStatus.Scheduled);

        if (activeRequestsCount >= _configuration.MaxPendingRequests)
        {
            throw new MaxPendingRequestsExceededException(_configuration.MaxPendingRequests, activeRequestsCount);
        }
    }

    /// <summary>
    /// Business rule: Tenant must wait minimum time between submissions.
    /// </summary>
    private void ValidateRateLimit(Tenant tenant)
    {
        TenantRequest? lastSubmission = tenant.Requests
            .Where(r => r.Status != TenantRequestStatus.Draft)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (lastSubmission != null)
        {
            TimeSpan timeSinceLastSubmission = DateTime.UtcNow - lastSubmission.CreatedAt;
            TimeSpan minimumWaitTime = _configuration.RateLimitTimeSpan;

            if (timeSinceLastSubmission < minimumWaitTime)
            {
                TimeSpan waitTime = minimumWaitTime - timeSinceLastSubmission;
                throw new SubmissionRateLimitExceededException(waitTime);
            }
        }
    }

    /// <summary>
    /// Business rule: Limit emergency requests per period to prevent abuse.
    /// </summary>
    private void ValidateEmergencyRequestLimit(Tenant tenant)
    {
        int emergencyRequestsInPeriod = tenant.Requests
            .Count(r => r.UrgencyLevel == "Emergency" && 
                       r.CreatedAt > DateTime.UtcNow.Subtract(_configuration.EmergencyLookbackTimeSpan));

        if (emergencyRequestsInPeriod >= _configuration.MaxEmergencyRequestsPerMonth)
        {
            throw new EmergencyRequestLimitExceededException(_configuration.MaxEmergencyRequestsPerMonth, emergencyRequestsInPeriod);
        }
    }

    #endregion
}

/// <summary>
/// Configuration model for tenant request submission policy.
/// Can be populated from appsettings.json or other configuration sources.
/// </summary>
public class TenantRequestPolicyConfiguration
{
    /// <summary>
    /// Maximum number of pending requests allowed per tenant.
    /// </summary>
    public int MaxPendingRequests { get; set; } = 5;

    /// <summary>
    /// Minimum hours between submissions (0-24). Set to 0 to disable.
    /// </summary>
    public int MinimumHoursBetweenSubmissions { get; set; } = 1;

    /// <summary>
    /// Maximum emergency requests per period. Set to 0 to disable.
    /// </summary>
    public int MaxEmergencyRequestsPerMonth { get; set; } = 3;

    /// <summary>
    /// Days to look back for emergency request counting.
    /// </summary>
    public int EmergencyRequestLookbackDays { get; set; } = 30;

    /// <summary>
    /// Whether rate limiting is enabled.
    /// </summary>
    public bool IsRateLimitingEnabled => MinimumHoursBetweenSubmissions > 0;

    /// <summary>
    /// Whether emergency limiting is enabled.
    /// </summary>
    public bool IsEmergencyLimitingEnabled => MaxEmergencyRequestsPerMonth > 0;

    /// <summary>
    /// Rate limit as TimeSpan.
    /// </summary>
    public TimeSpan RateLimitTimeSpan => TimeSpan.FromHours(MinimumHoursBetweenSubmissions);

    /// <summary>
    /// Emergency lookback period as TimeSpan.
    /// </summary>
    public TimeSpan EmergencyLookbackTimeSpan => TimeSpan.FromDays(EmergencyRequestLookbackDays);
}
