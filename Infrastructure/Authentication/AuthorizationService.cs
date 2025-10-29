using Microsoft.Extensions.Logging;
using MediatR;
using RentalRepairs.Application.Queries.Properties.GetPropertyById;
using RentalRepairs.Application.Queries.Tenants.GetTenantById;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Infrastructure.Authentication;

/// <summary>
/// Infrastructure authorization service that orchestrates data loading and delegates business logic to Domain services.
/// FIXED: Uses proper user identity instead of email pattern matching.
/// Follows Clean Architecture: Infrastructure loads data, Domain contains business rules.
/// </summary>
public class AuthorizationService
{
    private readonly IMediator _mediator;
    private readonly AuthorizationDomainService _domainService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        IMediator mediator,
        AuthorizationDomainService domainService,
        IHttpContextAccessor httpContextAccessor,
        ICurrentUserService currentUserService,
        ILogger<AuthorizationService> logger)
    {
        _mediator = mediator;
        _domainService = domainService;
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Infrastructure orchestration: Load data and delegate business logic to domain service.
    /// </summary>
    public async Task<bool> CanAccessPropertyAsync(string userId, int propertyId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use proper user identity instead of email pattern matching
            var user = await CreateUserFromCurrentIdentity(userId);
            if (user == null)
            {
                _logger.LogWarning("Could not create user identity for {UserId}", userId);
                return false;
            }

            var propertyDto = await _mediator.Send(new GetPropertyByIdQuery(Guid.Parse(propertyId.ToString())), cancellationToken);
            
            if (propertyDto == null)
            {
                _logger.LogWarning("Property {PropertyId} not found for access check", propertyId);
                return false;
            }

            // Convert DTO to Domain entity (simplified for demo)
            var property = CreatePropertyFromDto(propertyDto);

            // Domain: Apply business rules
            return _domainService.CanUserAccessProperty(user, property);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check property access for user {UserId} and property {PropertyId}", userId, propertyId);
            return false;
        }
    }

    /// <summary>
    /// Infrastructure orchestration: Load data and delegate business logic to domain service.
    /// </summary>
    public async Task<bool> CanAccessTenantRequestAsync(string userId, int tenantRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use proper user identity instead of email pattern matching
            var user = await CreateUserFromCurrentIdentity(userId);
            if (user == null)
            {
                _logger.LogWarning("Could not create user identity for {UserId}", userId);
                return false;
            }

            // Business Rule: System administrators can access all requests
            if (user.IsSystemAdmin())
                return true;

            // Load basic request data for authorization
            var requestDto = await _mediator.Send(new GetTenantRequestByIdQuery(Guid.Parse(tenantRequestId.ToString())), cancellationToken);
            if (requestDto == null)
                return false;

            // Business Rule: Check authorization based on user role and context
            if (user.IsWorker() && !string.IsNullOrEmpty(requestDto.AssignedWorkerEmail))
            {
                return user.Email.Equals(requestDto.AssignedWorkerEmail, StringComparison.OrdinalIgnoreCase);
            }

            // For other roles, load additional data as needed
            var tenantDto = await _mediator.Send(new GetTenantByIdQuery(requestDto.TenantId), cancellationToken);
            if (tenantDto != null && user.IsTenant())
            {
                return user.Email.Equals(tenantDto.ContactInfo.EmailAddress, StringComparison.OrdinalIgnoreCase);
            }

            // For superintendents, check property association using claims
            if (user.IsPropertySuperintendent())
            {
                var userPropertyCode = GetUserPropertyCodeFromClaims();
                return !string.IsNullOrEmpty(userPropertyCode) && userPropertyCode == tenantDto?.PropertyCode;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check tenant request access for user {UserId} and request {RequestId}", userId, tenantRequestId);
            return false;
        }
    }

    /// <summary>
    /// Infrastructure orchestration: Create user and delegate business logic to domain service.
    /// </summary>
    public async Task<bool> CanManageWorkersAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use proper user identity instead of email pattern matching
            var user = await CreateUserFromCurrentIdentity(userId);
            if (user == null)
            {
                return false;
            }
            
            // Domain: Apply business rules
            return _domainService.CanUserManageWorkers(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check worker management access for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Infrastructure orchestration: Load data and delegate business logic to domain service.
    /// </summary>
    public async Task<bool> IsPropertySuperintendentAsync(string userId, int propertyId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use proper user identity instead of email pattern matching
            var user = await CreateUserFromCurrentIdentity(userId);
            if (user == null || !user.IsPropertySuperintendent())
            {
                return false;
            }

            var propertyDto = await _mediator.Send(new GetPropertyByIdQuery(Guid.Parse(propertyId.ToString())), cancellationToken);
            
            if (propertyDto == null)
            {
                _logger.LogWarning("Property {PropertyId} not found for superintendent check", propertyId);
                return false;
            }

            // Check using claims-based property association
            var userPropertyCode = GetUserPropertyCodeFromClaims();
            return !string.IsNullOrEmpty(userPropertyCode) && 
                   userPropertyCode.Equals(propertyDto.Code, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check superintendent status for user {UserId} and property {PropertyId}", userId, propertyId);
            return false;
        }
    }

    /// <summary>
    /// Infrastructure orchestration: Load data and delegate business logic to domain service.
    /// </summary>
    public async Task<bool> IsPropertySuperintendentAsync(string userId, string propertyId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use proper user identity instead of email pattern matching
            var user = await CreateUserFromCurrentIdentity(userId);
            if (user == null || !user.IsPropertySuperintendent())
            {
                return false;
            }

            var propertyDto = await _mediator.Send(new GetPropertyByIdQuery(Guid.Parse(propertyId)), cancellationToken);
            
            if (propertyDto == null)
            {
                _logger.LogWarning("Property {PropertyId} not found for superintendent check", propertyId);
                return false;
            }

            // Check using claims-based property association
            var userPropertyCode = GetUserPropertyCodeFromClaims();
            return !string.IsNullOrEmpty(userPropertyCode) && 
                   userPropertyCode.Equals(propertyDto.Code, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check superintendent status for user {UserId} and property {PropertyId}", userId, propertyId);
            return false;
        }
    }

    /// <summary>
    /// Infrastructure orchestration: Load data and delegate business logic to domain service.
    /// </summary>
    public async Task<bool> IsTenantForRequestAsync(string userId, int tenantRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use proper user identity instead of email pattern matching
            var user = await CreateUserFromCurrentIdentity(userId);
            if (user == null || !user.IsTenant())
            {
                return false;
            }

            var requestDto = await _mediator.Send(new GetTenantRequestByIdQuery(Guid.Parse(tenantRequestId.ToString())), cancellationToken);
            
            if (requestDto == null)
            {
                _logger.LogWarning("Tenant request {RequestId} not found for tenant check", tenantRequestId);
                return false;
            }

            var tenantDto = await _mediator.Send(new GetTenantByIdQuery(requestDto.TenantId), cancellationToken);
            
            if (tenantDto == null)
            {
                _logger.LogWarning("Tenant not found for request {RequestId}", tenantRequestId);
                return false;
            }

            // Check using email from user identity
            return user.Email.Equals(tenantDto.ContactInfo.EmailAddress, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check tenant ownership for user {UserId} and request {RequestId}", userId, tenantRequestId);
            return false;
        }
    }

    /// <summary>
    /// Infrastructure orchestration: Load data and delegate business logic to domain service.
    /// </summary>
    public async Task<bool> IsWorkerAssignedToRequestAsync(string userId, int tenantRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use proper user identity instead of email pattern matching
            var user = await CreateUserFromCurrentIdentity(userId);
            if (user == null || !user.IsWorker())
            {
                return false;
            }

            var requestDto = await _mediator.Send(new GetTenantRequestByIdQuery(Guid.Parse(tenantRequestId.ToString())), cancellationToken);
            
            if (requestDto == null)
            {
                _logger.LogWarning("Tenant request {RequestId} not found for worker assignment check", tenantRequestId);
                return false;
            }

            // Check using email from user identity
            return !string.IsNullOrEmpty(requestDto.AssignedWorkerEmail) &&
                   user.Email.Equals(requestDto.AssignedWorkerEmail, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check worker assignment for user {UserId} and request {RequestId}", userId, tenantRequestId);
            return false;
        }
    }

    #region Private Infrastructure Helper Methods

    /// <summary>
    /// FIXED: Create User domain entity from current authenticated identity.
    /// Uses proper claims instead of email pattern matching.
    /// </summary>
    private async Task<User?> CreateUserFromCurrentIdentity(string userId)
    {
        await Task.CompletedTask;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("No authenticated user found for creating identity");
            return null;
        }

        var userRole = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        var displayName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? userId;

        if (string.IsNullOrEmpty(userRole))
        {
            _logger.LogWarning("No role found in claims for user {UserId}", userId);
            return null;
        }

        return userRole switch
        {
            "SystemAdmin" => User.CreateSystemAdmin(userId, displayName),
            "PropertySuperintendent" => CreatePropertySuperintendentFromClaims(userId, displayName),
            "Worker" => CreateWorkerFromClaims(userId, displayName),
            "Tenant" => CreateTenantFromClaims(userId, displayName),
            _ => null
        };
    }

    /// <summary>
    /// FIXED: Create PropertySuperintendent from claims instead of email patterns.
    /// </summary>
    private User CreatePropertySuperintendentFromClaims(string userId, string displayName)
    {
        var propertyCode = GetUserPropertyCodeFromClaims() ?? "UNKNOWN";
        return User.CreatePropertySuperintendent(userId, displayName, propertyCode);
    }

    /// <summary>
    /// FIXED: Create Worker from claims instead of email patterns.
    /// </summary>
    private User CreateWorkerFromClaims(string userId, string displayName)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var specialization = httpContext?.User.FindFirst("worker_specialization")?.Value ?? "General Maintenance";
        return User.CreateWorker(userId, displayName, specialization);
    }

    /// <summary>
    /// FIXED: Create Tenant from claims instead of assumptions.
    /// </summary>
    private User CreateTenantFromClaims(string userId, string displayName)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var propertyCode = httpContext?.User.FindFirst("property_code")?.Value ?? "UNKNOWN";
        var unitNumber = httpContext?.User.FindFirst("unit_number")?.Value ?? "UNKNOWN";
        return User.CreateTenant(userId, displayName, propertyCode, unitNumber);
    }

    /// <summary>
    /// Helper: Get property code from user claims.
    /// </summary>
    private string? GetUserPropertyCodeFromClaims()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User.FindFirst("property_code")?.Value;
    }

    /// <summary>
    /// Infrastructure helper: Create simplified Property domain entity from DTO.
    /// In production, use proper domain entity factories or repositories.
    /// </summary>
    private RentalRepairs.Domain.Entities.Property CreatePropertyFromDto(Application.DTOs.PropertyDto propertyDto)
    {
        // Simplified domain entity creation for demonstration
        // In production, use proper domain entity factories
        return new RentalRepairs.Domain.Entities.Property(
            propertyDto.Name,
            propertyDto.Code,
            new RentalRepairs.Domain.ValueObjects.PropertyAddress(
                propertyDto.Address.StreetName, // Using available properties
                propertyDto.Address.City,
                "Unknown", // State not available in DTO structure
                propertyDto.Address.PostalCode
            ),
            propertyDto.PhoneNumber,
            new RentalRepairs.Domain.ValueObjects.PersonContactInfo(
                propertyDto.Superintendent.FirstName + " " + propertyDto.Superintendent.LastName,
                propertyDto.Superintendent.EmailAddress,
                propertyDto.Superintendent.MobilePhone ?? "Not provided"
            ),
            propertyDto.Units, // Use existing units list
            propertyDto.NoReplyEmailAddress
        );
    }

    #endregion
}