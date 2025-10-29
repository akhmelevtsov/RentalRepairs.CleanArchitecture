using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for loading a property by code with tenant relationships.
/// General-purpose specification for property operations requiring tenant data.
/// </summary>
public class PropertyByCodeWithTenantsSpecification : BaseSpecification<Property>
{
    /// <summary>
    /// Initializes specification to find a property by code with tenant data included.
    /// </summary>
    /// <param name="propertyCode">The property code to find</param>
    public PropertyByCodeWithTenantsSpecification(string propertyCode)
        : base(p => p.Code == propertyCode)
    {
        AddInclude(p => p.Tenants);
        ApplyOrderBy(p => p.Name);
    }
}
