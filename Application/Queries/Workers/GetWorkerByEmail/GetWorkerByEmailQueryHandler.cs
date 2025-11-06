using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Workers.GetWorkerByEmail;

/// <summary>
/// Query handler to get worker by email using direct projection.
/// Phase 2: Convert enum to string for DTO.
/// </summary>
public class GetWorkerByEmailQueryHandler : IQueryHandler<GetWorkerByEmailQuery, WorkerDto?>
{
    private readonly IApplicationDbContext _context;

    public GetWorkerByEmailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkerDto?> Handle(GetWorkerByEmailQuery request, CancellationToken cancellationToken)
    {
        // Direct EF projection with filtering
        var workerDto = await Task.FromResult(_context.Workers
            .Where(w => w.ContactInfo.EmailAddress == request.Email)
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
            .FirstOrDefault());

        return workerDto;
    }
}