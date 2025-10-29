using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Tenants;

/// <summary>
/// Specification for finding a tenant by property code and unit number.
/// </summary>
public class TenantByUnitSpecification : BaseSpecification<Tenant>
{
    public TenantByUnitSpecification(string propertyCode, string unitNumber)
        : base(t => t.PropertyCode == propertyCode && t.UnitNumber == unitNumber)
    {
    }
}
