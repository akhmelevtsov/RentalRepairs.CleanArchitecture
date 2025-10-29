namespace RentalRepairs.Application.DTOs.Statistics;

/// <summary>
/// DTO for property summary dashboard showing aggregated statistics across all properties.
/// Used for executive dashboard and high-level reporting.
/// </summary>
public class PropertySummaryDto
{
    public int TotalProperties { get; set; }
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int VacantUnits { get; set; }
    public double AverageOccupancyRate { get; set; }
    public int ActiveRequestsCount { get; set; }
    public int PropertiesWithActiveRequests { get; set; }
}