using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for loading a property with all tenant data to calculate available units.
/// Enables the Property aggregate to use domain logic for unit availability calculations.
/// </summary>
public class PropertyWithAvailableUnitsSpecification : BaseSpecification<Property>
{
    /// <summary>
    /// Initializes specification to find a property by code with all tenant relationships loaded.
    /// </summary>
    /// <param name="propertyCode">The property code to find</param>
    public PropertyWithAvailableUnitsSpecification(string propertyCode)
        : base(p => p.Code == propertyCode)
    {
        // Include tenants so the Property aggregate can calculate available units
        AddInclude(p => p.Tenants);
    }
}
