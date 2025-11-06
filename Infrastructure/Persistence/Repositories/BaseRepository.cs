using Microsoft.EntityFrameworkCore;
using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Specifications;

namespace RentalRepairs.Infrastructure.Persistence.Repositories;

/// <summary>
/// ? Base repository implementation to ensure consistent IRepository<T> contract fulfillment
/// Updated to use Guid-based entities and implements all required interface methods
/// </summary>
public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;

    protected BaseRepository(ApplicationDbContext context)
    {
        Context = context;
    }

    /// <summary>
    /// ? Fixed: Use Guid instead of int for entity ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<T>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// ? Consistent implementation of GetAllAsync across all repositories
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<T>().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// ? Consistent implementation of AddAsync across all repositories
    /// </summary>
    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<T>().AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// ? Consistent implementation of Update across all repositories
    /// </summary>
    public virtual void Update(T entity)
    {
        Context.Set<T>().Update(entity);
    }

    /// <summary>
    /// ? Consistent implementation of Remove across all repositories
    /// </summary>
    public virtual void Remove(T entity)
    {
        Context.Set<T>().Remove(entity);
    }

    /// <summary>
    /// ? Consistent implementation of CountAsync across all repositories
    /// </summary>
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<T>().CountAsync(cancellationToken);
    }

    /// <summary>
    /// ? Added missing ExistsAsync method required by IRepository interface
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<T>().AnyAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// ? Consistent implementation of SaveChangesAsync across all repositories
    /// </summary>
    public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// ? Consistent specification pattern implementation
    /// </summary>
    protected virtual IQueryable<T> ApplySpecification(ISpecification<T> specification)
    {
        var query = Context.Set<T>().AsQueryable();

        if (specification.Criteria != null) query = query.Where(specification.Criteria);

        // Apply includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Apply string includes
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
            query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending != null)
            query = query.OrderByDescending(specification.OrderByDescending);

        return query;
    }
}