using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Commands.Properties.RegisterProperty;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.DTOs.Statistics;

namespace RentalRepairs.Application.Queries.Properties.GetPropertiesWithStats;

/// <summary>
/// Query handler to get properties with statistics using direct EF Core with optimized queries
/// </summary>
public class
    GetPropertiesWithStatsQueryHandler : IRequestHandler<GetPropertiesWithStatsQuery, IEnumerable<PropertyWithStatsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPropertiesWithStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PropertyWithStatsDto>> Handle(GetPropertiesWithStatsQuery request,
        CancellationToken cancellationToken)
    {
        // Use direct EF Core with projections for better performance
        // Avoid using Mapster.Adapt inside EF projections as it can't be translated
        var propertiesWithStats = await _context.Properties
            .Select(p => new PropertyWithStatsDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                // Manual mapping instead of Mapster.Adapt to avoid translation issues
                Address = new PropertyAddressDto
                {
                    StreetNumber = p.Address.StreetNumber,
                    StreetName = p.Address.StreetName,
                    City = p.Address.City,
                    PostalCode = p.Address.PostalCode,
                    FullAddress = p.Address.StreetNumber + " " + p.Address.StreetName + ", " + p.Address.City + ", " +
                                  p.Address.PostalCode
                },
                PhoneNumber = p.PhoneNumber,
                NoReplyEmailAddress = p.NoReplyEmailAddress,
                // Manual mapping instead of Mapster.Adapt to avoid translation issues
                Superintendent = new PersonContactInfoDto
                {
                    FirstName = p.Superintendent.FirstName,
                    LastName = p.Superintendent.LastName,
                    EmailAddress = p.Superintendent.EmailAddress,
                    MobilePhone = p.Superintendent.MobilePhone,
                    FullName = p.Superintendent.FirstName + " " + p.Superintendent.LastName
                },
                Units = p.Units,
                TotalUnits = p.Units.Count,
                OccupiedUnits = p.Tenants.Count,
                VacantUnits = p.Units.Count - p.Tenants.Count,
                OccupancyRate = p.Units.Count > 0 ? (double)p.Tenants.Count / p.Units.Count * 100 : 0,
                ActiveRequestsCount = p.Tenants.SelectMany(t => t.Requests)
                    .Count(r => r.Status == Domain.Enums.TenantRequestStatus.Submitted ||
                                r.Status == Domain.Enums.TenantRequestStatus.Scheduled),
                TotalRequestsCount = p.Tenants.SelectMany(t => t.Requests).Count(),
                CreatedAt = p.CreatedAt
            })
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return propertiesWithStats;
    }
}