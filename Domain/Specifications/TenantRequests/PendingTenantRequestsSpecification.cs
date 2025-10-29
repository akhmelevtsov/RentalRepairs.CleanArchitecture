using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding pending tenant requests.
/// </summary>
public class PendingTenantRequestsSpecification : BaseSpecification<TenantRequest>
{
    public PendingTenantRequestsSpecification()
        : base(tr => tr.Status == TenantRequestStatus.Submitted || tr.Status == TenantRequestStatus.Scheduled)
    {
    }
}
