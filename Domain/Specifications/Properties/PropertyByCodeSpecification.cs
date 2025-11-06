using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for finding a property by its unique code.
/// </summary>
public class PropertyByCodeSpecification : BaseSpecification<Property>
{
    public PropertyByCodeSpecification(string code)
        : base(p => p.Code == code)
    {
    }
}
