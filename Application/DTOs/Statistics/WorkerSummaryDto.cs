namespace RentalRepairs.Application.DTOs.Statistics;

/// <summary>
/// DTO for worker summary statistics providing aggregated insights about the workforce.
/// Used for workforce management dashboard and capacity planning.
/// </summary>
public class WorkerSummaryDto
{
    public int TotalWorkers { get; set; }
    public int ActiveWorkers { get; set; }
    public int InactiveWorkers { get; set; }
    public int WorkersWithSpecialization { get; set; }
    public int WorkersWithActiveAssignments { get; set; }
    public int TotalActiveAssignments { get; set; }
    public int TotalCompletedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public double AverageAssignmentsPerWorker { get; set; }
    public Dictionary<string, int> SpecializationBreakdown { get; set; } = new();
}