using RentalRepairs.Application.Commands.Properties.RegisterProperty;

namespace RentalRepairs.Application.DTOs.Statistics;

/// <summary>
/// DTO for property with occupancy and request statistics.
/// Used for detailed property dashboard and reporting.
/// </summary>
public class PropertyWithStatsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public PropertyAddressDto Address { get; set; } = new();
    public string PhoneNumber { get; set; } = string.Empty;
    public string NoReplyEmailAddress { get; set; } = string.Empty;
    public PersonContactInfoDto Superintendent { get; set; } = new();
    public List<string> Units { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    // Statistics
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int VacantUnits { get; set; }
    public double OccupancyRate { get; set; }
    public int ActiveRequestsCount { get; set; }
    public int TotalRequestsCount { get; set; }
}