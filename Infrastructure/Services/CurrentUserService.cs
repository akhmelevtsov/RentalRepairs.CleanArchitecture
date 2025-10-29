using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Infrastructure.Services;

/// <summary>
/// FIXED: Infrastructure service focused on system context only.
/// Removed hardcoded business rules that belonged in domain layer.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    // ? FIXED: Infrastructure-only concerns
    public string? UserId => Environment.UserName ?? "system";
    public string? UserName => Environment.UserName ?? "System";
    public bool IsAuthenticated => !string.IsNullOrEmpty(Environment.UserName);
    
    // ? FIXED: Removed hardcoded "System" role - this should come from authentication context
    public string? UserRole => null; // Let authentication system determine this

    /// <summary>
    /// ? FIXED: Returns only infrastructure-level claims
    /// </summary>
    public Dictionary<string, string> GetUserClaims()
    {
        return new Dictionary<string, string>
        {
            { "source", "Infrastructure" },
            { "environment", Environment.MachineName },
            { "process", Environment.ProcessId.ToString() }
        };
    }

    /// <summary>
    /// ? FIXED: Infrastructure audit identifier without business logic
    /// </summary>
    public string GetAuditUserIdentifier()
    {
        var user = Environment.UserName ?? "system";
        var machine = Environment.MachineName;
        return $"{user}@{machine}";
    }
}