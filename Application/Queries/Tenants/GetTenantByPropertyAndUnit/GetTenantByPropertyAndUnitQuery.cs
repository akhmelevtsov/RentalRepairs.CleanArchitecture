using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Tenants.GetTenantByPropertyAndUnit;

/// <summary>
/// Query to retrieve a tenant by property and unit number.
/// Used for tenant lookup during request submissions and property management.
/// </summary>
public class GetTenantByPropertyAndUnitQuery : IQuery<TenantDto>
{
    public Guid PropertyId { get; set; }
    public string UnitNumber { get; set; } = default!;

    public GetTenantByPropertyAndUnitQuery(Guid propertyId, string unitNumber)
    {
        PropertyId = propertyId;
        UnitNumber = unitNumber;
    }
}