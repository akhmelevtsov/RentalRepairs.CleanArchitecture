using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Tenants.GetTenantById;

/// <summary>
/// Query to retrieve a specific tenant by their unique identifier.
/// Used for tenant profile pages and individual tenant operations.
/// </summary>
public class GetTenantByIdQuery : IQuery<TenantDto>
{
    public Guid TenantId { get; set; }

    public GetTenantByIdQuery(Guid tenantId)
    {
        TenantId = tenantId;
    }
}