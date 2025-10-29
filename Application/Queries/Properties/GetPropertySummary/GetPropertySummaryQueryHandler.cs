using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs.Statistics;

namespace RentalRepairs.Application.Queries.Properties.GetPropertySummary;

/// <summary>
/// Query handler to get property summary for dashboard
/// </summary>
public class GetPropertySummaryQueryHandler : IRequestHandler<GetPropertySummaryQuery, PropertySummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetPropertySummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PropertySummaryDto> Handle(GetPropertySummaryQuery request, CancellationToken cancellationToken)
    {
        // Use direct EF Core aggregation for efficient summary calculation
        var summary = await _context.Properties
            .GroupBy(p => 1) // Group all properties together
            .Select(g => new PropertySummaryDto
            {
                TotalProperties = g.Count(),
                TotalUnits = g.Sum(p => p.Units.Count),
                OccupiedUnits = g.Sum(p => p.Tenants.Count),
                VacantUnits = g.Sum(p => p.Units.Count) - g.Sum(p => p.Tenants.Count),
                AverageOccupancyRate = g.Average(p => p.Units.Count > 0 ? (double)p.Tenants.Count / p.Units.Count * 100 : 0),
                ActiveRequestsCount = g.Sum(p => p.Tenants.SelectMany(t => t.Requests)
                    .Count(r => r.Status == Domain.Enums.TenantRequestStatus.Submitted || 
                               r.Status == Domain.Enums.TenantRequestStatus.Scheduled)),
                PropertiesWithActiveRequests = g.Count(p => p.Tenants.SelectMany(t => t.Requests)
                    .Any(r => r.Status == Domain.Enums.TenantRequestStatus.Submitted || 
                             r.Status == Domain.Enums.TenantRequestStatus.Scheduled))
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary ?? new PropertySummaryDto();
    }
}