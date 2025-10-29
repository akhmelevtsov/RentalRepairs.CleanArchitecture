using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding tenant requests by urgency level.
/// </summary>
public class TenantRequestByUrgencySpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestByUrgencySpecification(string urgencyLevel)
        : base(tr => tr.UrgencyLevel == urgencyLevel)
    {
    }
}
