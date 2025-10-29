namespace RentalRepairs.Application.Common.Constants;

/// <summary>
/// User roles for the rental repairs system
/// Application layer version to avoid WebUI dependency on Infrastructure
/// </summary>
public static class UserRoles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string PropertySuperintendent = "PropertySuperintendent";
    public const string Tenant = "Tenant";
    public const string Worker = "Worker";

    /// <summary>
    /// Get all available roles
    /// </summary>
    public static List<string> GetAllRoles() => new()
    {
        SystemAdmin,
        PropertySuperintendent,
        Tenant,
        Worker
    };

    /// <summary>
    /// Check if role is valid
    /// </summary>
    public static bool IsValidRole(string role) => GetAllRoles().Contains(role);

    /// <summary>
    /// Get role display name
    /// </summary>
    public static string GetDisplayName(string role) => role switch
    {
        SystemAdmin => "System Administrator",
        PropertySuperintendent => "Property Superintendent",
        Tenant => "Tenant",
        Worker => "Maintenance Worker",
        _ => role
    };
}