using MediatR;

namespace RentalRepairs.Application.Queries.Properties.CheckUnitAvailability;

/// <summary>
/// Query to check if a specific unit is available in a property.
/// Uses domain logic through specifications instead of application-level business rules.
/// </summary>
/// <param name="PropertyCode">The property code to check</param>
/// <param name="UnitNumber">The unit number to check availability for</param>
public record CheckUnitAvailabilityQuery(string PropertyCode, string UnitNumber) : IRequest<bool>;