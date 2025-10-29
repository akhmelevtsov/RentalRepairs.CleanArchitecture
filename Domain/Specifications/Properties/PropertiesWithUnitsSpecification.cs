using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for finding properties that contain specific units.
/// </summary>
public class PropertiesWithUnitsSpecification : BaseSpecification<Property>
{
    public PropertiesWithUnitsSpecification(List<string> unitNumbers) 
        : base(p => p.Units.Any(u => unitNumbers.Contains(u)))
    {
    }
}
