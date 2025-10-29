using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Tenants.GetTenantById;

/// <summary>
/// Query handler to get tenant by ID with direct EF Core access
/// </summary>
public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTenantByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Requests)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        return tenant?.Adapt<TenantDto>();
    }
}
