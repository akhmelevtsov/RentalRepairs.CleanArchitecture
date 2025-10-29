using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding overdue tenant requests.
/// </summary>
public class TenantRequestOverdueSpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestOverdueSpecification(int daysThreshold)
        : base(tr => tr.Status == TenantRequestStatus.Submitted && 
                     tr.CreatedAt <= DateTime.UtcNow.AddDays(-daysThreshold))
    {
    }
}
