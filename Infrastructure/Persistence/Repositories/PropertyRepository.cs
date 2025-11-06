using Microsoft.EntityFrameworkCore;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Specifications;

namespace RentalRepairs.Infrastructure.Persistence.Repositories;

/// <summary>
/// ? Fixed PropertyRepository - inherits from BaseRepository and properly implements interface
/// Clean interface implementation without duplicate base methods
/// </summary>
public class PropertyRepository : BaseRepository<Property>, IPropertyRepository
{
    public PropertyRepository(ApplicationDbContext context) : base(context)
    {
    }

    #region ? Specific Property Methods - Clean Interface Implementation

    public async Task<Property?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.Properties
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    /// <summary>
    /// ? Check if property exists by code (renamed to avoid ambiguity with base ExistsAsync)
    /// </summary>
    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.Properties
            .AnyAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<Property>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        return await Context.Properties
            .Where(p => p.Address.City == city)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Property>> GetBySuperintendentEmailAsync(string email,
        CancellationToken cancellationToken = default)
    {
        return await Context.Properties
            .Where(p => p.Superintendent.EmailAddress == email)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region ? Specification Pattern Implementation

    public async Task<IEnumerable<Property>> GetBySpecificationAsync(ISpecification<Property> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    #endregion
}