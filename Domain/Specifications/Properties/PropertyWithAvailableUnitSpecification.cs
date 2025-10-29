using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for checking if a property has a specific available unit.
/// Loads property with tenant data to enable domain-level unit availability checks.
/// </summary>
public class PropertyWithAvailableUnitSpecification : BaseSpecification<Property>
{
    /// <summary>
    /// Initializes specification to find a property by code and ensure it has the specified unit.
    /// Includes tenant data to enable availability calculations using domain logic.
    /// </summary>
    /// <param name="propertyCode">The property code to find</param>
    /// <param name="unitNumber">The unit number to check</param>
    public PropertyWithAvailableUnitSpecification(string propertyCode, string unitNumber)
        : base(p => p.Code == propertyCode && p.Units.Contains(unitNumber))
    {
        // Include tenants so the Property aggregate can determine unit availability
        AddInclude(p => p.Tenants);
    }
}
