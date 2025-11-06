namespace RentalRepairs.Application.Common.Configuration;

/// <summary>
/// Configuration settings for WorkerService behavior.
/// </summary>
public class WorkerServiceSettings
{
    /// <summary>
    /// Maximum number of workers to return from availability queries.
    /// Default: 10
    /// </summary>
    public int MaxAvailableWorkers { get; set; } = 10;

    /// <summary>
    /// Number of days to look ahead when calculating worker availability.
    /// Default: 30
    /// </summary>
    public int BookingLookAheadDays { get; set; } = 30;

    /// <summary>
    /// Number of suggested dates to generate for worker assignment.
    /// Default: 7
    /// </summary>
    public int SuggestedDatesCount { get; set; } = 7;
}