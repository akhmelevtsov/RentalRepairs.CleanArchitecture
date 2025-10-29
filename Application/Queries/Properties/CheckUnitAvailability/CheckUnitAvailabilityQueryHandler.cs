using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Queries.Properties.CheckUnitAvailability;

/// <summary>
/// Handler for checking unit availability using domain logic.
/// Delegates business logic to the Property aggregate instead of duplicating it in application layer.
/// </summary>
public class CheckUnitAvailabilityQueryHandler : IRequestHandler<CheckUnitAvailabilityQuery, bool>
{
    private readonly IApplicationDbContext _context;

    public CheckUnitAvailabilityQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the unit availability check using domain specifications and business logic.
    /// </summary>
    /// <param name="request">The availability check request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unit is available, false otherwise</returns>
    public async Task<bool> Handle(CheckUnitAvailabilityQuery request, CancellationToken cancellationToken)
    {
        // Use domain specification pattern - load property with tenants
        var property = await _context.Properties
            .Include(p => p.Tenants)
            .Where(p => p.Code == request.PropertyCode && p.Units.Contains(request.UnitNumber))
            .FirstOrDefaultAsync(cancellationToken);
        
        if (property == null)
        {
            return false; // Property doesn't exist or unit doesn't exist
        }

        // ? Use DOMAIN LOGIC - Property aggregate handles the business rules
        return property.IsUnitAvailable(request.UnitNumber);
    }
}