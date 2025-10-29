using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding tenant requests by status.
/// </summary>
public class TenantRequestByStatusSpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestByStatusSpecification(TenantRequestStatus status)
        : base(tr => tr.Status == status)
    {
    }
}
