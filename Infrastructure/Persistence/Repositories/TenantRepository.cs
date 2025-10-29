using Microsoft.EntityFrameworkCore;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Specifications;

namespace RentalRepairs.Infrastructure.Persistence.Repositories;

/// <summary>
/// ? Fixed TenantRepository - inherits from BaseRepository and properly implements interface
/// Updated to use Guid-based entities and implements all required interface methods
/// Fixed to work with domain entities that use foreign keys instead of navigation properties
/// </summary>
public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    #region ? ITenantRepository Interface Implementation

    /// <summary>
    /// ? Fixed: Use Guid for propertyId parameter
    /// </summary>
    public async Task<Tenant?> GetByPropertyAndUnitAsync(Guid propertyId, string unitNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Tenants
            .FirstOrDefaultAsync(t => t.PropertyId == propertyId && t.UnitNumber == unitNumber, cancellationToken);
    }

    /// <summary>
    /// ? Fixed: Use Guid for propertyId parameter
    /// </summary>
    public async Task<IEnumerable<Tenant>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        return await Context.Tenants
            .Where(t => t.PropertyId == propertyId)
            .OrderBy(t => t.UnitNumber)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// ? Implemented: Get tenants with active requests
    /// </summary>
    public async Task<IEnumerable<Tenant>> GetWithActiveRequestsAsync(CancellationToken cancellationToken = default)
    {
        var activeStatuses = new[] 
        { 
            Domain.Enums.TenantRequestStatus.Draft,
            Domain.Enums.TenantRequestStatus.Submitted, 
            Domain.Enums.TenantRequestStatus.Scheduled 
        };

        return await Context.Tenants
            .Where(t => Context.TenantRequests
                .Any(tr => tr.TenantId == t.Id && activeStatuses.Contains(tr.Status)))
            .OrderBy(t => t.ContactInfo.LastName)
            .ThenBy(t => t.ContactInfo.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// ? Implemented: Check if tenant exists in specific unit
    /// Fixed to join with Properties table instead of using navigation property
    /// </summary>
    public async Task<bool> ExistsInUnitAsync(string propertyCode, string unitNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Tenants
            .Where(t => t.PropertyCode == propertyCode && t.UnitNumber == unitNumber)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// ? Implemented: Get tenant request through tenant aggregate
    /// Fixed: Use Guid for tenantRequestId parameter
    /// </summary>
    public async Task<TenantRequest?> GetTenantRequestByIdAsync(Guid tenantRequestId, CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == tenantRequestId, cancellationToken);
    }

    /// <summary>
    /// ? Implemented: Specification pattern support
    /// </summary>
    public async Task<IEnumerable<Tenant>> GetBySpecificationAsync(ISpecification<Tenant> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    #endregion

    #region ? Additional Helper Methods

    /// <summary>
    /// Helper method: Get tenant by email (commonly used)
    /// </summary>
    public async Task<Tenant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Tenants
            .FirstOrDefaultAsync(t => t.ContactInfo.EmailAddress == email, cancellationToken);
    }

    #endregion
}