using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetPropertyByCode;

/// <summary>
/// Query handler to get property by code with direct EF Core access
/// </summary>
public class GetPropertyByCodeQueryHandler : IRequestHandler<GetPropertyByCodeQuery, PropertyDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPropertyByCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PropertyDto?> Handle(GetPropertyByCodeQuery request, CancellationToken cancellationToken)
    {
        var property = await _context.Properties
            .Include(p => p.Tenants)
            .FirstOrDefaultAsync(p => p.Code == request.Code, cancellationToken);

        return property?.Adapt<PropertyDto>();
    }
}