using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Extensions;

/// <summary>
/// ✅ STEP 3: Cross-aggregate analytics extensions.
/// Provides business intelligence operations that span multiple aggregates.
/// Demonstrates advanced DDD patterns for complex business analytics.
/// </summary>
public static class CrossAggregateAnalyticsExtensions
{
    #region Property-Request Analytics

    /// <summary>
    /// ✅ STEP 3: Analyzes maintenance patterns across properties and requests.
    /// Provides insights into property maintenance needs and request patterns.
    /// </summary>
    /// <param name="properties">Collection of properties</param>
    /// <param name="requests">Collection of tenant requests</param>
    /// <returns>Comprehensive maintenance analysis</returns>
    public static MaintenancePatternAnalysis AnalyzeMaintenancePatterns(
        this IEnumerable<Property> properties, 
        IEnumerable<TenantRequest> requests)
    {
        var propertyList = properties.ToList();
        var requestList = requests.ToList();

        var propertyRequestMap = requestList
            .GroupBy(r => r.PropertyName)
            .ToDictionary(g => g.Key, g => g.ToList());

        var patternsByProperty = new Dictionary<string, PropertyMaintenancePattern>();

        foreach (Property? property in propertyList)
        {
            List<TenantRequest> propertyRequests = propertyRequestMap.GetValueOrDefault(property.Name, new List<TenantRequest>());
            
            patternsByProperty[property.Name] = new PropertyMaintenancePattern
            {
                PropertyName = property.Name,
                PropertyCode = property.Code,
                TotalRequests = propertyRequests.Count,
                EmergencyRequests = propertyRequests.Count(r => r.IsEmergency),
                AverageResolutionTime = propertyRequests.Any() ? 
                    propertyRequests.CalculateAverageResolutionTime().TotalHours : 0,
                TopCategories = propertyRequests.IdentifyTopIssueCategories(),
                OccupancyRate = property.GetOccupancyRate(),
                PerformanceScore = property.CalculatePerformanceScore(),
                RequestsPerUnit = property.Units.Count > 0 ? 
                    (double)propertyRequests.Count / property.Units.Count : 0
            };
        }

        return new MaintenancePatternAnalysis
        {
            TotalProperties = propertyList.Count,
            TotalRequests = requestList.Count,
            PatternsByProperty = patternsByProperty,
            HighMaintenanceProperties = patternsByProperty.Values
                .Where(p => p.RequestsPerUnit > 0.5)
                .OrderByDescending(p => p.RequestsPerUnit)
                .Take(5)
                .ToList(),
            PropertyPerformanceCorrelation = CalculateMaintenancePerformanceCorrelation(patternsByProperty.Values)
        };
    }

    /// <summary>
    /// ✅ STEP 3: Analyzes worker efficiency across different properties.
    /// Provides insights into worker performance patterns by location.
    /// </summary>
    /// <param name="workers">Collection of workers</param>
    /// <param name="requests">Collection of tenant requests</param>
    /// <returns>Worker efficiency analysis by property</returns>
    public static WorkerEfficiencyAnalysis AnalyzeWorkerEfficiency(
        this IEnumerable<Worker> workers,
        IEnumerable<TenantRequest> requests)
    {
        var workerList = workers.ToList();
        var requestList = requests.Where(r => !string.IsNullOrEmpty(r.AssignedWorkerEmail)).ToList();

        var workerPerformance = new Dictionary<string, WorkerPerformanceMetrics>();

        foreach (Worker? worker in workerList)
        {
            List<TenantRequest> workerRequests = requestList.AssignedToWorker(worker.ContactInfo.EmailAddress);
            List<TenantRequest> completedRequests = workerRequests.CompletedSuccessfully();

            workerPerformance[worker.ContactInfo.EmailAddress] = new WorkerPerformanceMetrics
            {
                WorkerName = worker.ContactInfo.GetFullName(),
                WorkerEmail = worker.ContactInfo.EmailAddress,
                Specialization = worker.Specialization ?? "General Maintenance",
                TotalAssignments = workerRequests.Count,
                CompletedSuccessfully = completedRequests.Count,
                AverageResolutionTime = workerRequests.Any() ? 
                    workerRequests.CalculateAverageResolutionTime().TotalHours : 0,
                FirstCallResolutionRate = workerRequests.CalculateFirstCallResolutionRate(),
                EmergencyRequestsHandled = workerRequests.Count(r => r.IsEmergency),
                PropertiesServed = workerRequests.GroupBy(r => r.PropertyName).Count(),
                PerformanceScore = completedRequests.Any() ?
                    completedRequests.Average(r => r.CalculateResolutionPerformanceScore()) : 0
            };
        }

        return new WorkerEfficiencyAnalysis
        {
            TotalWorkers = workerList.Count,
            WorkerMetrics = workerPerformance,
            TopPerformers = workerPerformance.Values
                .OrderByDescending(w => w.PerformanceScore)
                .Take(5)
                .ToList(),
            SpecializationEfficiency = CalculateSpecializationEfficiency(workerPerformance.Values, requestList)
        };
    }

