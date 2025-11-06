using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Constants;

namespace RentalRepairs.Infrastructure.Services;

/// <summary>
/// Current user service that extracts user information from HTTP context claims.
/// Properly implements claims-based authentication for web applications.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current user ID from claims (typically email)
    /// </summary>
    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst(ClaimTypes.Email)?.Value;
        }
    }

    /// <summary>
    /// Gets the current user's display name from claims
    /// </summary>
    public string? UserName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.Name)?.Value
                   ?? user.Identity.Name;
        }
    }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true;
        }
    }

    /// <summary>
    /// Gets the current user's role from claims
    /// </summary>
    public string? UserRole
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            // Try to get role from ClaimTypes.Role
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(roleClaim))
                return roleClaim;

            // Fallback: Check for multiple role claims and return the first one
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles.FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets all user claims as a dictionary
    /// </summary>
    public Dictionary<string, string> GetUserClaims()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return new Dictionary<string, string>();

        return user.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(c => c.Value))
            );
    }

    /// <summary>
    /// Gets audit-friendly user identifier with context
    /// </summary>
    public string GetAuditUserIdentifier()
    {
        var userId = UserId ?? "anonymous";
        var userName = UserName ?? "Unknown";
        var role = UserRole ?? "NoRole";

        return $"{userName} ({userId}) [{role}]";
    }
}