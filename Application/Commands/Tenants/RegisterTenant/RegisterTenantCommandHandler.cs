using MediatR;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Application.Commands.Tenants.RegisterTenant;

/// <summary>
/// FIXED: Command handler that follows proper DDD architecture
/// Uses application orchestration instead of obsolete domain service
/// </summary>
public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPropertyRepository _propertyRepository; // ? ADDED: Use repository for data access
    private readonly ITenantRepository _tenantRepository; // ? ADDED: Use repository for data access
    private readonly PropertyPolicyService _policyService; // ? FIXED: Use pure domain service

    public RegisterTenantCommandHandler(
        IApplicationDbContext context,
        IPropertyRepository propertyRepository,
        ITenantRepository tenantRepository,
        PropertyPolicyService policyService) // ? FIXED: Inject pure domain service
    {
        _context = context;
        _propertyRepository = propertyRepository;
        _tenantRepository = tenantRepository;
        _policyService = policyService;
    }

    public async Task<Guid> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        // Data loading (Application layer responsibility)
        var property = await _propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property == null)
        {
            throw new InvalidOperationException($"Property with ID {request.PropertyId} not found");
        }

        // Cross-aggregate validation (Application layer responsibility)
        var isUnitOccupied = await _tenantRepository.ExistsInUnitAsync(property.Code, request.UnitNumber, cancellationToken);
        if (isUnitOccupied)
        {
            throw new InvalidOperationException($"Unit '{request.UnitNumber}' is already occupied in property '{property.Code}'");
        }

        // Create contact info value object
        var contactInfo = new PersonContactInfo(
            request.ContactInfo.FirstName,
            request.ContactInfo.LastName,
            request.ContactInfo.EmailAddress,
            request.ContactInfo.MobilePhone);

        // Business validation via pure domain service (Domain layer responsibility)
        _policyService.ValidateTenantRegistration(property, contactInfo, request.UnitNumber);

        // Business logic execution (Domain layer responsibility)
        var tenant = property.RegisterTenant(contactInfo, request.UnitNumber);

        // Persistence (Application layer responsibility)
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}