    #endregion

    #region Resource Utilization Analytics

    /// <summary>
    /// ✅ STEP 3: Analyzes resource utilization across the entire system.
    /// Provides comprehensive resource optimization insights.
    /// </summary>
    /// <param name="properties">Collection of properties</param>
    /// <param name="workers">Collection of workers</param>
    /// <param name="requests">Collection of tenant requests</param>
    /// <returns>System-wide resource utilization analysis</returns>
    public static SystemResourceAnalysis AnalyzeSystemResources(
        this IEnumerable<Property> properties,
        IEnumerable<Worker> workers,
        IEnumerable<TenantRequest> requests)
    {
        var propertyList = properties.ToList();
        var workerList = workers.ToList();
        var requestList = requests.ToList();

        var activeWorkers = workerList.Where(w => w.IsActive).ToList();
        WorkloadDistribution workloadDistribution = activeWorkers.CalculateWorkloadDistribution();
        
        return new SystemResourceAnalysis
        {
            PropertyUtilization = new PropertyUtilizationMetrics
            {
                TotalProperties = propertyList.Count,
                AverageOccupancy = propertyList.Any() ? propertyList.Average(p => p.GetOccupancyRate()) : 0,
                PropertiesAtCapacity = propertyList.Count(p => !p.CanAccommodateAdditionalTenants()),
                UnderutilizedProperties = propertyList.Count(p => p.GetOccupancyRate() < 0.7)
            },
            WorkforceUtilization = new WorkforceUtilizationMetrics
            {
                TotalWorkers = workerList.Count,
                ActiveWorkers = activeWorkers.Count,
                AverageWorkload = workloadDistribution.AverageWorkload,
                OverloadedWorkers = workloadDistribution.OverloadedWorkers,
                UnassignedRequests = requestList.RequiringWorkerAssignment().Count
            },
            RequestFlow = new RequestFlowMetrics
            {
                TotalRequests = requestList.Count,
                PendingRequests = requestList.Count(r => r.Status == TenantRequestStatus.Submitted),
                InProgressRequests = requestList.Count(r => r.Status == TenantRequestStatus.Scheduled),
                OverdueRequests = requestList.GetOverdueRequests().Count(),
                EmergencyRequests = requestList.Count(r => r.IsEmergency)
            },
            SystemEfficiency = CalculateSystemEfficiency(propertyList, activeWorkers, requestList)
        };
    }

