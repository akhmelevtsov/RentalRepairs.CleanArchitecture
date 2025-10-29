using RentalRepairs.Domain.Services;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Services;

/// <summary>
/// FIXED: Application service for user role management.
/// Uses ICurrentUserService instead of direct HttpContext access to maintain clean architecture.
/// Domain service is only used for business rule validation, not user identification.
/// </summary>
public class UserRoleService
{
    private readonly UserRoleDomainService _domainService;
    private readonly ICurrentUserService _currentUserService;

    public UserRoleService(
        UserRoleDomainService domainService,
        ICurrentUserService currentUserService)
    {
        _domainService = domainService ?? throw new ArgumentNullException(nameof(domainService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <summary>
    /// FIXED: Application orchestration - gets role from authenticated user via ICurrentUserService.
    /// Uses proper Clean Architecture approach without direct infrastructure dependencies.
    /// </summary>
    public string DetermineUserRole(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new ArgumentException("User email cannot be empty", nameof(userEmail));
        }

        // FIXED: Use ICurrentUserService to get current user's role
        if (_currentUserService.IsAuthenticated)
        {
            var userRole = _currentUserService.UserRole;
            if (!string.IsNullOrEmpty(userRole) && _domainService.IsValidRole(userRole))
            {
                return userRole;
            }
        }

        // FALLBACK: For background processes or when no authenticated user exists
        // Default to Tenant role if no role can be determined
        // This avoids the problematic email pattern matching
        return "Tenant";
    }

    /// <summary>
    /// Application orchestration: Gets role-specific permissions via domain service.
    /// </summary>
    public List<string> GetRolePermissions(string role)
    {
        return _domainService.GetPermissionsForRole(role);
    }

    /// <summary>
    /// Application orchestration: Validates role via domain service.
    /// </summary>
    public bool IsValidRole(string role)
    {
        return _domainService.IsValidRole(role);
    }

    /// <summary>
    /// ADDED: Get current authenticated user's role directly.
    /// This is the preferred method for authorization checks.
    /// </summary>
    public string? GetCurrentUserRole()
    {
        return _currentUserService.IsAuthenticated ? _currentUserService.UserRole : null;
    }

    /// <summary>
    /// ADDED: Check if current user has a specific permission.
    /// Uses domain service business rules.
    /// </summary>
    public bool CurrentUserHasPermission(string permission)
    {
        var currentRole = GetCurrentUserRole();
        if (string.IsNullOrEmpty(currentRole))
            return false;

        return _domainService.RoleHasPermission(currentRole, permission);
    }

    /// <summary>
    /// ADDED: Check if current user can decline requests.
    /// Uses domain service business rules.
    /// </summary>
    public bool CanCurrentUserDeclineRequests()
    {
        var currentRole = GetCurrentUserRole();
        if (string.IsNullOrEmpty(currentRole))
            return false;

        return _domainService.CanRoleDeclineRequests(currentRole);
    }
}