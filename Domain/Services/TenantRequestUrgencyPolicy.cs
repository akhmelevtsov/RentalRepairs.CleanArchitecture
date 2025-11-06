using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for managing tenant request urgency levels and related business logic.
/// Moves urgency-related business rules from Application layer to Domain layer.
/// Provides centralized urgency level management and validation.
/// </summary>
public class TenantRequestUrgencyPolicy
{
    // Domain business rules - valid urgency levels
    private static readonly string[] ValidUrgencyLevels = { "Low", "Normal", "High", "Critical", "Emergency" };

    private static readonly Dictionary<string, int> UrgencyPriorities = new()
    {
        ["Emergency"] = 1,
        ["Critical"] = 2,
        ["High"] = 3,
        ["Normal"] = 4,
        ["Low"] = 5
    };

    private static readonly Dictionary<string, int> ExpectedResolutionHours = new()
    {
        ["Emergency"] = 2,
        ["Critical"] = 4,
        ["High"] = 24,
        ["Normal"] = 72,
        ["Low"] = 168
    };

    /// <summary>
    /// Business rule: Determines default urgency level based on tenant's submission history.
    /// Moved from Application layer TenantRequestSubmissionService.
    /// </summary>
    public string DetermineDefaultUrgencyLevel(Tenant tenant)
    {
        if (tenant?.Requests == null || !tenant.Requests.Any())
        {
            return "Normal";
        }

        // Business rule: Analyze recent submissions to suggest appropriate default urgency
        var recentRequests = tenant.Requests
            .Where(r => r.Status != TenantRequestStatus.Draft)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToList();

        if (!recentRequests.Any())
        {
            return "Normal";
        }

        int emergencyCount = recentRequests.Count(r => r.UrgencyLevel == "Emergency");
        int criticalCount = recentRequests.Count(r => r.UrgencyLevel == "Critical");

        // Business rule: If tenant frequently submits high priority requests, 
        // suggest Normal to encourage proper prioritization
        if (emergencyCount >= 2 || criticalCount >= 3)
        {
            return "Normal";
        }

        // Otherwise, use Normal as default
        return "Normal";
    }

    /// <summary>
    /// Business rule: Gets available urgency levels based on tenant's emergency request history.
    /// Moved from Application layer TenantRequestSubmissionService.
    /// </summary>
    public List<UrgencyLevelOption> GetAvailableUrgencyLevels(Tenant tenant)
    {
        int emergencyRequestsLastMonth = CountEmergencyRequestsLastMonth(tenant);

        return ValidUrgencyLevels.Select(urgencyLevel => new UrgencyLevelOption
        {
            Value = urgencyLevel,
            DisplayName = GetUrgencyDisplayName(urgencyLevel),
            Description = GetUrgencyDescription(urgencyLevel),
            IsAvailable = urgencyLevel != "Emergency" || emergencyRequestsLastMonth < 3,
            DisabledReason = urgencyLevel == "Emergency" && emergencyRequestsLastMonth >= 3
                ? "Emergency submissions limited due to recent usage"
                : null,
            ExpectedResolutionHours = GetExpectedResolutionHours(urgencyLevel),
            Priority = GetUrgencyPriority(urgencyLevel)
        }).ToList();
    }

    /// <summary>
    /// Business rule: Validates if an urgency level is allowed for a tenant.
    /// Prevents abuse of emergency priority levels.
    /// </summary>
    public bool IsUrgencyLevelAllowed(Tenant tenant, string urgencyLevel)
    {
        if (!IsValidUrgencyLevel(urgencyLevel))
        {
            return false;
        }

        // Business rule: Emergency requests are limited to prevent abuse
        if (urgencyLevel == "Emergency")
        {
            int emergencyRequestsLastMonth = CountEmergencyRequestsLastMonth(tenant);
            return emergencyRequestsLastMonth < 3;
        }

        return true;
    }

    /// <summary>
    /// Business rule: Determines if an urgency level qualifies as emergency.
    /// Moved from TenantRequest entity for centralized logic.
    /// </summary>
    public bool IsEmergencyLevel(string urgencyLevel)
    {
        return urgencyLevel is "Critical" or "Emergency";
    }

    /// <summary>
    /// Business rule: Gets expected resolution time based on urgency level.
    /// Provides SLA information for different urgency levels.
    /// </summary>
    public int GetExpectedResolutionHours(string urgencyLevel)
    {
        return ExpectedResolutionHours.ContainsKey(urgencyLevel)
            ? ExpectedResolutionHours[urgencyLevel]
            : 72; // Default to Normal level
    }

    /// <summary>
    /// Business rule: Gets priority order for urgency level (lower number = higher priority).
    /// Used for sorting and prioritization logic.
    /// </summary>
    public int GetUrgencyPriority(string urgencyLevel)
    {
        return UrgencyPriorities.ContainsKey(urgencyLevel)
            ? UrgencyPriorities[urgencyLevel]
            : 5; // Default to Low priority
    }

