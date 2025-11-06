using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Specifications.Workers;

/// <summary>
/// Specification for finding active workers with a specific specialization.
/// Phase 2: Now uses WorkerSpecialization enum.
/// </summary>
public class WorkerBySpecializationSpecification : BaseSpecification<Worker>
{
    public WorkerBySpecializationSpecification(WorkerSpecialization specialization)
        : base(w => w.IsActive && w.Specialization == specialization)
    {
        ApplyOrderBy(w => w.ContactInfo.LastName);
    }
}
