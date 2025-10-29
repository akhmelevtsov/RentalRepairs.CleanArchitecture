using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs.Statistics;

namespace RentalRepairs.Application.Queries.Properties.GetPropertySummary;

/// <summary>
/// Query to retrieve aggregated property summary for executive dashboard.
/// Used for high-level overview and KPI reporting.
/// </summary>
public class GetPropertySummaryQuery : IQuery<PropertySummaryDto>
{
}