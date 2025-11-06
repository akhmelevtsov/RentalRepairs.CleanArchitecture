using RentalRepairs.Domain.Common;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
///     Value object for property metrics calculation results.
///     Immutable representation of property occupancy and status metrics.
/// </summary>
public sealed class PropertyMetrics : ValueObject
{
    public int TotalUnits { get; init; }
    public int OccupiedUnits { get; init; }
    public int VacantUnits { get; init; }
    public double OccupancyRate { get; init; }
    public bool RequiresAttention { get; init; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalUnits;
        yield return OccupiedUnits;
        yield return VacantUnits;
        yield return OccupancyRate;
        yield return RequiresAttention;
    }

    public override string ToString()
    {
        return
            $"Occupancy: {OccupiedUnits}/{TotalUnits} ({OccupancyRate:P1}) - {(RequiresAttention ? "Requires Attention" : "OK")}";
    }
}
