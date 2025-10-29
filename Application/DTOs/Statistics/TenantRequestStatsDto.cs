namespace RentalRepairs.Application.DTOs.Statistics;

/// <summary>
/// DTO for tenant request statistics providing comprehensive metrics about request workflows.
/// Used for analytics dashboard and performance reporting.
/// </summary>
public class TenantRequestStatsDto
{
    public int TotalRequests { get; set; }
    public int DraftRequests { get; set; }
    public int SubmittedRequests { get; set; }
    public int ScheduledRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int FailedRequests { get; set; }
    public int DeclinedRequests { get; set; }
    public int ClosedRequests { get; set; }
    public int EmergencyRequests { get; set; }
    public int HighUrgencyRequests { get; set; }
    public int NormalRequests { get; set; }
    public int LowUrgencyRequests { get; set; }
    public int RequestsScheduledToday { get; set; }
    public int OverdueRequests { get; set; }
    public double AverageCompletionDays { get; set; }
}