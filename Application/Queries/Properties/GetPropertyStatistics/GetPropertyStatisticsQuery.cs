using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Queries.Properties.GetPropertyStatistics;

/// <summary>
/// Query to retrieve detailed statistics for a specific property.
/// Used for property management and performance analysis.
/// </summary>
public class GetPropertyStatisticsQuery : IQuery<PropertyStatisticsDto>
{
    public Guid PropertyId { get; set; }

    public GetPropertyStatisticsQuery(Guid propertyId)
    {
        PropertyId = propertyId;
    }
}

/// <summary>
/// DTO for property statistics data.
/// Contains occupancy and performance metrics for individual properties.
/// </summary>
public class PropertyStatisticsDto
{
    public string PropertyName { get; set; } = default!;
    public string PropertyCode { get; set; } = default!;
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int AvailableUnits { get; set; }
    public double OccupancyRate { get; set; }
    public string SuperintendentName { get; set; } = default!;
    public string SuperintendentEmail { get; set; } = default!;
}