using MediatR;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Repositories;

namespace RentalRepairs.Application.Commands.Properties.RegisterProperty;

/// <summary>
/// FIXED: Command handler that follows proper DDD architecture
/// Uses application orchestration instead of obsolete domain service
/// </summary>
public class RegisterPropertyCommandHandler : IRequestHandler<RegisterPropertyCommand, Guid>
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly PropertyPolicyService _policyService; // ? FIXED: Use pure domain service

    public RegisterPropertyCommandHandler(
        IPropertyRepository propertyRepository,
        PropertyPolicyService policyService) // ? FIXED: Inject pure domain service
    {
        _propertyRepository = propertyRepository;
        _policyService = policyService;
    }

    public async Task<Guid> Handle(RegisterPropertyCommand request, CancellationToken cancellationToken)
    {
        // Cross-aggregate validation (Application layer responsibility)
        var exists = await _propertyRepository.ExistsByCodeAsync(request.Code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Property with code '{request.Code}' already exists");
        }

        // Create value objects
        var address = new PropertyAddress(
            request.Address.StreetNumber,
            request.Address.StreetName,
            request.Address.City,
            request.Address.PostalCode);

        var superintendent = new PersonContactInfo(
            request.Superintendent.FirstName,
            request.Superintendent.LastName,
            request.Superintendent.EmailAddress,
            request.Superintendent.MobilePhone);

        // Business validation via pure domain service (Domain layer responsibility)
        _policyService.ValidatePropertyCreation(
            request.Name,
            request.Code,
            address,
            request.PhoneNumber,
            superintendent,
            request.Units,
            request.NoReplyEmailAddress);

        // Create the property using domain aggregate (Domain layer responsibility)
        var property = new Domain.Entities.Property(
            request.Name,
            request.Code,
            address,
            request.PhoneNumber,
            superintendent,
            request.Units,
            request.NoReplyEmailAddress);

        // Persistence (Application layer responsibility)
        await _propertyRepository.AddAsync(property, cancellationToken);
        await _propertyRepository.SaveChangesAsync(cancellationToken);

        return property.Id;
    }
}