using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Application.Queries.Workers.GetWorkers;

/// <summary>
/// Query handler for getting workers with optional filters.
/// Phase 2: Now uses SpecializationDeterminationService to handle enum/string conversion.
/// </summary>
public class GetWorkersQueryHandler : IQueryHandler<GetWorkersQuery, List<WorkerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly SpecializationDeterminationService _specializationService;

    public GetWorkersQueryHandler(
        IApplicationDbContext context,
        SpecializationDeterminationService specializationService)
    {
        _context = context;
        _specializationService = specializationService;
    }

    public async Task<List<WorkerDto>> Handle(GetWorkersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Workers.AsQueryable();

        // Apply filters directly
        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        if (!string.IsNullOrEmpty(request.Specialization))
        {
            // Parse string filter to enum
            var specializationEnum = _specializationService.ParseSpecialization(request.Specialization);
            query = query.Where(w => w.Specialization == specializationEnum);
        }

        // Project to DTO
        var workerDtos = await Task.FromResult(query
            .OrderBy(w => w.ContactInfo.LastName)
            .ThenBy(w => w.ContactInfo.FirstName)
            .Select(w => new WorkerDto
            {
                Id = w.Id,
                ContactInfo = new PersonContactInfoDto
                {
                    FirstName = w.ContactInfo.FirstName,
                    LastName = w.ContactInfo.LastName,
                    EmailAddress = w.ContactInfo.EmailAddress,
                    MobilePhone = w.ContactInfo.MobilePhone,
                    FullName = w.ContactInfo.GetFullName()
                },
                // Convert enum to string for DTO
                Specialization = w.Specialization.ToString(),
                IsActive = w.IsActive,
                Notes = w.Notes,
                RegistrationDate = w.CreatedAt
            })
            .ToList());

        return workerDtos;
    }
}