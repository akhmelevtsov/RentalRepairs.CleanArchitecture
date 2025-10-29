using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Workers.GetWorkers;

/// <summary>
/// ? Query handlers using consistent direct projection approach
/// </summary>
public class GetWorkersQueryHandler : IQueryHandler<GetWorkersQuery, List<WorkerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkerDto>> Handle(GetWorkersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Workers.AsQueryable();

        // Apply filters directly
        if (request.IsActive.HasValue)
        {
            query = query.Where(w => w.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrEmpty(request.Specialization))
        {
            query = query.Where(w => w.Specialization == request.Specialization);
        }

        // ? Option 1: Use direct EF projection (most efficient)
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
                Specialization = w.Specialization,
                IsActive = w.IsActive,
                Notes = w.Notes,
                RegistrationDate = w.CreatedAt
                // ? DisplayName is computed property, don't assign to it
            })
            .ToList());

        return workerDtos;
    }
}
