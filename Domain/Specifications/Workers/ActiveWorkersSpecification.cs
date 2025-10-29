using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Workers;

/// <summary>
/// Specification for finding active workers, ordered by last name.
/// </summary>
public class ActiveWorkersSpecification : BaseSpecification<Worker>
{
    public ActiveWorkersSpecification() : base(w => w.IsActive)
    {
        ApplyOrderBy(w => w.ContactInfo.LastName);
    }
}