    /// <summary>
    /// ✅ STEP 3: Identifies bottlenecks in the maintenance workflow.
    /// Provides actionable insights for process optimization.
    /// </summary>
    /// <param name="requests">Collection of tenant requests</param>
    /// <param name="workers">Collection of workers</param>
    /// <returns>Bottleneck analysis with recommendations</returns>
    public static BottleneckAnalysis IdentifySystemBottlenecks(
        this IEnumerable<TenantRequest> requests,
        IEnumerable<Worker> workers)
    {
        var requestList = requests.ToList();
        var workerList = workers.Where(w => w.IsActive).ToList();

        var bottlenecks = new List<BottleneckIdentification>();

        // Worker availability bottleneck
        List<TenantRequest> unassignedRequests = requestList.RequiringWorkerAssignment();
        if (unassignedRequests.Count > workerList.Count * 2)
        {
            bottlenecks.Add(new BottleneckIdentification
            {
                Type = "Worker Shortage",
                Severity = "High",
                Description = $"{unassignedRequests.Count} unassigned requests vs {workerList.Count} active workers",
                ImpactedRequests = unassignedRequests.Count,
                Recommendation = "Consider hiring additional workers or optimizing work schedules"
            });
        }

        // Specialization bottleneck
        var specializationDemand = requestList
            .GroupBy(r => Worker.DetermineRequiredSpecialization(r.Title, r.Description))
            .ToDictionary(g => g.Key, g => g.Count());

        var specializationSupply = workerList
            .GroupBy(w => w.Specialization ?? "General Maintenance")
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (KeyValuePair<string, int> demand in specializationDemand)
        {
            int supply = specializationSupply.GetValueOrDefault(demand.Key, 0);
            if (demand.Value > supply * 5) // More than 5 requests per specialist
            {
                bottlenecks.Add(new BottleneckIdentification
                {
                    Type = "Specialization Shortage",
                    Severity = "Medium",
                    Description = $"{demand.Value} requests need {demand.Key} specialists but only {supply} available",
                    ImpactedRequests = demand.Value,
                    Recommendation = $"Train more workers in {demand.Key} or hire specialists"
                });
            }
        }

        // Emergency handling bottleneck
        List<TenantRequest> emergencyRequests = requestList.GetEmergencyRequests();
        List<Worker> emergencyCapableWorkers = workerList.GetAvailableForEmergency();
        if (emergencyRequests.Count > emergencyCapableWorkers.Count)
        {
            bottlenecks.Add(new BottleneckIdentification
            {
                Type = "Emergency Response",
                Severity = "Critical",
                Description = $"{emergencyRequests.Count} emergency requests vs {emergencyCapableWorkers.Count} emergency-capable workers",
                ImpactedRequests = emergencyRequests.Count,
                Recommendation = "Designate more workers for emergency response or streamline emergency procedures"
            });
        }

        return new BottleneckAnalysis
        {
            AnalysisDate = DateTime.UtcNow,
            BottlenecksIdentified = bottlenecks,
            OverallSystemHealth = CalculateSystemHealth(bottlenecks),
            PriorityActions = bottlenecks
                .Where(b => b.Severity == "Critical" || b.Severity == "High")
                .OrderBy(b => b.Severity == "Critical" ? 0 : 1)
                .ToList()
        };
    }

    #endregion

    #region Predictive Analytics

    /// <summary>
    /// ✅ STEP 3: Performs trend analysis for predictive maintenance.
    /// Identifies patterns that can predict future maintenance needs.
    /// </summary>
    /// <param name="requests">Historical tenant requests</param>
    /// <param name="properties">Collection of properties</param>
    /// <returns>Predictive maintenance insights</returns>
    public static PredictiveMaintenanceInsights AnalyzePredictiveMaintenanceTrends(
        this IEnumerable<TenantRequest> requests,
        IEnumerable<Property> properties)
    {
        var requestList = requests.ToList();
        var propertyList = properties.ToList();

        var monthlyTrends = requestList
            .Where(r => r.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyMaintenanceTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalRequests = g.Count(),
                EmergencyRequests = g.Count(r => r.IsEmergency),
                CategoryBreakdown = g.GroupBy(r => r.DetermineCategoryFromDescription())
                                   .ToDictionary(cg => cg.Key, cg => cg.Count())
            })
            .ToList();

        var propertyRiskAssessment = propertyList.Select(p => 
        {
            IEnumerable<TenantRequest> propertyRequests = requestList.Where(r => r.PropertyName == p.Name);
            List<TenantRequest> recentRequests = propertyRequests.CreatedWithinDays(90);
            
            return new PropertyRiskAssessment
            {
                PropertyName = p.Name,
                PropertyCode = p.Code,
                RiskScore = CalculatePropertyRiskScore(p, recentRequests),
                PredictedMaintenanceNeed = PredictMaintenanceNeed(propertyRequests),
                RecommendedActions = GenerateMaintenanceRecommendations(p, recentRequests)
            };
        }).ToList();

