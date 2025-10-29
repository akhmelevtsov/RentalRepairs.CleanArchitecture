using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetPropertyById;

/// <summary>
/// Query handler to get property by ID with direct EF Core access
/// </summary>
public class GetPropertyByIdQueryHandler : IRequestHandler<GetPropertyByIdQuery, PropertyDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPropertyByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PropertyDto?> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        var property = await _context.Properties
            .Include(p => p.Tenants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        return property?.Adapt<PropertyDto>();
    }
}
