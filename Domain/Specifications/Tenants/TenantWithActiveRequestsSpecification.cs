using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Tenants;

/// <summary>
/// Specification for finding tenants with active requests.
/// </summary>
public class TenantWithActiveRequestsSpecification : BaseSpecification<Tenant>
{
    public TenantWithActiveRequestsSpecification()
        : base(t => t.Requests.Any(r =>
            r.Status == Enums.TenantRequestStatus.Submitted ||
            r.Status == Enums.TenantRequestStatus.Scheduled))
    {
    }
}
