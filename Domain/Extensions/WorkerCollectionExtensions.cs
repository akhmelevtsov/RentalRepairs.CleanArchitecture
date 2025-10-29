using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Extensions;

/// <summary>
/// ✅ STEP 2: Extension methods for Worker collections.
/// Provides domain-specific operations on Worker collections following DDD patterns.
/// Encapsulates collection-level business logic outside of individual aggregates.
/// </summary>
public static class WorkerCollectionExtensions
{
    /// <summary>
    /// ✅ STEP 2: Gets workers available for emergency response.
    /// Encapsulates emergency worker selection business logic in domain extensions.
    /// </summary>
    /// <param name="workers">Collection of workers to filter</param>
    /// <returns>Workers capable of handling emergency requests</returns>
    public static List<Worker> GetAvailableForEmergency(this IEnumerable<Worker> workers)
    {
        return workers
            .Where(w => w.IsActive && w.IsEmergencyResponseCapable())
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 2: Finds the best worker match for a request.
    /// Uses Worker aggregate scoring methods in a collection operation.
    /// </summary>
    /// <param name="workers">Collection of workers to evaluate</param>
    /// <param name="request">The tenant request to match against</param>
    /// <returns>Best matching worker or null if none suitable</returns>
    public static Worker? FindBestMatchForRequest(this IEnumerable<Worker> workers, TenantRequest request)
    {
        return workers
            .Where(w => w.CanBeAssignedToRequest(request))
            .OrderByDescending(w => w.CalculateScoreForRequest(request))
            .FirstOrDefault();
    }

    /// <summary>
    /// ✅ STEP 2: Gets workers with specific specialization.
    /// Encapsulates specialization filtering logic in domain extensions.
    /// </summary>
    /// <param name="workers">Collection of workers to filter</param>
    /// <param name="specialization">Required specialization</param>
    /// <returns>Workers with the specified specialization</returns>
    public static List<Worker> WithSpecialization(this IEnumerable<Worker> workers, string specialization)
    {
        return workers
            .Where(w => w.IsActive && w.HasSpecializedSkills(specialization))
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 2: Gets workers available on a specific date.
    /// Uses Worker aggregate availability methods in collection operations.
    /// </summary>
    /// <param name="workers">Collection of workers to check</param>
    /// <param name="date">Date to check availability for</param>
    /// <returns>Workers available on the specified date</returns>
    public static List<Worker> AvailableOnDate(this IEnumerable<Worker> workers, DateTime date)
    {
        return workers
            .Where(w => w.IsActive && w.IsAvailableForWork(date))
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 2: Gets workers with light workload.
    /// Uses Worker aggregate workload methods to identify available capacity.
    /// </summary>
    /// <param name="workers">Collection of workers to evaluate</param>
    /// <param name="maxWorkload">Maximum workload threshold (default: 2)</param>
    /// <returns>Workers with workload below the threshold</returns>
    public static List<Worker> WithLightWorkload(this IEnumerable<Worker> workers, int maxWorkload = 2)
    {
        return workers
            .Where(w => w.IsActive && w.GetUpcomingWorkloadCount(DateTime.UtcNow) <= maxWorkload)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 2: Gets assignment recommendations for multiple workers.
    /// Uses Worker aggregate methods to generate rich recommendation data.
    /// </summary>
    /// <param name="workers">Collection of workers to evaluate</param>
    /// <param name="request">The tenant request to generate recommendations for</param>
    /// <param name="maxRecommendations">Maximum number of recommendations (default: 3)</param>
    /// <returns>Ordered list of worker assignment recommendations</returns>
    public static List<WorkerAssignmentRecommendation> GetAssignmentRecommendations(
        this IEnumerable<Worker> workers, 
        TenantRequest request, 
        int maxRecommendations = 3)
    {
        return workers
            .Where(w => w.CanBeAssignedToRequest(request))
            .Select(w => new WorkerAssignmentRecommendation
            {
                Worker = w,
                Score = w.CalculateScoreForRequest(request),
                Confidence = w.CalculateRecommendationConfidence(request),
                Reasoning = w.GenerateRecommendationReasoning(request),
                EstimatedCompletionTime = w.EstimateCompletionTime(request)
            })
            .OrderByDescending(r => r.Score)
            .Take(maxRecommendations)
            .ToList();
    }

    /// <summary>
    /// ✅ STEP 2: Groups workers by their specialization.
    /// Provides domain-specific grouping for specialization analysis.
    /// </summary>
    /// <param name="workers">Collection of workers to group</param>
    /// <returns>Dictionary of specialization to workers</returns>
    public static Dictionary<string, List<Worker>> GroupBySpecialization(this IEnumerable<Worker> workers)
    {
        return workers
            .Where(w => w.IsActive)
            .GroupBy(w => w.Specialization ?? "General Maintenance")
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// ✅ STEP 2: Calculates workload distribution across workers.
    /// Provides collection-level analytics using Worker aggregate methods.
    /// </summary>
    /// <param name="workers">Collection of workers to analyze</param>
    /// <returns>Workload distribution summary</returns>
    public static WorkloadDistribution CalculateWorkloadDistribution(this IEnumerable<Worker> workers)
    {
        var activeWorkers = workers.Where(w => w.IsActive).ToList();
        var workloads = activeWorkers.Select(w => w.GetUpcomingWorkloadCount(DateTime.UtcNow)).ToList();

        return new WorkloadDistribution
        {
            TotalWorkers = activeWorkers.Count,
            AverageWorkload = workloads.Any() ? workloads.Average() : 0,
            MaxWorkload = workloads.Any() ? workloads.Max() : 0,
            MinWorkload = workloads.Any() ? workloads.Min() : 0,
            OverloadedWorkers = activeWorkers.Count(w => w.GetUpcomingWorkloadCount(DateTime.UtcNow) > 5)
        };
    }
}

/// <summary>
/// Value object for workload distribution analysis.
/// </summary>
public class WorkloadDistribution
{
    public int TotalWorkers { get; init; }
    public double AverageWorkload { get; init; }
    public int MaxWorkload { get; init; }
    public int MinWorkload { get; init; }
    public int OverloadedWorkers { get; init; }
}
