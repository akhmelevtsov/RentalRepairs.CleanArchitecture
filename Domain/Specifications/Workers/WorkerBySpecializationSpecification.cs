using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Workers;

/// <summary>
/// Specification for finding active workers with a specific specialization.
/// </summary>
public class WorkerBySpecializationSpecification : BaseSpecification<Worker>
{
    public WorkerBySpecializationSpecification(string specialization) 
        : base(w => w.IsActive && w.Specialization == specialization)
    {
        ApplyOrderBy(w => w.ContactInfo.LastName);
    }
}