        return new PredictiveMaintenanceInsights
        {
            MonthlyTrends = monthlyTrends,
            PropertyRiskAssessments = propertyRiskAssessment,
            HighRiskProperties = propertyRiskAssessment
                .Where(p => p.RiskScore > 70)
                .OrderByDescending(p => p.RiskScore)
                .ToList(),
            SystemWidePredictions = GenerateSystemPredictions(monthlyTrends, propertyRiskAssessment)
        };
    }

    #endregion

    #region Private Helper Methods

    private static double CalculateMaintenancePerformanceCorrelation(IEnumerable<PropertyMaintenancePattern> patterns)
    {
        var patternList = patterns.ToList();
        if (patternList.Count < 2)
        {
            return 0;
        }

        // Simple correlation between maintenance frequency and performance score
        double avgRequestsPerUnit = patternList.Average(p => p.RequestsPerUnit);
        double avgPerformanceScore = patternList.Average(p => p.PerformanceScore);

        double correlation = patternList.Select(p => 
            (p.RequestsPerUnit - avgRequestsPerUnit) * (p.PerformanceScore - avgPerformanceScore)
        ).Sum() / (patternList.Count - 1);

        return Math.Max(-1, Math.Min(1, correlation / 100)); // Normalize to [-1, 1]
    }

    private static Dictionary<string, SpecializationEfficiencyMetrics> CalculateSpecializationEfficiency(
        IEnumerable<WorkerPerformanceMetrics> workerMetrics,
        IEnumerable<TenantRequest> requests)
    {
        var requestsBySpecialization = requests
            .GroupBy(r => Worker.DetermineRequiredSpecialization(r.Title, r.Description))
            .ToDictionary(g => g.Key, g => g.ToList());

        return workerMetrics
            .GroupBy(w => w.Specialization)
            .ToDictionary(g => g.Key, g => new SpecializationEfficiencyMetrics
            {
                Specialization = g.Key,
                WorkerCount = g.Count(),
                AveragePerformanceScore = g.Average(w => w.PerformanceScore),
                TotalRequestsHandled = g.Sum(w => w.TotalAssignments),
                AverageResolutionTime = g.Average(w => w.AverageResolutionTime),
                FirstCallResolutionRate = g.Average(w => w.FirstCallResolutionRate)
            });
    }

    private static SystemEfficiencyMetrics CalculateSystemEfficiency(
        List<Property> properties,
        List<Worker> workers,
        List<TenantRequest> requests)
    {
        return new SystemEfficiencyMetrics
        {
            OverallEfficiencyScore = (
                properties.Any() ? properties.Average(p => p.CalculatePerformanceScore()) : 0 +
                requests.CalculateFirstCallResolutionRate()
            ) / 2,
            ResourceUtilizationRate = requests.CalculateResourceUtilizationRate(),
            ResponseTimeEfficiency = CalculateResponseTimeEfficiency(requests),
            CapacityUtilization = properties.AnalyzeCapacity().UtilizationPercentage
        };
    }

    private static double CalculateResponseTimeEfficiency(List<TenantRequest> requests)
    {
        var completedRequests = requests.Where(r => r.CompletedDate.HasValue).ToList();
        if (!completedRequests.Any())
        {
            return 100;
        }

        int onTimeRequests = completedRequests.Count(r => r.WasResolvedOnTime());
        return (double)onTimeRequests / completedRequests.Count * 100;
    }

    private static string CalculateSystemHealth(List<BottleneckIdentification> bottlenecks)
    {
        int criticalCount = bottlenecks.Count(b => b.Severity == "Critical");
        int highCount = bottlenecks.Count(b => b.Severity == "High");

        if (criticalCount > 0)
        {
            return "Critical";
        }

        if (highCount > 2)
        {
            return "Poor";
        }

        if (highCount > 0)
        {
            return "Fair";
        }

        return "Good";
    }

    private static double CalculatePropertyRiskScore(Property property, IEnumerable<TenantRequest> recentRequests)
    {
        var requestList = recentRequests.ToList();
        var riskFactors = new List<double>();

        // High request frequency
        riskFactors.Add(Math.Min(100, requestList.Count * 5));

        // Low performance score
        riskFactors.Add(100 - property.CalculatePerformanceScore());

        // Emergency request ratio
        double emergencyRatio = requestList.Any() ? 
            (double)requestList.Count(r => r.IsEmergency) / requestList.Count * 100 : 0;
        riskFactors.Add(emergencyRatio);

        // Low occupancy (might indicate issues)
        if (property.GetOccupancyRate() < 0.8)
        {
            riskFactors.Add((0.8 - property.GetOccupancyRate()) * 100);
        }

        return riskFactors.Any() ? riskFactors.Average() : 0;
    }

    private static string PredictMaintenanceNeed(IEnumerable<TenantRequest> historicalRequests)
    {
        var requestList = historicalRequests.ToList();
        int recentTrend = requestList.CreatedWithinDays(30).Count;
        int previousTrend = requestList.CreatedWithinDays(60).Count - recentTrend;

        if (recentTrend > previousTrend * 1.5)
        {
            return "Increasing";
        }

        if (recentTrend < previousTrend * 0.7)
        {
            return "Decreasing";
        }

        return "Stable";
    }

    private static List<string> GenerateMaintenanceRecommendations(Property property, IEnumerable<TenantRequest> recentRequests)
    {
        var recommendations = new List<string>();
        var requestList = recentRequests.ToList();

        if (property.GetOccupancyRate() < 0.7)
        {
            recommendations.Add("Investigate low occupancy - may indicate property issues");
        }

        List<string> categoryBreakdown = requestList.IdentifyTopIssueCategories();
        if (categoryBreakdown.Any())
        {
            recommendations.Add($"Focus on {categoryBreakdown.First()} issues - highest frequency category");
        }

        if (requestList.Count(r => r.IsEmergency) > requestList.Count * 0.3)
        {
            recommendations.Add("High emergency ratio - consider preventive maintenance");
        }

        return recommendations;
    }

    private static SystemPredictions GenerateSystemPredictions(
        List<MonthlyMaintenanceTrend> trends,
        List<PropertyRiskAssessment> riskAssessments)
    {
        var lastThreeMonths = trends.TakeLast(3).ToList();
        double avgRequestsGrowth = lastThreeMonths.Count > 1 ? 
            (lastThreeMonths.Last().TotalRequests - lastThreeMonths.First().TotalRequests) / (double)lastThreeMonths.Count : 0;

        return new SystemPredictions
        {
            PredictedRequestVolumeNextMonth = Math.Max(0, trends.LastOrDefault()?.TotalRequests + avgRequestsGrowth ?? 0),
            PredictedHighRiskProperties = riskAssessments.Count(r => r.RiskScore > 70),
            RecommendedActions = GenerateSystemRecommendations(trends, riskAssessments)
        };
    }

    private static List<string> GenerateSystemRecommendations(
        List<MonthlyMaintenanceTrend> trends,
        List<PropertyRiskAssessment> riskAssessments)
    {
        var recommendations = new List<string>();

        if (trends.Count >= 3 && trends.TakeLast(3).All(t => t.TotalRequests > trends.First().TotalRequests))
        {
            recommendations.Add("Request volume trending upward - consider expanding workforce");
        }

        int highRiskCount = riskAssessments.Count(r => r.RiskScore > 70);
        if (highRiskCount > riskAssessments.Count * 0.2)
        {
            recommendations.Add($"{highRiskCount} properties at high risk - prioritize preventive maintenance");
        }

        return recommendations;
    }

    #endregion
}

