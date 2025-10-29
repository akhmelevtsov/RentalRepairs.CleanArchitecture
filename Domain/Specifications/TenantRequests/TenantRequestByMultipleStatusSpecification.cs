using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding tenant requests by multiple statuses.
/// Enhanced specification for Phase 1 request categorization service.
/// </summary>
public class TenantRequestByMultipleStatusSpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestByMultipleStatusSpecification(params TenantRequestStatus[] statuses)
        : base(tr => statuses.Contains(tr.Status))
    {
        ApplyOrderBy(tr => tr.CreatedAt);
    }

    public TenantRequestByMultipleStatusSpecification(IEnumerable<TenantRequestStatus> statuses)
        : base(tr => statuses.Contains(tr.Status))
    {
        ApplyOrderBy(tr => tr.CreatedAt);
    }
}
