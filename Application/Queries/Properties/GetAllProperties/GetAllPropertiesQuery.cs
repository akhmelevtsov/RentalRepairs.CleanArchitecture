using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetAllProperties;

/// <summary>
/// Query to retrieve all properties in the system.
/// Used for administrative overviews and bulk operations.
/// </summary>
public class GetAllPropertiesQuery : IQuery<IEnumerable<PropertyDto>>
{
}