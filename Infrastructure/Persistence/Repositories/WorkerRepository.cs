using Microsoft.EntityFrameworkCore;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Specifications;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Infrastructure.Persistence.Repositories;

/// <summary>
/// Worker repository implementing IWorkerRepository.
/// Phase 2: Now uses WorkerSpecialization enum and SpecializationDeterminationService.
/// </summary>
public class WorkerRepository : BaseRepository<Worker>, IWorkerRepository
{
    private readonly SpecializationDeterminationService _specializationService;

    public WorkerRepository(
        ApplicationDbContext context,
        SpecializationDeterminationService specializationService) : base(context)
    {
        _specializationService = specializationService;
    }

    #region Specific Worker Methods

    public async Task<Worker?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Workers
            .FirstOrDefaultAsync(w => w.ContactInfo.EmailAddress == email, cancellationToken);
    }

    public async Task<IEnumerable<Worker>> GetBySpecializationAsync(string specialization,
        CancellationToken cancellationToken = default)
    {
        // Parse string specialization to enum
        var specializationEnum = _specializationService.ParseSpecialization(specialization);

        return await Context.Workers
            .Where(w => w.Specialization == specializationEnum)
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

    #region Specification Pattern Implementation

    public async Task<IEnumerable<Worker>> GetBySpecificationAsync(ISpecification<Worker> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    #endregion
}