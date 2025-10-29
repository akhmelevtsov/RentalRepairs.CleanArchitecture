namespace RentalRepairs.Application.DTOs.Workers;

/// <summary>
/// DTO for worker assignment operations.
/// Used for assignment workflows, availability checking, and worker selection.
/// </summary>
public class WorkerAssignmentDto
{
    public Guid WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string WorkerEmail { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public bool IsAvailable { get; set; }
    public int CurrentWorkload { get; set; }
    public DateTime? NextAvailableDate { get; set; }
}