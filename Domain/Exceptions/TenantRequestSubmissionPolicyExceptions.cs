namespace RentalRepairs.Domain.Exceptions;

/// <summary>
/// Base exception for tenant request submission policy violations.
/// Follows DDD principles by representing domain rule violations.
/// </summary>
public abstract class TenantRequestSubmissionPolicyException : DomainException
{
    protected TenantRequestSubmissionPolicyException(string message) : base(message) { }
    protected TenantRequestSubmissionPolicyException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when tenant exceeds maximum pending requests limit.
/// </summary>
public class MaxPendingRequestsExceededException : TenantRequestSubmissionPolicyException
{
    public int MaxAllowed { get; }
    public int CurrentCount { get; }

    public MaxPendingRequestsExceededException(int maxAllowed, int currentCount) 
        : base($"Maximum pending requests exceeded. Allowed: {maxAllowed}, Current: {currentCount}")
    {
        MaxAllowed = maxAllowed;
        CurrentCount = currentCount;
    }
}

/// <summary>
/// Exception thrown when tenant submits requests too frequently.
/// </summary>
public class SubmissionRateLimitExceededException : TenantRequestSubmissionPolicyException
{
    public TimeSpan RetryAfter { get; }

    public SubmissionRateLimitExceededException(TimeSpan retryAfter) 
        : base($"Submission rate limit exceeded. Please wait {retryAfter.TotalMinutes:F0} minutes before submitting another request.")
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// Exception thrown when tenant exceeds emergency request limits.
/// </summary>
public class EmergencyRequestLimitExceededException : TenantRequestSubmissionPolicyException
{
    public int MaxAllowedPerMonth { get; }
    public int CurrentMonthCount { get; }

    public EmergencyRequestLimitExceededException(int maxAllowedPerMonth, int currentMonthCount) 
        : base($"Emergency request limit exceeded. Allowed per month: {maxAllowedPerMonth}, Current month: {currentMonthCount}")
    {
        MaxAllowedPerMonth = maxAllowedPerMonth;
        CurrentMonthCount = currentMonthCount;
    }
}
