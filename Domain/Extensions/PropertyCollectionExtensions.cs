using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Extensions;

/// <summary>
/// ✅ STEP 3: Enhanced extension methods for Property collections.
/// Comprehensive domain-specific operations following DDD patterns.
/// Builds on existing property operations with richer analytics and business logic.
/// </summary>
public static class PropertyCollectionExtensions
{
    #region Existing Methods (Preserved)

    /// <summary>
    /// Business logic: Gets properties that require attention.
    /// Uses the Property aggregate's business rule for attention determination.
    /// </summary>
    public static IEnumerable<Property> GetPropertiesRequiringAttention(this IEnumerable<Property> properties)
    {
        return properties.Where(p => p.RequiresAttention());
    }

    /// <summary>
    /// Business logic: Calculates system-wide occupancy statistics.
    /// </summary>
    public static OccupancyStatistics CalculateOccupancyStatistics(this IEnumerable<Property> properties)
    {
        var propertyList = properties.ToList();
        
        if (!propertyList.Any())
        {
            return new OccupancyStatistics();
        }

        var occupancyRates = propertyList.Select(p => p.GetOccupancyRate()).ToList();
        
        return new OccupancyStatistics
        {
            AverageOccupancyRate = occupancyRates.Average(),
            HighestOccupancyRate = occupancyRates.Max(),
            LowestOccupancyRate = occupancyRates.Min(),
            PropertiesAbove90Percent = propertyList.Count(p => p.GetOccupancyRate() > 0.9),
            PropertiesBelow70Percent = propertyList.Count(p => p.GetOccupancyRate() < 0.7)
        };
    }

    /// <summary>
    /// Business logic: Calculates system performance score across all properties.
    /// </summary>
    public static double CalculateSystemPerformanceScore(this IEnumerable<Property> properties)
    {
        var propertyList = properties.ToList();
        
        if (!propertyList.Any())
        {
            return 100.0;
        }

        return propertyList.Average(p => p.CalculatePerformanceScore());
    }

    /// <summary>
    /// Business logic: Gets properties by occupancy range.
    /// </summary>
    public static IEnumerable<Property> GetPropertiesByOccupancyRange(this IEnumerable<Property> properties, 
        double minOccupancy, 
        double maxOccupancy)
    {
        return properties.Where(p => 
        {
            double occupancy = p.GetOccupancyRate();
            return occupancy >= minOccupancy && occupancy <= maxOccupancy;
        });
    }

    /// <summary>
    /// Business logic: Calculates total revenue potential across properties.
    /// </summary>
    public static double CalculateTotalRevenuePotential(this IEnumerable<Property> properties, 
        double averageRentPerUnit = 1000)
    {
        return properties.Sum(p => p.CalculateRevenuePotential(averageRentPerUnit));
    }

    /// <summary>
    /// Business logic: Gets capacity analysis for property portfolio.
    /// </summary>
    public static PropertyCapacityAnalysis AnalyzeCapacity(this IEnumerable<Property> properties)
    {
        var propertyList = properties.ToList();

        int totalUnits = propertyList.Sum(p => p.Units.Count);
        int occupiedUnits = propertyList.Sum(p => p.GetOccupiedUnitsCount());
        int availableUnits = propertyList.Sum(p => p.GetAvailableUnits().Count());
        
        return new PropertyCapacityAnalysis
        {
            TotalCapacity = totalUnits,
            CurrentUtilization = occupiedUnits,
            AvailableCapacity = availableUnits,
            UtilizationPercentage = totalUnits > 0 ? (double)occupiedUnits / totalUnits * 100 : 0,
            CanAccommodateAdditional = availableUnits > 0,
            PropertiesAtCapacity = propertyList.Count(p => !p.CanAccommodateAdditionalTenants())
        };
    }

    /// <summary>
    /// Business logic: Groups properties by performance tier.
    /// </summary>
    public static Dictionary<string, List<Property>> GroupByPerformanceTier(this IEnumerable<Property> properties)
    {
        var propertyList = properties.ToList();
        
        return new Dictionary<string, List<Property>>
        {
            ["High Performance"] = propertyList.Where(p => p.CalculatePerformanceScore() >= 80).ToList(),
            ["Medium Performance"] = propertyList.Where(p => p.CalculatePerformanceScore() >= 60 && p.CalculatePerformanceScore() < 80).ToList(),
            ["Low Performance"] = propertyList.Where(p => p.CalculatePerformanceScore() < 60).ToList()
        };
    }

