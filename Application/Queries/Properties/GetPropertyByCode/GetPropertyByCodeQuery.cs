using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetPropertyByCode;

/// <summary>
/// Query to retrieve a property by its unique code.
/// Used for property lookup in tenant request submissions and API operations.
/// </summary>
public class GetPropertyByCodeQuery : IQuery<PropertyDto>
{
    public string Code { get; set; }

    public GetPropertyByCodeQuery(string code)
    {
        Code = code;
    }
}