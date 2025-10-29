namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
///     Value object for property metrics calculation results.
/// </summary>
public class PropertyMetrics
{
    public int TotalUnits { get; init; }
    public int OccupiedUnits { get; init; }
    public int VacantUnits { get; init; }
    public double OccupancyRate { get; init; }
    public bool RequiresAttention { get; init; }
}
