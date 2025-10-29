using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Tenants.GetTenantsByProperty;

/// <summary>
/// Query to retrieve all tenants for a specific property with optional filtering.
/// Used for property tenant management and tenant lists by property.
/// </summary>
public class GetTenantsByPropertyQuery : IQuery<List<TenantDto>>
{
    public Guid PropertyId { get; set; }
    public bool WithActiveRequestsOnly { get; set; }

    public GetTenantsByPropertyQuery(Guid propertyId)
    {
        PropertyId = propertyId;
    }
}