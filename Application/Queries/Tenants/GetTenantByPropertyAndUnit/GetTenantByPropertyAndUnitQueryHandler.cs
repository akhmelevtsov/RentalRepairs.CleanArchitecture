using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Tenants.GetTenantByPropertyAndUnit;

/// <summary>
/// Query handler to get tenant by property and unit using direct EF Core access
/// </summary>
public class GetTenantByPropertyAndUnitQueryHandler : IRequestHandler<GetTenantByPropertyAndUnitQuery, TenantDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTenantByPropertyAndUnitQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantDto?> Handle(GetTenantByPropertyAndUnitQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Requests)
            .FirstOrDefaultAsync(t => t.PropertyId == request.PropertyId && t.UnitNumber == request.UnitNumber, cancellationToken);

        return tenant?.Adapt<TenantDto>();
    }
}
