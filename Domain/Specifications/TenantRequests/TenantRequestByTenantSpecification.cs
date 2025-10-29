using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding tenant requests by tenant.
/// </summary>
public class TenantRequestByTenantSpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestByTenantSpecification(Guid tenantId)
        : base(tr => tr.TenantId == tenantId)
    {
    }
}
