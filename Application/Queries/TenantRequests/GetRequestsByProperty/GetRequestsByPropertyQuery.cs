using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Models;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Application.Queries.TenantRequests.GetRequestsByProperty;

/// <summary>
/// Query to retrieve requests for a property identified by code with pagination.
/// Used for API endpoints and property-specific request management.
/// </summary>
public class GetRequestsByPropertyQuery : IQuery<PagedResult<TenantRequestDto>>
{
    public string PropertyCode { get; set; } = default!;
    public TenantRequestStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public GetRequestsByPropertyQuery(string propertyCode)
    {
        PropertyCode = propertyCode;
    }
}