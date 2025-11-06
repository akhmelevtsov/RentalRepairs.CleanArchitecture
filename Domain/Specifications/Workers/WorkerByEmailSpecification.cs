using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Workers;

/// <summary>
/// Specification for finding a worker by email address.
/// </summary>
public class WorkerByEmailSpecification : BaseSpecification<Worker>
{
    public WorkerByEmailSpecification(string email)
        : base(w => w.ContactInfo.EmailAddress == email)
    {
    }
}
