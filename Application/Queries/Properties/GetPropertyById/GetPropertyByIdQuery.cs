using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetPropertyById;

/// <summary>
/// Query to retrieve a specific property by its unique identifier.
/// Used for property detail pages and individual property operations.
/// </summary>
public class GetPropertyByIdQuery : IQuery<PropertyDto>
{
    public Guid Id { get; set; }

    public GetPropertyByIdQuery(Guid id)
    {
        Id = id;
    }
}