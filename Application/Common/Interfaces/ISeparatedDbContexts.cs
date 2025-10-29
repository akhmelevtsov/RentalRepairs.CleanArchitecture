using Microsoft.EntityFrameworkCore;

namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// ? PROPER CQRS: Separate read context optimized for queries
/// - No change tracking (AsNoTracking by default)
/// - Optimized connection strings (read replicas)
/// - Different indexes and optimizations than write DB
/// </summary>
public interface IReadDbContext
{
    // Read-optimized DbSets with no change tracking
    DbSet<TenantRequestListItemReadModel> TenantRequestListItems { get; }
    DbSet<TenantRequestDetailsReadModel> TenantRequestDetails { get; }
    
    // Raw SQL support for complex analytics
    Task<List<T>> QueryAsync<T>(string sql, params object[] parameters) where T : class;
}

/// <summary>
/// ? PROPER CQRS: Commands only work with write context
/// - Full change tracking
/// - Domain events
/// - Transaction support
/// - Optimized for writes
/// </summary>
public interface IWriteDbContext
{
    // Domain entities for commands only
    DbSet<Domain.Entities.TenantRequest> TenantRequests { get; }
    DbSet<Domain.Entities.Tenant> Tenants { get; }
    DbSet<Domain.Entities.Property> Properties { get; }
    DbSet<Domain.Entities.Worker> Workers { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}