    #endregion

    #region ✅ STEP 3: Enhanced Property Analytics

    /// <summary>
    /// ✅ STEP 3: Gets properties with high maintenance request frequency.
    /// Supports proactive property management and maintenance planning.
    /// Note: This is a simplified implementation for demonstration. In practice, 
    /// you would integrate with actual maintenance request tracking.
    /// </summary>
    /// <param name="properties">Collection of properties to analyze</param>
    /// <param name="threshold">Minimum performance threshold to be considered high maintenance</param>
    /// <returns>Properties with high maintenance frequency</returns>
    public static List<Property> WithHighMaintenanceFrequency(this IEnumerable<Property> properties, double threshold = 60)
    {
        return properties
            .Where(p => p.CalculatePerformanceScore() < threshold)
            .OrderBy(p => p.CalculatePerformanceScore())
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets properties by geographic region/area.
    /// Supports regional analysis and resource allocation.
    /// </summary>
    /// <param name="properties">Collection of properties to filter</param>
    /// <param name="city">City to filter by</param>
    /// <returns>Properties in the specified city</returns>
    public static List<Property> InCity(this IEnumerable<Property> properties, string city)
    {
        return properties
            .Where(p => p.Address.City.Equals(city, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets properties managed by specific superintendent.
    /// Supports superintendent workload analysis.
    /// </summary>
    /// <param name="properties">Collection of properties to filter</param>
    /// <param name="superintendentEmail">Email of the superintendent</param>
    /// <returns>Properties managed by the superintendent</returns>
    public static List<Property> ManagedBy(this IEnumerable<Property> properties, string superintendentEmail)
    {
        return properties
            .Where(p => p.Superintendent.EmailAddress.Equals(superintendentEmail, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets properties with available units for new tenant placement.
    /// </summary>
    /// <param name="properties">Collection of properties to filter</param>
    /// <returns>Properties that can accommodate new tenants</returns>
    public static List<Property> WithAvailableUnits(this IEnumerable<Property> properties)
    {
        return properties
            .Where(p => p.CanAccommodateAdditionalTenants())
            .OrderByDescending(p => p.GetAvailableUnits().Count())
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets properties at full capacity.
    /// Supports capacity planning and resource allocation.
    /// </summary>
    /// <param name="properties">Collection of properties to filter</param>
    /// <returns>Properties at maximum occupancy</returns>
    public static List<Property> AtFullCapacity(this IEnumerable<Property> properties)
    {
        return properties
            .Where(p => !p.CanAccommodateAdditionalTenants())
            .OrderBy(p => p.Name)
            .ToList();
    }

    #endregion

    #region ✅ STEP 3: Portfolio Performance Analysis

    /// <summary>
    /// ✅ STEP 3: Calculates comprehensive portfolio metrics.
    /// Provides executive-level portfolio performance insights.
    /// </summary>
    /// <param name="properties">Collection of properties to analyze</param>
    /// <returns>Comprehensive portfolio metrics</returns>
    public static PropertyPortfolioMetrics CalculatePortfolioMetrics(this IEnumerable<Property> properties)
    {
        var propertyList = properties.ToList();
        if (!propertyList.Any())
        {
            return new PropertyPortfolioMetrics();
        }

        OccupancyStatistics occupancyStats = propertyList.CalculateOccupancyStatistics();
        
        return new PropertyPortfolioMetrics
        {
            TotalProperties = propertyList.Count,
            TotalUnits = propertyList.Sum(p => p.Units.Count),
            OccupiedUnits = propertyList.Sum(p => p.GetOccupiedUnitsCount()),
            AvailableUnits = propertyList.Sum(p => p.GetAvailableUnits().Count()),
            AverageOccupancyRate = occupancyStats.AverageOccupancyRate,
            AveragePerformanceScore = propertyList.Average(p => p.CalculatePerformanceScore()),
            PropertiesRequiringAttention = propertyList.Count(p => p.RequiresAttention()),
            HighPerformanceProperties = propertyList.Count(p => p.CalculatePerformanceScore() >= 80),
            LowPerformanceProperties = propertyList.Count(p => p.CalculatePerformanceScore() < 60),
            TotalRevenuePotential = propertyList.CalculateTotalRevenuePotential()
        };
    }

    /// <summary>
    /// ✅ STEP 3: Identifies top performing properties.
    /// </summary>
    /// <param name="properties">Collection of properties to analyze</param>
    /// <param name="topCount">Number of top properties to return</param>
    /// <returns>Top performing properties</returns>
    public static List<Property> GetTopPerformers(this IEnumerable<Property> properties, int topCount = 5)
    {
        return properties
            .OrderByDescending(p => p.CalculatePerformanceScore())
            .ThenByDescending(p => p.GetOccupancyRate())
            .Take(topCount)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Identifies underperforming properties needing attention.
    /// </summary>
    /// <param name="properties">Collection of properties to analyze</param>
    /// <param name="performanceThreshold">Minimum performance score threshold</param>
    /// <returns>Underperforming properties</returns>
    public static List<Property> GetUnderperformers(this IEnumerable<Property> properties, double performanceThreshold = 60)
    {
        return properties
            .Where(p => p.CalculatePerformanceScore() < performanceThreshold)
            .OrderBy(p => p.CalculatePerformanceScore())
            .ThenBy(p => p.GetOccupancyRate())
            .ToList();
    }

    #endregion

    #region ✅ STEP 3: Operational Intelligence

    /// <summary>
    /// ✅ STEP 3: Groups properties by maintenance complexity.
    /// Supports resource planning and worker specialization allocation.
    /// Uses performance score as a proxy for maintenance complexity.
    /// </summary>
    /// <param name="properties">Collection of properties to group</param>
    /// <returns>Properties grouped by maintenance complexity</returns>
    public static Dictionary<string, List<Property>> GroupByMaintenanceComplexity(this IEnumerable<Property> properties)
    {
        return new Dictionary<string, List<Property>>
        {
            ["High Maintenance"] = properties.Where(p => p.CalculatePerformanceScore() < 50).ToList(),
            ["Medium Maintenance"] = properties.Where(p => p.CalculatePerformanceScore() >= 50 && p.CalculatePerformanceScore() < 80).ToList(),
            ["Low Maintenance"] = properties.Where(p => p.CalculatePerformanceScore() >= 80).ToList()
        };
    }

    /// <summary>
    /// ✅ STEP 3: Groups properties by size/scale.
    /// </summary>
    /// <param name="properties">Collection of properties to group</param>
    /// <returns>Properties grouped by unit count</returns>
    public static Dictionary<string, List<Property>> GroupBySize(this IEnumerable<Property> properties)
    {
        return new Dictionary<string, List<Property>>
        {
            ["Large (50+ units)"] = properties.Where(p => p.Units.Count >= 50).ToList(),
            ["Medium (20-49 units)"] = properties.Where(p => p.Units.Count >= 20 && p.Units.Count < 50).ToList(),
            ["Small (5-19 units)"] = properties.Where(p => p.Units.Count >= 5 && p.Units.Count < 20).ToList(),
            ["Very Small (<5 units)"] = properties.Where(p => p.Units.Count < 5).ToList()
        };
    }

    /// <summary>
    /// ✅ STEP 3: Groups properties by geographic region.
    /// </summary>
    /// <param name="properties">Collection of properties to group</param>
    /// <returns>Properties grouped by city</returns>
    public static Dictionary<string, List<Property>> GroupByRegion(this IEnumerable<Property> properties)
    {
        return properties
            .GroupBy(p => p.Address.City)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Name).ToList());
    }

    #endregion

    #region ✅ STEP 3: Predictive Analytics Support

    /// <summary>
    /// ✅ STEP 3: Identifies properties at risk based on multiple factors.
    /// Supports proactive management and intervention strategies.
    /// </summary>
    /// <param name="properties">Collection of properties to analyze</param>
    /// <returns>Properties identified as at-risk</returns>
    public static List<Property> IdentifyAtRiskProperties(this IEnumerable<Property> properties)
    {
        return properties
            .Where(p => p.CalculatePerformanceScore() < 60 || 
                       p.GetOccupancyRate() < 0.7 || 
                       p.RequiresAttention())
            .OrderBy(p => p.CalculatePerformanceScore())
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets properties with growth potential.
    /// Identifies properties suitable for investment or expansion.
    /// </summary>
    /// <param name="properties">Collection of properties to analyze</param>
    /// <returns>Properties with growth potential</returns>
    public static List<Property> WithGrowthPotential(this IEnumerable<Property> properties)
    {
        return properties
            .Where(p => p.CalculatePerformanceScore() >= 70 && 
                       p.GetOccupancyRate() >= 0.8 && 
                       p.CanAccommodateAdditionalTenants())
            .OrderByDescending(p => p.CalculatePerformanceScore())
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Calculates superintendent workload distribution.
    /// Supports resource balancing and workload management.
    /// </summary>
    /// <param name="properties">Collection of properties to analyze</param>
    /// <returns>Superintendent workload distribution</returns>
    public static Dictionary<string, SuperintendentWorkload> CalculateSuperintendentWorkloads(this IEnumerable<Property> properties)
    {
        return properties
            .GroupBy(p => p.Superintendent.EmailAddress)
            .ToDictionary(
                g => g.Key,
                g => new SuperintendentWorkload
                {
                    SuperintendentName = g.First().Superintendent.GetFullName(),
                    PropertiesManaged = g.Count(),
                    TotalUnits = g.Sum(p => p.Units.Count),
                    TotalTenants = g.Sum(p => p.GetOccupiedUnitsCount()),
                    AveragePerformanceScore = g.Average(p => p.CalculatePerformanceScore()),
                    PropertiesRequiringAttention = g.Count(p => p.RequiresAttention())
                });
    }

    #endregion
}

/// <summary>
/// Value object for occupancy statistics across property portfolio.
/// </summary>
public class OccupancyStatistics
{
    public double AverageOccupancyRate { get; init; }
    public double HighestOccupancyRate { get; init; }
    public double LowestOccupancyRate { get; init; }
    public int PropertiesAbove90Percent { get; init; }
    public int PropertiesBelow70Percent { get; init; }
}

/// <summary>
/// Value object for property capacity analysis results.
/// </summary>
public class PropertyCapacityAnalysis
{
    public int TotalCapacity { get; init; }
    public int CurrentUtilization { get; init; }
    public int AvailableCapacity { get; init; }
    public double UtilizationPercentage { get; init; }
    public bool CanAccommodateAdditional { get; init; }
    public int PropertiesAtCapacity { get; init; }
}

/// <summary>
/// ✅ STEP 3: Enhanced value object for comprehensive portfolio metrics.
/// </summary>
public class PropertyPortfolioMetrics
{
    public int TotalProperties { get; init; }
    public int TotalUnits { get; init; }
    public int OccupiedUnits { get; init; }
    public int AvailableUnits { get; init; }
    public double AverageOccupancyRate { get; init; }
    public double AveragePerformanceScore { get; init; }
    public int PropertiesRequiringAttention { get; init; }
    public int HighPerformanceProperties { get; init; }
    public int LowPerformanceProperties { get; init; }
    public double TotalRevenuePotential { get; init; }
    
    public double PortfolioUtilization => TotalUnits > 0 ? (double)OccupiedUnits / TotalUnits * 100 : 0;
    public double HighPerformanceRate => TotalProperties > 0 ? (double)HighPerformanceProperties / TotalProperties * 100 : 0;
    public double AttentionRequiredRate => TotalProperties > 0 ? (double)PropertiesRequiringAttention / TotalProperties * 100 : 0;
}

/// <summary>
/// ✅ STEP 3: Value object for superintendent workload analysis.
/// </summary>
public class SuperintendentWorkload
{
    public string SuperintendentName { get; init; } = string.Empty;
    public int PropertiesManaged { get; init; }
    public int TotalUnits { get; init; }
    public int TotalTenants { get; init; }
    public double AveragePerformanceScore { get; init; }
    public int PropertiesRequiringAttention { get; init; }
    
    public double WorkloadScore => PropertiesManaged * 10 + TotalUnits * 0.5 + PropertiesRequiringAttention * 5;
    public bool IsOverloaded => PropertiesManaged > 10 || TotalUnits > 200 || PropertiesRequiringAttention > 3;
}