#region Value Objects for Cross-Aggregate Analytics

/// <summary>
/// ✅ STEP 3: Value object for maintenance pattern analysis results.
/// </summary>
public class MaintenancePatternAnalysis
{
    public int TotalProperties { get; init; }
    public int TotalRequests { get; init; }
    public Dictionary<string, PropertyMaintenancePattern> PatternsByProperty { get; init; } = new();
    public List<PropertyMaintenancePattern> HighMaintenanceProperties { get; init; } = new();
    public double PropertyPerformanceCorrelation { get; init; }
}

public class PropertyMaintenancePattern
{
    public string PropertyName { get; init; } = string.Empty;
    public string PropertyCode { get; init; } = string.Empty;
    public int TotalRequests { get; init; }
    public int EmergencyRequests { get; init; }
    public double AverageResolutionTime { get; init; }
    public List<string> TopCategories { get; init; } = new();
    public double OccupancyRate { get; init; }
    public double PerformanceScore { get; init; }
    public double RequestsPerUnit { get; init; }
}

/// <summary>
/// ✅ STEP 3: Value object for worker efficiency analysis.
/// </summary>
public class WorkerEfficiencyAnalysis
{
    public int TotalWorkers { get; init; }
    public Dictionary<string, WorkerPerformanceMetrics> WorkerMetrics { get; init; } = new();
    public List<WorkerPerformanceMetrics> TopPerformers { get; init; } = new();
    public Dictionary<string, SpecializationEfficiencyMetrics> SpecializationEfficiency { get; init; } = new();
}

