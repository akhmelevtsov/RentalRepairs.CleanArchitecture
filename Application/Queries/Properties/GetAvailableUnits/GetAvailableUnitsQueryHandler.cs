using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Queries.Properties.GetAvailableUnits;

/// <summary>
/// Handler for getting available units using domain logic.
/// Delegates business calculations to the Property aggregate.
/// </summary>
public class GetAvailableUnitsQueryHandler : IRequestHandler<GetAvailableUnitsQuery, List<string>>
{
    private readonly IApplicationDbContext _context;

    public GetAvailableUnitsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles getting available units using domain specifications and business logic.
    /// </summary>
    /// <param name="request">The available units request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available unit numbers</returns>
    public async Task<List<string>> Handle(GetAvailableUnitsQuery request, CancellationToken cancellationToken)
    {
        // Load property with tenants using EF Core
        var property = await _context.Properties
            .Include(p => p.Tenants)
            .Where(p => p.Code == request.PropertyCode)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (property == null)
        {
            throw new ArgumentException($"Property with code '{request.PropertyCode}' not found");
        }

        // ? Use DOMAIN LOGIC - Property aggregate calculates available units
        return property.GetAvailableUnits().ToList();
    }
}