    /// <summary>
    /// Business rule: Validates if an urgency level is valid.
    /// Moved from Application layer validators.
    /// </summary>
    public bool IsValidUrgencyLevel(string urgencyLevel)
    {
        return !string.IsNullOrWhiteSpace(urgencyLevel) &&
               ValidUrgencyLevels.Contains(urgencyLevel, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Business rule: Gets user-friendly display name for urgency level.
    /// Standardizes urgency level presentation.
    /// </summary>
    public string GetUrgencyDisplayName(string urgencyLevel)
    {
        return urgencyLevel switch
        {
            "Emergency" => "Emergency",
            "Critical" => "Critical",
            "High" => "High Priority",
            "Normal" => "Normal",
            "Low" => "Low Priority",
            _ => urgencyLevel
        };
    }

    /// <summary>
    /// Business rule: Gets detailed description for urgency level.
    /// Moved from Application layer TenantRequestSubmissionService.
    /// </summary>
    public string GetUrgencyDescription(string urgencyLevel)
    {
        return urgencyLevel switch
        {
            "Emergency" => "Immediate attention required (resolved within 2 hours)",
            "Critical" => "High priority issue (resolved within 4 hours)",
            "High" => "Important issue (resolved within 24 hours)",
            "Normal" => "Standard request (resolved within 3 days)",
            "Low" => "Non-urgent request (resolved within 1 week)",
            _ => GetUrgencyDisplayName(urgencyLevel)
        };
    }

    /// <summary>
    /// Business rule: Analyzes tenant's urgency level usage patterns.
    /// Provides insights for policy enforcement and tenant guidance.
    /// </summary>
    public UrgencyUsageAnalysis AnalyzeUrgencyUsage(Tenant tenant, TimeSpan? period = null)
    {
        TimeSpan analysisPeriod = period ?? TimeSpan.FromDays(30);
        DateTime cutoffDate = DateTime.UtcNow - analysisPeriod;

        var recentRequests = tenant.Requests
            .Where(r => r.CreatedAt >= cutoffDate && r.Status != TenantRequestStatus.Draft)
            .ToList();

        var analysis = new UrgencyUsageAnalysis
        {
            AnalysisPeriod = analysisPeriod,
            TotalRequests = recentRequests.Count,
            EmergencyRequests = recentRequests.Count(r => r.UrgencyLevel == "Emergency"),
            CriticalRequests = recentRequests.Count(r => r.UrgencyLevel == "Critical"),
            HighRequests = recentRequests.Count(r => r.UrgencyLevel == "High"),
            NormalRequests = recentRequests.Count(r => r.UrgencyLevel == "Normal"),
            LowRequests = recentRequests.Count(r => r.UrgencyLevel == "Low")
        };

        // Business rule: Determine if usage pattern indicates abuse
        analysis.HasSuspiciousPattern = analysis.EmergencyRequests >= 3 ||
                                        analysis.CriticalRequests + analysis.EmergencyRequests >
                                        analysis.TotalRequests * 0.6;

        // Business rule: Calculate recommended default urgency
        analysis.RecommendedDefaultUrgency = DetermineDefaultUrgencyLevel(tenant);

        return analysis;
    }

    /// <summary>
    /// Gets all valid urgency levels for system configuration.
    /// </summary>
    public List<string> GetAllValidUrgencyLevels()
    {
        return ValidUrgencyLevels.ToList();
    }

    #region Private Helper Methods

    /// <summary>
    /// Counts emergency requests in the last month for business rule enforcement.
    /// </summary>
    private int CountEmergencyRequestsLastMonth(Tenant tenant)
    {
        if (tenant?.Requests == null)
        {
            return 0;
        }

        return tenant.Requests.Count(r =>
            r.UrgencyLevel == "Emergency" &&
            r.Status != TenantRequestStatus.Draft &&
            r.CreatedAt >= DateTime.UtcNow.AddDays(-30));
    }

    #endregion
}

/// <summary>
/// Represents an urgency level option with business context.
/// Moved from Application layer for domain-driven design.
/// </summary>
public class UrgencyLevelOption
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string? DisabledReason { get; set; }
    public int ExpectedResolutionHours { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Analysis of tenant's urgency level usage patterns.
/// Business intelligence for policy enforcement.
/// </summary>
public class UrgencyUsageAnalysis
{
    public TimeSpan AnalysisPeriod { get; set; }
    public int TotalRequests { get; set; }
    public int EmergencyRequests { get; set; }
    public int CriticalRequests { get; set; }
    public int HighRequests { get; set; }
    public int NormalRequests { get; set; }
    public int LowRequests { get; set; }
    public bool HasSuspiciousPattern { get; set; }
    public string RecommendedDefaultUrgency { get; set; } = "Normal";

    public double EmergencyPercentage => TotalRequests > 0 ? (double)EmergencyRequests / TotalRequests * 100 : 0;

    public double HighPriorityPercentage => TotalRequests > 0
        ? (double)(EmergencyRequests + CriticalRequests + HighRequests) / TotalRequests * 100
        : 0;
}
