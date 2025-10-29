using MediatR;

namespace RentalRepairs.Application.Queries.Properties.GetAvailableUnits;

/// <summary>
/// Query to get all available units for a property.
/// Uses domain logic through specifications instead of application-level calculations.
/// </summary>
/// <param name="PropertyCode">The property code to get available units for</param>
public record GetAvailableUnitsQuery(string PropertyCode) : IRequest<List<string>>;