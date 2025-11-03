using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// ✅ STEP 2: Pure domain service that delegates to Worker aggregate methods.
/// Demonstrates proper DDD pattern where domain services coordinate aggregate behavior
/// rather than implementing business logic that belongs in aggregates.
/// </summary>
public class WorkerAssignmentPolicyService
{
    /// <summary>
    /// ✅ STEP 2: Delegates to Worker aggregate method.
    /// Domain service now coordinates aggregates rather than implementing business logic.
    /// </summary>
    public bool CanAssignWorkerToRequest(Worker worker, TenantRequest request)
    {
        if (worker == null || request == null)
        {
            return false;
        }

        // ✅ STEP 2: Delegate to Worker aggregate method - proper DDD pattern
        return worker.CanBeAssignedToRequest(request);
    }

    /// <summary>
    /// ✅ STEP 2: Uses Worker aggregate methods for filtering and scoring.
    /// Domain service orchestrates multiple Worker aggregates but delegates logic to them.
    /// </summary>
    public List<Worker> FilterEligibleWorkers(TenantRequest request, List<Worker> availableWorkers)
    {
        if (request == null || availableWorkers == null)
        {
            return new List<Worker>();
        }

        var eligibleWorkers = new List<Worker>();

        foreach (Worker worker in availableWorkers)
        {
            // ✅ STEP 2: Use Worker aggregate method instead of domain service logic
            if (worker.CanBeAssignedToRequest(request))
            {
                eligibleWorkers.Add(worker);
            }
        }

        // ✅ STEP 2: Sort by Worker aggregate scoring method
        return eligibleWorkers.OrderByDescending(w => w.CalculateScoreForRequest(request)).ToList();
    }

    /// <summary>
    /// ✅ STEP 2: Uses Worker aggregate scoring method.
    /// Domain service coordinates but delegates scoring to the Worker aggregate.
    /// </summary>
    public Worker? FindBestWorker(TenantRequest request, List<Worker> eligibleWorkers)
    {
        if (request == null || eligibleWorkers == null || !eligibleWorkers.Any())
        {
            return null;
        }

        // ✅ STEP 2: Use Worker aggregate scoring method
        return eligibleWorkers.OrderByDescending(w => w.CalculateScoreForRequest(request)).First();
    }

    /// <summary>
    /// ✅ STEP 2: Uses Worker aggregate methods for auto-assignment logic.
    /// </summary>
    public bool CanAutoAssignRequest(TenantRequest request, List<Worker> availableWorkers)
    {
        if (request == null || availableWorkers == null)
        {
            return false;
        }

        // Business rule: Only emergency requests can be auto-assigned
        if (!request.IsEmergency)
        {
            return false;
        }

        // ✅ STEP 2: Use Worker aggregate methods for eligibility
        List<Worker> eligibleWorkers = FilterEligibleWorkers(request, availableWorkers);
        return eligibleWorkers.Any();
    }

    /// <summary>
    /// ✅ STEP 2: Delegates to Worker aggregate validation method.
    /// </summary>
    public AssignmentValidationResult ValidateBasicAssignment(Worker worker, TenantRequest request, DateTime scheduledDate)
    {
        if (worker == null)
        {
            return AssignmentValidationResult.Failure("Worker not found");
        }

        if (request == null)
        {
            return AssignmentValidationResult.Failure("Request not found");
        }

        // ✅ STEP 2: Delegate to Worker aggregate validation method
        return worker.ValidateAssignmentToRequest(request, scheduledDate);
    }

    /// <summary>
    /// ✅ STEP 2: Uses Worker aggregate methods for recommendations.
    /// Domain service coordinates multiple workers but delegates logic to Worker aggregates.
    /// </summary>
    public List<WorkerAssignmentRecommendation> GetAssignmentRecommendations(TenantRequest request, List<Worker> availableWorkers)
    {
        if (request == null || availableWorkers == null)
        {
            return new List<WorkerAssignmentRecommendation>();
        }

        List<Worker> eligibleWorkers = FilterEligibleWorkers(request, availableWorkers);
        var recommendations = new List<WorkerAssignmentRecommendation>();

        foreach (Worker? worker in eligibleWorkers.Take(3)) // Top 3 recommendations
        {
            // ✅ STEP 2: Use Worker aggregate methods instead of domain service methods
            int score = worker.CalculateScoreForRequest(request);
            double confidence = worker.CalculateRecommendationConfidence(request);
            string reasoning = worker.GenerateRecommendationReasoning(request);
            TimeSpan estimatedTime = worker.EstimateCompletionTime(request);

            recommendations.Add(new WorkerAssignmentRecommendation
            {
                Worker = worker,
                Score = score,
                Confidence = confidence,
                Reasoning = reasoning,
                EstimatedCompletionTime = estimatedTime
            });
        }

        return recommendations.OrderByDescending(r => r.Score).ToList();
    }

}
