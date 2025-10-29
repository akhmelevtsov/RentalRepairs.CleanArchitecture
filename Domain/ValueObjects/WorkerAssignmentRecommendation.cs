using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Recommendation for worker assignment with scoring and reasoning.
/// Value object containing all information needed for assignment recommendations.
/// </summary>
public class WorkerAssignmentRecommendation
{
    public required Worker Worker { get; set; }
    public int Score { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public TimeSpan EstimatedCompletionTime { get; set; }
}
