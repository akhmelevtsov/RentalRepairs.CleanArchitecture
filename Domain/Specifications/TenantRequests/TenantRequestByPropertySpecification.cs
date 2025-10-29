using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding tenant requests by property.
/// </summary>
public class TenantRequestByPropertySpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestByPropertySpecification(Guid propertyId)
        : base(tr => tr.PropertyId == propertyId)
    {
    }
}
