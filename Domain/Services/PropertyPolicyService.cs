using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Exceptions;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Pure domain service for property business policies.
/// Contains only business logic without infrastructure dependencies.
/// Replaces the repository-dependent PropertyDomainService.
/// </summary>
public class PropertyPolicyService
{
    /// <summary>
    /// Pure business logic: Validates property creation business rules.
    /// </summary>
    public void ValidatePropertyCreation(
        string name,
        string code,
        PropertyAddress address,
        string phoneNumber,
        PersonContactInfo superintendent,
        List<string> units,
        string noReplyEmailAddress)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Property name cannot be empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Property code cannot be empty", nameof(code));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (superintendent == null)
        {
            throw new ArgumentNullException(nameof(superintendent));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentNullException(nameof(phoneNumber));
        }

        if (string.IsNullOrWhiteSpace(noReplyEmailAddress))
        {
            throw new ArgumentNullException(nameof(noReplyEmailAddress));
        }

        ValidateUnits(units);
    }

    /// <summary>
    /// Pure business logic: Validates tenant registration business rules.
    /// </summary>
    public void ValidateTenantRegistration(
        Property property,
        PersonContactInfo contactInfo,
        string unitNumber)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        if (contactInfo == null)
        {
            throw new ArgumentNullException(nameof(contactInfo));
        }

        if (string.IsNullOrWhiteSpace(unitNumber))
        {
            throw new ArgumentException("Unit number cannot be empty", nameof(unitNumber));
        }

        // Business logic validation - unit exists in property
        if (!property.Units.Contains(unitNumber))
        {
            throw new PropertyDomainException($"Unit {unitNumber} does not exist in property {property.Code}");
        }

        // Business logic validation - unit availability (from property's perspective)
        if (!property.IsUnitAvailable(unitNumber))
        {
            throw new PropertyDomainException($"Unit {unitNumber} is already occupied in property {property.Code}");
        }
    }

    /// <summary>
    /// Pure business logic: Determines optimal unit assignment strategy.
    /// </summary>
    public UnitAssignmentStrategy DetermineOptimalUnitAssignment(
        Property property,
        PersonContactInfo tenantContactInfo,
        string? preferredUnit = null)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        var availableUnits = property.GetAvailableUnits().ToList();
        
        if (!availableUnits.Any())
        {
            return new UnitAssignmentStrategy
            {
                IsAssignmentPossible = false,
                Reason = "No available units",
                RecommendedUnits = new List<string>()
            };
        }

        // Business logic: Prefer requested unit if available
        if (!string.IsNullOrWhiteSpace(preferredUnit) && availableUnits.Contains(preferredUnit))
        {
            return new UnitAssignmentStrategy
            {
                IsAssignmentPossible = true,
                RecommendedUnits = new List<string> { preferredUnit },
                Reason = "Preferred unit is available"
            };
        }

        // Business logic: Recommend based on property characteristics
        var sortedUnits = availableUnits.OrderBy(u => u).ToList();
        
        return new UnitAssignmentStrategy
        {
            IsAssignmentPossible = true,
            RecommendedUnits = sortedUnits,
            Reason = preferredUnit != null 
                ? $"Preferred unit '{preferredUnit}' not available, showing alternatives"
                : "Available units sorted by unit number"
        };
    }

    /// <summary>
    /// Pure business logic: Calculates property performance metrics.
    /// </summary>
    public PropertyPerformanceMetrics CalculatePropertyPerformance(
        Property property,
        List<TenantRequest> recentRequests,
        TimeSpan analysisWindow)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        DateTime cutoffDate = DateTime.UtcNow - analysisWindow;
        List<TenantRequest> relevantRequests = recentRequests?.Where(r => r.CreatedAt >= cutoffDate).ToList() ?? new List<TenantRequest>();

        PropertyMetrics baseMetrics = property.CalculateMetrics();
        
        return new PropertyPerformanceMetrics
        {
            BaseMetrics = baseMetrics,
            MaintenanceRequestCount = relevantRequests.Count,
            AverageResolutionTime = CalculateAverageResolutionTime(relevantRequests),
            EmergencyRequestCount = relevantRequests.Count(r => r.IsEmergency),
            PerformanceScore = CalculateOverallPerformanceScore(baseMetrics, relevantRequests),
            RecommendedActions = GeneratePropertyRecommendations(baseMetrics, relevantRequests)
        };
    }

    #region Private Helper Methods - Pure Business Logic

    private void ValidateUnits(List<string> units)
    {
        if (units == null || !units.Any())
        {
            throw new PropertyDomainException("Property must have at least one unit");
        }

        if (units.Distinct().Count() != units.Count)
        {
            throw new PropertyDomainException("Property cannot have duplicate unit numbers");
        }

        foreach (string unit in units)
        {
            if (!IsValidUnitNumber(unit))
            {
                throw new PropertyDomainException($"Unit number '{unit}' has invalid format");
            }
        }
    }

    private bool IsValidUnitNumber(string unitNumber)
    {
        return !string.IsNullOrWhiteSpace(unitNumber) && 
               unitNumber.Length <= 10 && 
               System.Text.RegularExpressions.Regex.IsMatch(unitNumber, @"^[A-Za-z0-9\-\s]+$");
    }

    private TimeSpan CalculateAverageResolutionTime(List<TenantRequest> requests)
    {
        var completedRequests = requests.Where(r => r.CompletedDate.HasValue).ToList();
        
        if (!completedRequests.Any())
        {
            return TimeSpan.Zero;
        }

        double totalTime = completedRequests
            .Sum(r => (r.CompletedDate!.Value - r.CreatedAt).TotalHours);
        
        return TimeSpan.FromHours(totalTime / completedRequests.Count);
    }

    private double CalculateOverallPerformanceScore(PropertyMetrics baseMetrics, List<TenantRequest> requests)
    {
        double occupancyScore = baseMetrics.OccupancyRate * 40; // 40% weight
        double maintenanceScore = CalculateMaintenanceScore(requests) * 35; // 35% weight
        double responseScore = CalculateResponseScore(requests) * 25; // 25% weight
        
        return occupancyScore + maintenanceScore + responseScore;
    }

    private double CalculateMaintenanceScore(List<TenantRequest> requests)
    {
        if (!requests.Any())
        {
            return 100; // No requests = perfect score
        }

        var completedRequests = requests.Where(r => r.CompletedDate.HasValue).ToList();
        double successRate = (double)completedRequests.Count(r => r.WorkCompletedSuccessfully == true) / requests.Count;
        
        return successRate * 100;
    }

    private double CalculateResponseScore(List<TenantRequest> requests)
    {
        if (!requests.Any())
        {
            return 100;
        }

        int onTimeCount = requests.Count(r => r.WasResolvedOnTime());
        return ((double)onTimeCount / requests.Count) * 100;
    }

    private List<string> GeneratePropertyRecommendations(PropertyMetrics metrics, List<TenantRequest> requests)
    {
        var recommendations = new List<string>();

        if (metrics.OccupancyRate < 0.8)
        {
            recommendations.Add("Consider marketing strategies to improve occupancy rate");
        }

        if (requests.Count(r => r.IsEmergency) > 5)
        {
            recommendations.Add("High number of emergency requests - review preventive maintenance");
        }

        double failedRequestRate = requests.Any() ? 
            (double)requests.Count(r => r.Status == Domain.Enums.TenantRequestStatus.Failed) / requests.Count : 0;

        if (failedRequestRate > 0.1) // More than 10% failure rate
        {
            recommendations.Add("High request failure rate - review worker assignments and training");
        }

        return recommendations;
    }

    #endregion
}

/// <summary>
/// Value object for unit assignment strategy results.
/// </summary>
public class UnitAssignmentStrategy
{
    public bool IsAssignmentPossible { get; init; }
    public List<string> RecommendedUnits { get; init; } = new();
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Value object for comprehensive property performance metrics.
/// </summary>
public class PropertyPerformanceMetrics
{
    public PropertyMetrics BaseMetrics { get; init; } = new();
    public int MaintenanceRequestCount { get; init; }
    public TimeSpan AverageResolutionTime { get; init; }
    public int EmergencyRequestCount { get; init; }
    public double PerformanceScore { get; init; }
    public List<string> RecommendedActions { get; init; } = new();
}
