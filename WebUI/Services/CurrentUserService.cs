using RentalRepairs.Application.Common.Interfaces;
using System.Security.Claims;

namespace RentalRepairs.WebUI.Services;

/// <summary>
/// ? Enhanced CurrentUserService for Razor Pages with comprehensive user information
/// Provides detailed user context for audit trails and business operations
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// ? Primary user identifier - uses email as unique identifier
    /// Falls back to NameIdentifier claim if email not available
    /// </summary>
    public string? UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated != true)
                return null;

            // Primary: Use email as user identifier
            var email = context.User.FindFirstValue(ClaimTypes.Email) ??
                        context.User.FindFirstValue("email");

            if (!string.IsNullOrEmpty(email))
                return email;

            // Fallback: Use NameIdentifier
            return context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }

    /// <summary>
    /// ? Display name for user - uses Name claim or extracts from email
    /// </summary>
    public string? UserName
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated != true)
                return null;

            // Primary: Use display name
            var displayName = context.User.FindFirstValue(ClaimTypes.Name) ??
                              context.User.FindFirstValue("name");

            if (!string.IsNullOrEmpty(displayName))
                return displayName;

            // Fallback: Extract name from email
            var email = context.User.FindFirstValue(ClaimTypes.Email) ??
                        context.User.FindFirstValue("email");

            if (!string.IsNullOrEmpty(email))
            {
                var localPart = email.Split('@')[0];
                return localPart.Replace(".", " ").Replace("_", " ");
            }

            return "Unknown User";
        }
    }

    /// <summary>
    /// ? Authentication status
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// ? Get user role for audit context
    /// </summary>
    public string? UserRole
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated != true)
                return null;

            return context.User.FindFirstValue(ClaimTypes.Role) ??
                   context.User.FindFirstValue("role");
        }
    }

    /// <summary>
    /// ? Get all user claims for detailed audit context
    /// </summary>
    public Dictionary<string, string> GetUserClaims()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
            return new Dictionary<string, string>();

        return context.User.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.First().Value);
    }

    /// <summary>
    /// ? Get audit-friendly user identifier with context
    /// </summary>
    public string GetAuditUserIdentifier()
    {
        if (!IsAuthenticated)
            return "anonymous";

        var userId = UserId;
        var role = UserRole;

        if (!string.IsNullOrEmpty(role))
            return $"{userId} ({role})";

        return userId ?? "unknown";
    }
}