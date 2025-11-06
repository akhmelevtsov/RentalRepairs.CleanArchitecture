using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Models;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetProperties;

/// <summary>
/// Query handler to get properties with paging and filtering
/// </summary>
public class GetPropertiesQueryHandler : IRequestHandler<GetPropertiesQuery, PagedResult<PropertyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPropertiesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PropertyDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Properties
            .Include(p => p.Tenants)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.City)) query = query.Where(p => p.Address.City.Contains(request.City));

        if (!string.IsNullOrEmpty(request.SuperintendentEmail))
            query = query.Where(p => p.Superintendent.EmailAddress == request.SuperintendentEmail);

        if (request.WithTenants.HasValue)
        {
            if (request.WithTenants.Value)
                query = query.Where(p => p.Tenants.Any());
            else
                query = query.Where(p => !p.Tenants.Any());
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply paging and get results
        var properties = await query
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var dtos = properties.Adapt<List<PropertyDto>>();

        return new PagedResult<PropertyDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}