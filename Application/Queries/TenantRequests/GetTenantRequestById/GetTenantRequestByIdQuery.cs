using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;

/// <summary>
/// Query to retrieve a specific tenant request by its unique identifier.
/// Used for request detail pages and individual request operations.
/// </summary>
public class GetTenantRequestByIdQuery : IQuery<TenantRequestDto>
{
    public Guid Id { get; set; }

    public GetTenantRequestByIdQuery(Guid id)
    {
        Id = id;
    }
}