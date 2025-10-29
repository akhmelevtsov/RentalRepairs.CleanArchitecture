using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding tenant requests within a date range.
/// </summary>
public class TenantRequestsByDateRangeSpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestsByDateRangeSpecification(DateTime startDate, DateTime endDate)
        : base(tr => tr.CreatedAt >= startDate && tr.CreatedAt <= endDate)
    {
    }
}
