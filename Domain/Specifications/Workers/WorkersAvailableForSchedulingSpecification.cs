using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Workers;

/// <summary>
/// Specification for finding workers available for scheduling.
/// </summary>
public class WorkersAvailableForSchedulingSpecification : BaseSpecification<Worker>
{
    public WorkersAvailableForSchedulingSpecification()
        : base(w => w.IsActive)
    {
        ApplyOrderBy(w => w.ContactInfo.LastName);
    }
}
