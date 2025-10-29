using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for finding properties in a specific city.
/// </summary>
public class PropertiesByCitySpecification : BaseSpecification<Property>
{
    public PropertiesByCitySpecification(string city) 
        : base(p => p.Address.City == city)
    {
        ApplyOrderBy(p => p.Name);
    }
}
