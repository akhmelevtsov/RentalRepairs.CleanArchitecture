using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.Infrastructure.Authentication.Models;

/// <summary>
/// Configuration for demo authentication system
/// Provides secure demo mode with configurable users and proper password hashing
/// </summary>
public class DemoAuthenticationSettings
{
    public const string SectionName = "DemoAuthentication";

    /// <summary>
    /// Enable demo mode for local development and portfolio demos
    /// </summary>
    public bool EnableDemoMode { get; set; } = false;

    /// <summary>
    /// Default password for demo users (will be hashed)
    /// </summary>
    [Required]
    public string DefaultPassword { get; set; } = "Demo123!";

    /// <summary>
    /// Predefined demo users with roles and claims
    /// </summary>
    public Dictionary<string, DemoUserCredential> DemoUsers { get; set; } = new();

    /// <summary>
    /// Security settings for demo mode
    /// </summary>
    public DemoSecuritySettings Security { get; set; } = new();
}

/// <summary>
/// Demo user credential with secure password storage
/// </summary>
public class DemoUserCredential
{
    [Required] public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password (never store plain text)
    /// </summary>
    [Required]
    public string HashedPassword { get; set; } = string.Empty;

    [Required] public string DisplayName { get; set; } = string.Empty;

    [Required] public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Additional claims for user context (property_id, unit_number, etc.)
    /// </summary>
    public Dictionary<string, string> Claims { get; set; } = new();

    /// <summary>
    /// Whether this user is active and can login
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional description for demo purposes
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Security settings for demo mode
/// </summary>
public class DemoSecuritySettings
{
    /// <summary>
    /// JWT token expiration in hours
    /// </summary>
    public int TokenExpirationHours { get; set; } = 8;

    /// <summary>
    /// Maximum login attempts before lockout
    /// </summary>
    public int MaxLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Minimum password length for user registration
    /// </summary>
    public int MinPasswordLength { get; set; } = 6;

    /// <summary>
    /// Allow user registration in demo mode
    /// </summary>
    public bool AllowUserRegistration { get; set; } = true;

    /// <summary>
    /// Show demo credentials on login page
    /// </summary>
    public bool ShowDemoCredentials { get; set; } = true;
}

/// <summary>
/// Login attempt tracking for basic security
/// </summary>
public class LoginAttempt
{
    public string Email { get; set; } = string.Empty;
    public DateTime AttemptTime { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
}