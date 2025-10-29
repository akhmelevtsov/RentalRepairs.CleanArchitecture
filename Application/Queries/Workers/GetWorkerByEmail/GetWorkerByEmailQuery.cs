using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Workers.GetWorkerByEmail;

/// <summary>
/// Query to retrieve a worker by their email address.
/// Used for worker lookup during assignment and authentication workflows.
/// </summary>
public class GetWorkerByEmailQuery : IQuery<WorkerDto>
{
    public string Email { get; set; } = default!;

    public GetWorkerByEmailQuery(string email)
    {
        Email = email;
    }
}