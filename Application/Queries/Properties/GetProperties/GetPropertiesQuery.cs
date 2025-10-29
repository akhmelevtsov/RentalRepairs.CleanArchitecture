using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Models;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.Properties.GetProperties;

/// <summary>
/// Query to retrieve properties with filtering and pagination.
/// Used for property list pages with search and filtering capabilities.
/// </summary>
public class GetPropertiesQuery : IQuery<PagedResult<PropertyDto>>
{
    public string? City { get; set; }
    public string? SuperintendentEmail { get; set; }
    public bool? WithTenants { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}