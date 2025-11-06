using Microsoft.EntityFrameworkCore;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Specifications;

namespace RentalRepairs.Infrastructure.Persistence.Repositories;

/// <summary>
/// ? Fixed TenantRequestRepository - inherits from BaseRepository and properly implements interface
/// Updated to use Guid-based entities and implements all required interface methods
/// Fixed to work with domain entities that use foreign keys instead of navigation properties
/// </summary>
public class TenantRequestRepository : BaseRepository<TenantRequest>, ITenantRequestRepository
{
    public TenantRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    #region ITenantRequestRepository Core Methods

    /// <summary>
    /// ? Get tenant request by unique code
    /// </summary>
    public async Task<TenantRequest?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Code == code, cancellationToken);
    }

    /// <summary>
    /// ? Check if tenant request exists by code (renamed to avoid ambiguity with base ExistsAsync)
    /// </summary>
    public async Task<bool> ExistsByCodeAsync(string requestCode, CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .AnyAsync(tr => tr.Code == requestCode, cancellationToken);
    }

    #endregion

    #region Status-Based Queries

    /// <summary>
    /// ? Get all tenant requests with specific status
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetByStatusAsync(TenantRequestStatus status,
        CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .Where(tr => tr.Status == status)
            .OrderBy(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// ? Count tenant requests by status
    /// </summary>
    public async Task<int> CountByStatusAsync(TenantRequestStatus status, CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .CountAsync(tr => tr.Status == status, cancellationToken);
    }

    /// <summary>
    /// ? Get all pending (submitted) tenant requests
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .Where(tr => tr.Status == TenantRequestStatus.Submitted)
            .OrderBy(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Entity Relationship Queries

    /// <summary>
    /// ? Fixed: Use Guid for tenantId parameter
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetByTenantIdAsync(Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .Where(tr => tr.TenantId == tenantId)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// ? Fixed: Use Guid for propertyId parameter and work with direct foreign key
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetByPropertyIdAsync(Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .Where(tr => tr.PropertyId == propertyId)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// ? Get all tenant requests assigned to specific worker
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetByWorkerEmailAsync(string workerEmail,
        CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .Where(tr => tr.AssignedWorkerEmail == workerEmail)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Business Logic Queries

    /// <summary>
    /// ? Get tenant requests by urgency level
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetByUrgencyLevelAsync(string urgencyLevel,
        CancellationToken cancellationToken = default)
    {
        return await Context.TenantRequests
            .Where(tr => tr.UrgencyLevel == urgencyLevel)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// ? Get overdue tenant requests older than specified threshold
    /// Fixed: Single clear method with explicit parameter
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetOverdueRequestsAsync(int daysThreshold,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysThreshold);
        return await Context.TenantRequests
            .Where(tr => tr.Status == TenantRequestStatus.Submitted && tr.CreatedAt <= cutoffDate)
            .OrderBy(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Specification Pattern

    /// <summary>
    /// ? Get tenant requests using specification pattern for complex queries
    /// </summary>
    public async Task<IEnumerable<TenantRequest>> GetBySpecificationAsync(ISpecification<TenantRequest> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    #endregion
}