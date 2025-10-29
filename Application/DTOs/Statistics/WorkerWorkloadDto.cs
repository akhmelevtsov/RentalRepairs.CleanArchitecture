namespace RentalRepairs.Application.DTOs.Statistics;

/// <summary>
/// DTO for worker workload information providing detailed metrics about individual worker performance.
/// Used for worker management and assignment optimization.
/// </summary>
public class WorkerWorkloadDto
{
    public Guid WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string WorkerEmail { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TotalAssignments { get; set; }
    public int ActiveAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int FailedAssignments { get; set; }
    public int UpcomingAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public DateTime? NextScheduledWork { get; set; }
    public double AverageCompletionDays { get; set; }
}