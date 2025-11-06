using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Properties;

/// <summary>
/// Specification for finding properties by superintendent email.
/// </summary>
public class PropertiesBySuperintendentEmailSpecification : BaseSpecification<Property>
{
    public PropertiesBySuperintendentEmailSpecification(string email)
        : base(p => p.Superintendent.EmailAddress == email)
    {
        AddInclude(p => p.Tenants);
    }
}
