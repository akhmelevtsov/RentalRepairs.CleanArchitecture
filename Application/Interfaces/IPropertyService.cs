namespace RentalRepairs.Application.Interfaces;

/// <summary>
/// Property service interface containing ONLY business logic operations.
/// Simple CRUD operations should use CQRS directly via IMediator.
/// 
/// ARCHITECTURAL DECISION:
/// This service now contains only methods that orchestrate multiple domain operations
/// or contain genuine business logic. Pure CRUD operations have been removed to 
/// eliminate unnecessary indirection and follow proper CQRS patterns.
/// 
/// REMOVED METHODS (use direct CQRS instead):
/// - RegisterPropertyAsync ? Use RegisterPropertyCommand via IMediator
/// - GetPropertyByIdAsync ? Use GetPropertyByIdQuery via IMediator
/// - GetPropertyByCodeAsync ? Use GetPropertyByCodeQuery via IMediator
/// - GetPropertiesAsync ? Use GetPropertiesQuery via IMediator
/// - GetPropertyStatisticsAsync ? Use GetPropertyStatisticsQuery via IMediator
/// - RegisterTenantAsync ? Use RegisterTenantCommand via IMediator
/// - GetTenantByIdAsync ? Use GetTenantByIdQuery via IMediator
/// - GetTenantByPropertyAndUnitAsync ? Use GetTenantByPropertyAndUnitQuery via IMediator
/// - GetTenantsByPropertyAsync ? Use GetTenantsByPropertyQuery via IMediator
/// </summary>
public interface IPropertyService
{
}