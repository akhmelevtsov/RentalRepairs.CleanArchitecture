namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// ? Enhanced interface for current user service with comprehensive user information
/// Supports detailed audit trails and user context for Razor Pages applications
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Primary user identifier - typically email or unique user ID
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Display name for the current user
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Current user's role for authorization and audit context
    /// </summary>
    string? UserRole { get; }

    /// <summary>
    /// Get all user claims for detailed audit context
    /// </summary>
    Dictionary<string, string> GetUserClaims();

    /// <summary>
    /// Get audit-friendly user identifier with context
    /// </summary>
    string GetAuditUserIdentifier();
}