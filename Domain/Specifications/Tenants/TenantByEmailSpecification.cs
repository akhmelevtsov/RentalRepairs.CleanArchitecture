using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.Tenants;

/// <summary>
/// Specification for finding a tenant by email address.
/// </summary>
public class TenantByEmailSpecification : BaseSpecification<Tenant>
{
    public TenantByEmailSpecification(string email)
        : base(t => t.ContactInfo.EmailAddress.ToLower() == email.ToLower())
    {
    }
}
