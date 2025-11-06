using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;

/// <summary>
/// Query to retrieve a specific tenant request by its unique identifier.
/// Can optionally include business context (authorization, available actions).
/// </summary>
public class GetTenantRequestByIdQuery : IQuery<TenantRequestDto>
{
    public Guid Id { get; set; }

    /// <summary>
    /// When true, enriches the result with business context (CanEdit, AvailableActions, etc.)
    /// Returns TenantRequestDetailsDto instead of TenantRequestDto.
    /// </summary>
    public bool IncludeBusinessContext { get; set; }

    public GetTenantRequestByIdQuery(Guid id)
    {
        Id = id;
    }
}