using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding emergency tenant requests.
/// </summary>
public class TenantRequestEmergencySpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestEmergencySpecification()
        : base(tr => tr.IsEmergency)
    {
    }
}
