using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Domain.Extensions;

/// <summary>
/// ✅ STEP 3: Enhanced extension methods for TenantRequest collections.
/// Provides comprehensive domain-specific operations following DDD patterns.
/// Builds on Step 2 aggregate methods and existing collection operations.
/// </summary>
public static class TenantRequestCollectionExtensions
{
    #region Existing Methods (Preserved)

    /// <summary>
    /// Business logic: Calculates quality score for a collection of requests.
    /// Moved from DashboardBusinessService.CalculateQualityScore.
    /// </summary>
    public static double CalculateQualityScore(this IEnumerable<TenantRequest> requests)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return 100.0;
        }

        int failedRequests = requestList.Count(r => r.Status == TenantRequestStatus.Failed);
        double successRate = (double)(requestList.Count - failedRequests) / requestList.Count;
        return successRate * 100;
    }

    /// <summary>
    /// Business logic: Calculates average resolution time.
    /// Moved from DashboardBusinessService.CalculateAverageResolutionTime.
    /// </summary>
    public static TimeSpan CalculateAverageResolutionTime(this IEnumerable<TenantRequest> requests)
    {
        var completedRequests = requests.Where(r => r.CompletedDate.HasValue).ToList();
        if (!completedRequests.Any())
        {
            return TimeSpan.Zero;
        }

        double totalHours = completedRequests.Average(r => (r.CompletedDate!.Value - r.CreatedAt).TotalHours);
        return TimeSpan.FromHours(totalHours);
    }

    /// <summary>
    /// Business logic: Gets requests requiring immediate attention.
    /// Moved from DashboardBusinessService.IdentifyUrgentItems logic.
    /// </summary>
    public static IEnumerable<TenantRequest> GetRequestsRequiringAttention(this IEnumerable<TenantRequest> requests)
    {
        return requests.Where(r => r.RequiresImmediateAttention());
    }

    /// <summary>
    /// Business logic: Calculates first call resolution rate.
    /// Moved from DashboardBusinessService.CalculateFirstCallResolutionRate.
    /// </summary>
    public static double CalculateFirstCallResolutionRate(this IEnumerable<TenantRequest> requests)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return 100.0;
        }

        int resolved = requestList.Count(r => r.IsFirstCallResolution());
        return (double)resolved / requestList.Count * 100;
    }

    /// <summary>
    /// Business logic: Calculates overall efficiency score.
    /// Moved from DashboardBusinessService.CalculateOverallEfficiencyScore.
    /// </summary>
    public static double CalculateOverallEfficiencyScore(this IEnumerable<TenantRequest> requests, TenantRequestStatusPolicy statusPolicy)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return 100.0;
        }

        int completedOnTime = requestList.Count(r => 
            statusPolicy.IsCompletedStatus(r.Status) && !r.IsOverdue(statusPolicy));
        
        return (double)completedOnTime / requestList.Count * 100;
    }

    /// <summary>
    /// Business logic: Estimates customer satisfaction score.
    /// Moved from DashboardBusinessService.EstimateCustomerSatisfactionScore.
    /// </summary>
    public static double EstimateCustomerSatisfactionScore(this IEnumerable<TenantRequest> completedRequests)
    {
        var requestList = completedRequests.ToList();
        if (!requestList.Any())
        {
            return 85.0; // Default
        }

        int onTimeResolutions = requestList.Count(r => r.WasResolvedOnTime());
        double satisfactionBase = (double)onTimeResolutions / requestList.Count * 100;
        
        // Adjust for emergency handling
        var emergencyRequests = requestList.Where(r => r.IsEmergency).ToList();
        if (emergencyRequests.Any())
        {
            int emergencyHandledWell = emergencyRequests.Count(r => r.WasEmergencyHandledWell());
            double emergencyScore = (double)emergencyHandledWell / emergencyRequests.Count * 100;
            satisfactionBase = (satisfactionBase + emergencyScore) / 2;
        }

        return Math.Max(0, Math.Min(100, satisfactionBase));
    }

    /// <summary>
    /// Business logic: Calculates resource utilization rate.
    /// Moved from DashboardBusinessService.CalculateResourceUtilizationRate.
    /// </summary>
    public static double CalculateResourceUtilizationRate(this IEnumerable<TenantRequest> requests)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return 0;
        }

        int assignedRequests = requestList.Count(r => !string.IsNullOrEmpty(r.AssignedWorkerEmail));
        return (double)assignedRequests / requestList.Count * 100;
    }

    /// <summary>
    /// Business logic: Groups requests by urgency level.
    /// Moved from DashboardBusinessService.GroupRequestsByUrgency.
    /// </summary>
    public static Dictionary<string, int> GroupByUrgency(this IEnumerable<TenantRequest> requests)
    {
        return requests.GroupBy(r => r.UrgencyLevel)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Business logic: Calculates request age distribution.
    /// Moved from DashboardBusinessService.CalculateRequestAgeDistribution.
    /// </summary>
    public static Dictionary<string, int> CalculateAgeDistribution(this IEnumerable<TenantRequest> requests)
    {
        var requestList = requests.ToList();
        
        return new Dictionary<string, int>
        {
            ["0-1 days"] = requestList.Count(r => r.GetAgeCategory() == "0-1 days"),
            ["1-3 days"] = requestList.Count(r => r.GetAgeCategory() == "1-3 days"),
            ["3-7 days"] = requestList.Count(r => r.GetAgeCategory() == "3-7 days"),
            ["7+ days"] = requestList.Count(r => r.GetAgeCategory() == "7+ days")
        };
    }

    /// <summary>
    /// Business logic: Identifies overloaded resources.
    /// Moved from DashboardBusinessService.IdentifyOverloadedResources.
    /// </summary>
    public static List<string> IdentifyOverloadedResources(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => !string.IsNullOrEmpty(r.AssignedWorkerEmail))
            .GroupBy(r => r.AssignedWorkerEmail)
            .Where(g => g.Count() > 5)
            .Select(g => g.Key!)
            .ToList();
    }

    /// <summary>
    /// Business logic: Identifies top issue categories.
    /// Moved from DashboardBusinessService.IdentifyTopIssueCategories.
    /// </summary>
    public static List<string> IdentifyTopIssueCategories(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .GroupBy(r => r.DetermineCategoryFromDescription())
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();
    }

    /// <summary>
    /// Business logic: Filters requests by status using status policy.
    /// </summary>
    public static IEnumerable<TenantRequest> FilterByActiveStatus(this IEnumerable<TenantRequest> requests, TenantRequestStatusPolicy statusPolicy)
    {
        return requests.Where(r => statusPolicy.IsActiveStatus(r.Status));
    }

    /// <summary>
    /// Business logic: Filters requests by completed status using status policy.
    /// </summary>
    public static IEnumerable<TenantRequest> FilterByCompletedStatus(this IEnumerable<TenantRequest> requests, TenantRequestStatusPolicy statusPolicy)
    {
        return requests.Where(r => statusPolicy.IsCompletedStatus(r.Status));
    }

    /// <summary>
    /// Business logic: Gets overdue requests.
    /// </summary>
    public static IEnumerable<TenantRequest> GetOverdueRequests(this IEnumerable<TenantRequest> requests, TenantRequestStatusPolicy? statusPolicy = null)
    {
        return requests.Where(r => r.IsOverdue(statusPolicy));
    }

    /// <summary>
    /// Business logic: Calculates percentage change between two request collections.
    /// Moved from DashboardBusinessService.CalculatePercentageChange logic.
    /// </summary>
    public static double CalculatePercentageChangeFrom(this IEnumerable<TenantRequest> current, IEnumerable<TenantRequest> previous)
    {
        int currentCount = current.Count();
        int previousCount = previous.Count();
        
        if (previousCount == 0)
        {
            return currentCount > 0 ? 100 : 0;
        }

        return ((double)(currentCount - previousCount) / previousCount) * 100;
    }

    /// <summary>
    /// Business logic: Calculates trend analysis between two time periods.
    /// Moved from DashboardBusinessService.CalculateResolutionTimeTrend logic.
    /// </summary>
    public static double CalculateResolutionTimeTrend(this IEnumerable<TenantRequest> current, 
        IEnumerable<TenantRequest> previous, 
        TenantRequestStatusPolicy statusPolicy)
    {
        IEnumerable<TenantRequest> previousCompleted = previous.FilterByCompletedStatus(statusPolicy);
        IEnumerable<TenantRequest> currentCompleted = current.FilterByCompletedStatus(statusPolicy);

        double previousAvg = previousCompleted.CalculateAverageResolutionTime().TotalHours;
        double currentAvg = currentCompleted.CalculateAverageResolutionTime().TotalHours;
        
        if (previousAvg == 0)
        {
            return 0;
        }

        return ((currentAvg - previousAvg) / previousAvg) * 100;
    }

    #endregion

    #region ✅ STEP 3: Enhanced Status-Based Operations

    /// <summary>
    /// ✅ STEP 3: Gets requests that require immediate attention with priority ordering.
    /// Enhanced version using aggregate methods from Step 2.
    /// </summary>
    public static List<TenantRequest> RequiringImmediateAttention(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.RequiresImmediateAttention())
            .OrderByDescending(r => r.CalculateUrgencyPriority())
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets requests in specific statuses with fluent interface.
    /// </summary>
    public static List<TenantRequest> WithStatuses(this IEnumerable<TenantRequest> requests, params TenantRequestStatus[] statuses)
    {
        return requests
            .Where(r => statuses.Contains(r.Status))
            .OrderByDescending(r => r.IsEmergency)
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets active (in-progress) requests using aggregate method.
    /// </summary>
    public static List<TenantRequest> GetActiveRequests(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.IsActive())
            .OrderByDescending(r => r.RequiresImmediateAttention())
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets emergency requests with intelligent ordering.
    /// </summary>
    public static List<TenantRequest> GetEmergencyRequests(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.IsEmergency)
            .OrderBy(r => r.CreatedAt)
            .ToList();
    }

    #endregion

    #region ✅ STEP 3: Time-Based Operations

    /// <summary>
    /// ✅ STEP 3: Gets requests created within specified timeframe.
    /// </summary>
    public static List<TenantRequest> CreatedWithinDays(this IEnumerable<TenantRequest> requests, int days)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-days);
        return requests
            .Where(r => r.CreatedAt >= cutoff)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets requests scheduled for a specific date range.
    /// </summary>
    public static List<TenantRequest> ScheduledBetween(this IEnumerable<TenantRequest> requests, DateTime startDate, DateTime endDate)
    {
        return requests
            .Where(r => r.ScheduledDate.HasValue && 
                       r.ScheduledDate.Value.Date >= startDate.Date && 
                       r.ScheduledDate.Value.Date <= endDate.Date)
            .OrderBy(r => r.ScheduledDate)
            .ThenByDescending(r => r.IsEmergency)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets requests by age category using aggregate method.
    /// </summary>
    public static List<TenantRequest> ByAgeCategory(this IEnumerable<TenantRequest> requests, string ageCategory)
    {
        return requests
            .Where(r => r.GetAgeCategory() == ageCategory)
            .ToList();
    }

    #endregion

    #region ✅ STEP 3: Enhanced Performance Analytics

    /// <summary>
    /// ✅ STEP 3: Calculates comprehensive performance metrics using aggregate methods.
    /// </summary>
    public static RequestPerformanceMetrics CalculateComprehensivePerformanceMetrics(this IEnumerable<TenantRequest> requests)
    {
        var requestList = requests.ToList();
        var completedRequests = requestList.Where(r => r.Status == TenantRequestStatus.Done).ToList();

        return new RequestPerformanceMetrics
        {
            TotalRequests = requestList.Count,
            CompletedRequests = completedRequests.Count,
            PendingRequests = requestList.Count(r => r.Status == TenantRequestStatus.Submitted),
            OverdueRequests = requestList.Count(r => r.IsOverdue()),
            EmergencyRequests = requestList.Count(r => r.IsEmergency),
            AverageResolutionTimeHours = requestList.CalculateAverageResolutionTime().TotalHours,
            FirstCallResolutionRate = requestList.CalculateFirstCallResolutionRate(),
            AveragePerformanceScore = completedRequests.Any() ? 
                completedRequests.Average(r => r.CalculateResolutionPerformanceScore()) : 0.0
        };
    }

    #endregion

    #region ✅ STEP 3: Enhanced Categorization and Grouping

    /// <summary>
    /// ✅ STEP 3: Groups requests by category using aggregate method.
    /// </summary>
    public static Dictionary<string, List<TenantRequest>> GroupByCategory(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .GroupBy(r => r.DetermineCategoryFromDescription())
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).ToList());
    }

    /// <summary>
    /// ✅ STEP 3: Groups requests by urgency level with enhanced data.
    /// </summary>
    public static Dictionary<string, List<TenantRequest>> GroupByUrgencyLevel(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .GroupBy(r => r.UrgencyLevel)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.CreatedAt).ToList());
    }

    /// <summary>
    /// ✅ STEP 3: Groups requests by property for property-level analysis.
    /// </summary>
    public static Dictionary<string, List<TenantRequest>> GroupByProperty(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .GroupBy(r => r.PropertyName)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).ToList());
    }

    #endregion

    #region ✅ STEP 3: Worker Assignment Support

    /// <summary>
    /// ✅ STEP 3: Gets requests assigned to specific worker.
    /// </summary>
    public static List<TenantRequest> AssignedToWorker(this IEnumerable<TenantRequest> requests, string workerEmail)
    {
        return requests
            .Where(r => !string.IsNullOrEmpty(r.AssignedWorkerEmail) && 
                       r.AssignedWorkerEmail.Equals(workerEmail, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.ScheduledDate ?? DateTime.MaxValue)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets unassigned requests that need worker assignment.
    /// </summary>
    public static List<TenantRequest> RequiringWorkerAssignment(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.Status == TenantRequestStatus.Submitted && string.IsNullOrEmpty(r.AssignedWorkerEmail))
            .OrderByDescending(r => r.IsEmergency)
            .ThenByDescending(r => r.CalculateUrgencyPriority())
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets requests requiring specific specialization.
    /// </summary>
    public static List<TenantRequest> RequiringSpecialization(this IEnumerable<TenantRequest> requests, string specialization)
    {
        return requests
            .Where(r => Worker.DetermineRequiredSpecialization(r.Title, r.Description)
                       .Equals(specialization, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.IsEmergency)
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    #endregion

    #region ✅ STEP 3: Quality and Satisfaction Analysis

    /// <summary>
    /// ✅ STEP 3: Gets requests with successful completion.
    /// </summary>
    public static List<TenantRequest> CompletedSuccessfully(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.Status == TenantRequestStatus.Done && 
                       r.WorkCompletedSuccessfully == true)
            .OrderByDescending(r => r.CompletedDate)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets requests that were resolved on time using aggregate method.
    /// </summary>
    public static List<TenantRequest> ResolvedOnTime(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.WasResolvedOnTime())
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets emergency requests that were handled well using aggregate method.
    /// </summary>
    public static List<TenantRequest> EmergenciesHandledWell(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.IsEmergency && r.WasEmergencyHandledWell())
            .ToList();
    }

    #endregion

    #region ✅ STEP 3: Dashboard and Planning Support

    /// <summary>
    /// ✅ STEP 3: Gets urgent requests for dashboard display.
    /// </summary>
    public static List<TenantRequest> GetUrgentRequests(this IEnumerable<TenantRequest> requests)
    {
        return requests
            .Where(r => r.IsEmergency || r.IsOverdue() || r.RequiresImmediateAttention())
            .OrderByDescending(r => r.CalculateUrgencyPriority())
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Gets requests for daily planning.
    /// </summary>
    public static List<TenantRequest> ForDailyPlanning(this IEnumerable<TenantRequest> requests, DateTime date)
    {
        return requests
            .Where(r => r.ScheduledDate.HasValue && r.ScheduledDate.Value.Date == date.Date)
            .OrderBy(r => r.ScheduledDate)
            .ThenByDescending(r => r.IsEmergency)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 3: Calculates requests requiring attention count.
    /// </summary>
    public static int CountRequiringAttention(this IEnumerable<TenantRequest> requests)
    {
        return requests.Count(r => r.RequiresImmediateAttention() || r.IsOverdue());
    }

    #endregion
}

/// <summary>
/// ✅ STEP 3: Enhanced value object for comprehensive request performance metrics.
/// </summary>
public class RequestPerformanceMetrics
{
    public int TotalRequests { get; init; }
    public int CompletedRequests { get; init; }
    public int PendingRequests { get; init; }
    public int OverdueRequests { get; init; }
    public int EmergencyRequests { get; init; }
    public double AverageResolutionTimeHours { get; init; }
    public double FirstCallResolutionRate { get; init; }
    public double AveragePerformanceScore { get; init; }
    
    public double CompletionRate => TotalRequests > 0 ? (double)CompletedRequests / TotalRequests * 100 : 0;
    public double OverdueRate => TotalRequests > 0 ? (double)OverdueRequests / TotalRequests * 100 : 0;
    public double EmergencyRate => TotalRequests > 0 ? (double)EmergencyRequests / TotalRequests * 100 : 0;
    public double OnTimeResolutionRate => CompletedRequests > 0 ? (100 - OverdueRate) : 0;
}
