using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for retrieving properties with their tenants included.
/// </summary>
public class PropertyWithTenantsSpecification : BaseSpecification<Property>
{
    public PropertyWithTenantsSpecification() : base()
    {
        AddInclude(p => p.Tenants);
    }
}