public class WorkerPerformanceMetrics
{
    public string WorkerName { get; init; } = string.Empty;
    public string WorkerEmail { get; init; } = string.Empty;
    public string Specialization { get; init; } = string.Empty;
    public int TotalAssignments { get; init; }
    public int CompletedSuccessfully { get; init; }
    public double AverageResolutionTime { get; init; }
    public double FirstCallResolutionRate { get; init; }
    public int EmergencyRequestsHandled { get; init; }
    public int PropertiesServed { get; init; }
    public double PerformanceScore { get; init; }
    
    public double SuccessRate => TotalAssignments > 0 ? (double)CompletedSuccessfully / TotalAssignments * 100 : 0;
}

public class SpecializationEfficiencyMetrics
{
    public string Specialization { get; init; } = string.Empty;
    public int WorkerCount { get; init; }
    public double AveragePerformanceScore { get; init; }
    public int TotalRequestsHandled { get; init; }
    public double AverageResolutionTime { get; init; }
    public double FirstCallResolutionRate { get; init; }
}

/// <summary>
/// ✅ STEP 3: Value object for system resource analysis.
/// </summary>
public class SystemResourceAnalysis
{
    public PropertyUtilizationMetrics PropertyUtilization { get; init; } = new();
    public WorkforceUtilizationMetrics WorkforceUtilization { get; init; } = new();
    public RequestFlowMetrics RequestFlow { get; init; } = new();
    public SystemEfficiencyMetrics SystemEfficiency { get; init; } = new();
}

public class PropertyUtilizationMetrics
{
    public int TotalProperties { get; init; }
    public double AverageOccupancy { get; init; }
    public int PropertiesAtCapacity { get; init; }
    public int UnderutilizedProperties { get; init; }
}

public class WorkforceUtilizationMetrics
{
    public int TotalWorkers { get; init; }
    public int ActiveWorkers { get; init; }
    public double AverageWorkload { get; init; }
    public int OverloadedWorkers { get; init; }
    public int UnassignedRequests { get; init; }
}

public class RequestFlowMetrics
{
    public int TotalRequests { get; init; }
    public int PendingRequests { get; init; }
    public int InProgressRequests { get; init; }
    public int OverdueRequests { get; init; }
    public int EmergencyRequests { get; init; }
}

public class SystemEfficiencyMetrics
{
    public double OverallEfficiencyScore { get; init; }
    public double ResourceUtilizationRate { get; init; }
    public double ResponseTimeEfficiency { get; init; }
    public double CapacityUtilization { get; init; }
}

/// <summary>
/// ✅ STEP 3: Value object for bottleneck analysis.
/// </summary>
public class BottleneckAnalysis
{
    public DateTime AnalysisDate { get; init; }
    public List<BottleneckIdentification> BottlenecksIdentified { get; init; } = new();
    public string OverallSystemHealth { get; init; } = "Good";
    public List<BottleneckIdentification> PriorityActions { get; init; } = new();
}

public class BottleneckIdentification
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int ImpactedRequests { get; init; }
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// ✅ STEP 3: Value object for predictive maintenance insights.
/// </summary>
public class PredictiveMaintenanceInsights
{
    public List<MonthlyMaintenanceTrend> MonthlyTrends { get; init; } = new();
    public List<PropertyRiskAssessment> PropertyRiskAssessments { get; init; } = new();
    public List<PropertyRiskAssessment> HighRiskProperties { get; init; } = new();
    public SystemPredictions SystemWidePredictions { get; init; } = new();
}

public class MonthlyMaintenanceTrend
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int TotalRequests { get; init; }
    public int EmergencyRequests { get; init; }
    public Dictionary<string, int> CategoryBreakdown { get; init; } = new();
}

public class PropertyRiskAssessment
{
    public string PropertyName { get; init; } = string.Empty;
    public string PropertyCode { get; init; } = string.Empty;
    public double RiskScore { get; init; }
    public string PredictedMaintenanceNeed { get; init; } = string.Empty;
    public List<string> RecommendedActions { get; init; } = new();
}

public class SystemPredictions
{
    public double PredictedRequestVolumeNextMonth { get; init; }
    public int PredictedHighRiskProperties { get; init; }
    public List<string> RecommendedActions { get; init; } = new();
}

#endregion
