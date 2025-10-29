using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetAllProperties;

/// <summary>
/// Query handler for property-related queries using direct EF Core access
/// </summary>
public class GetAllPropertiesQueryHandler : IRequestHandler<GetAllPropertiesQuery, IEnumerable<PropertyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllPropertiesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PropertyDto>> Handle(GetAllPropertiesQuery request, CancellationToken cancellationToken)
    {
        var properties = await _context.Properties
            .Include(p => p.Tenants)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return properties.Adapt<IEnumerable<PropertyDto>>();
    }
}
