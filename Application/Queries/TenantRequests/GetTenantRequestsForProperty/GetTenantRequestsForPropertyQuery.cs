using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestsForProperty;

/// <summary>
/// Query to retrieve all tenant requests for a specific property.
/// Used for property management and property-specific request views.
/// </summary>
public class GetTenantRequestsForPropertyQuery : IQuery<List<TenantRequestDto>>
{
    public Guid PropertyId { get; set; }
    public string? Status { get; set; }

    public GetTenantRequestsForPropertyQuery(Guid propertyId)
    {
        PropertyId = propertyId;
    }
}