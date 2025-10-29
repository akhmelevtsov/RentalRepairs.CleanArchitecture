using RentalRepairs.Domain.Exceptions;

namespace RentalRepairs.Domain.Enums;

/// <summary>
/// Strongly-typed enumeration for tenant request urgency levels.
/// Provides compile-time validation and consistent business logic.
/// </summary>
public enum TenantRequestUrgency
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3,
    Emergency = 4
}

/// <summary>
/// Extension methods for TenantRequestUrgency enum.
/// Centralizes business logic related to urgency levels.
/// </summary>
public static class TenantRequestUrgencyExtensions
{
    /// <summary>
    /// Gets the display name for the urgency level.
    /// </summary>
    public static string GetDisplayName(this TenantRequestUrgency urgency)
    {
        return urgency switch
        {
            TenantRequestUrgency.Low => "Low",
            TenantRequestUrgency.Normal => "Normal",
            TenantRequestUrgency.High => "High",
            TenantRequestUrgency.Critical => "Critical",
            TenantRequestUrgency.Emergency => "Emergency",
            _ => urgency.ToString()
        };
    }

    /// <summary>
    /// Gets the expected resolution time in hours for each urgency level.
    /// </summary>
    public static int GetExpectedResolutionHours(this TenantRequestUrgency urgency)
    {
        return urgency switch
        {
            TenantRequestUrgency.Emergency => 2,
            TenantRequestUrgency.Critical => 4,
            TenantRequestUrgency.High => 24,
            TenantRequestUrgency.Normal => 72,
            TenantRequestUrgency.Low => 168,
            _ => 72
        };
    }

    /// <summary>
    /// Determines if the urgency level requires immediate attention.
    /// </summary>
    public static bool RequiresImmediateAttention(this TenantRequestUrgency urgency)
    {
        return urgency is TenantRequestUrgency.Critical or TenantRequestUrgency.Emergency;
    }

    /// <summary>
    /// Converts string urgency level to enum.
    /// Provides backward compatibility with existing string-based system.
    /// Throws TenantRequestDomainException for consistency with domain layer.
    /// </summary>
    public static TenantRequestUrgency FromString(string urgencyLevel)
    {
        return urgencyLevel?.Trim().ToLowerInvariant() switch
        {
            "low" => TenantRequestUrgency.Low,
            "normal" => TenantRequestUrgency.Normal,
            "high" => TenantRequestUrgency.High,
            "critical" => TenantRequestUrgency.Critical,
            "emergency" => TenantRequestUrgency.Emergency,
            _ => throw new TenantRequestDomainException($"Invalid urgency level: {urgencyLevel}")
        };
    }

    /// <summary>
    /// Gets all available urgency levels as strings for backward compatibility.
    /// </summary>
    public static IReadOnlyList<string> GetAllDisplayNames()
    {
        return Enum.GetValues<TenantRequestUrgency>()
            .Select(u => u.GetDisplayName())
            .ToList()
            .AsReadOnly();
    }
}
