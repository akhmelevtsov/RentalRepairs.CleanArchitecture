using MediatR;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestStatusSummary;

/// <summary>
/// Handler for tenant request status summary analytics.
/// Provides efficient database aggregation for status distribution reporting.
/// </summary>
public class
    GetTenantRequestStatusSummaryQueryHandler : IRequestHandler<GetTenantRequestStatusSummaryQuery,
    Dictionary<string, int>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantRequestStatusSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, int>> Handle(GetTenantRequestStatusSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // Efficient database aggregation - much faster than loading entities
        return await Task.FromResult(_context.TenantRequests
            .GroupBy(tr => tr.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count()));
    }
}