using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Workers.GetWorkerById;

/// <summary>
/// Query handler to get worker by ID using direct projection.
/// Phase 2: Convert enum to string for DTO.
/// </summary>
public class GetWorkerByIdQueryHandler : IQueryHandler<GetWorkerByIdQuery, WorkerDto?>
{
    private readonly IApplicationDbContext _context;

    public GetWorkerByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkerDto?> Handle(GetWorkerByIdQuery request, CancellationToken cancellationToken)
    {
        // Direct EF projection - no mapping overhead
        var workerDto = await Task.FromResult(_context.Workers
            .Where(w => w.Id == request.WorkerId)
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