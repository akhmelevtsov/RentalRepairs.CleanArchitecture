using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Tenants.GetTenantsByProperty;

/// <summary>
/// Query handler to get tenants by property using direct EF Core access
/// </summary>
public class GetTenantsByPropertyQueryHandler : IRequestHandler<GetTenantsByPropertyQuery, List<TenantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantsByPropertyQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantDto>> Handle(GetTenantsByPropertyQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tenants
            .Include(t => t.Requests)
            .AsQueryable();

        if (request.PropertyId != Guid.Empty)
        {
            query = query.Where(t => t.PropertyId == request.PropertyId);
        }

        if (request.WithActiveRequestsOnly)
        {
            query = query.Where(t => t.Requests.Any(r => r.Status == Domain.Enums.TenantRequestStatus.Submitted || 
                                                        r.Status == Domain.Enums.TenantRequestStatus.Scheduled));
        }

        var tenants = await query
            .OrderBy(t => t.UnitNumber)
            .ToListAsync(cancellationToken);

        return tenants.Adapt<List<TenantDto>>();
    }
}