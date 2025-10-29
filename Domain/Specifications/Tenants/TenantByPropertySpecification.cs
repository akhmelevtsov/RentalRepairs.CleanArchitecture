using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Tenants;

/// <summary>
/// Specification for finding tenants by property ID.
/// </summary>
public class TenantByPropertySpecification : BaseSpecification<Tenant>
{
    public TenantByPropertySpecification(Guid propertyId)
        : base(t => t.PropertyId == propertyId)
    {
    }
}
