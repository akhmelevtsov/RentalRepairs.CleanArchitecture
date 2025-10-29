using Microsoft.EntityFrameworkCore;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Specifications;

namespace RentalRepairs.Infrastructure.Persistence.Repositories;

/// <summary>
/// ? Fixed WorkerRepository - inherits from BaseRepository and properly implements interface
/// Clean interface implementation without duplicate base methods
/// </summary>
public class WorkerRepository : BaseRepository<Worker>, IWorkerRepository
{
    public WorkerRepository(ApplicationDbContext context) : base(context)
    {
    }

    #region ? Specific Worker Methods - Clean Interface Implementation

    public async Task<Worker?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Workers
            .FirstOrDefaultAsync(w => w.ContactInfo.EmailAddress == email, cancellationToken);
    }

    public async Task<IEnumerable<Worker>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default)
    {
        return await Context.Workers
            .Where(w => w.Specialization == specialization)
            .OrderBy(w => w.ContactInfo.LastName)
            .ThenBy(w => w.ContactInfo.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Worker>> GetActiveWorkersAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Workers
            .Where(w => w.IsActive)
            .OrderBy(w => w.ContactInfo.LastName)
            .ThenBy(w => w.ContactInfo.FirstName)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region ? Specification Pattern Implementation

    public async Task<IEnumerable<Worker>> GetBySpecificationAsync(ISpecification<Worker> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    #endregion
}