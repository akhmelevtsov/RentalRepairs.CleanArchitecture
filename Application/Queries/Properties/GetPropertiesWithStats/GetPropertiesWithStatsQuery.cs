using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs.Statistics;

namespace RentalRepairs.Application.Queries.Properties.GetPropertiesWithStats;

/// <summary>
/// Query to retrieve properties with comprehensive statistics.
/// Used for management dashboards and detailed reporting.
/// </summary>
public class GetPropertiesWithStatsQuery : IQuery<IEnumerable<PropertyWithStatsDto>>
{
}