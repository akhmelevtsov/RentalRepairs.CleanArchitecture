namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// Consolidated Authentication Service Interface - Single source of truth for authentication
/// All users authenticate with email/password only - system determines role and parameters automatically
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Unified authentication method - determines user role and parameters automatically
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(string email, string password);
    
    /// <summary>
    /// Validate user credentials without full authentication
    /// </summary>
    Task<bool> ValidateCredentialsAsync(string email, string password);
    
    /// <summary>
    /// Validate authentication token
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Sign out current user
    /// </summary>
    Task SignOutAsync();
}

/// <summary>
/// Demo user management service interface for Application layer
/// </summary>
public interface IDemoUserService
{
    /// <summary>
    /// Validate user credentials against demo user database
    /// </summary>
    Task<DemoUserValidationResult> ValidateUserAsync(string email, string password);

    /// <summary>
    /// Get demo user by email
    /// </summary>
    Task<DemoUserCredential?> GetDemoUserAsync(string email);

    /// <summary>
    /// Register a new demo user (if enabled)
    /// </summary>
    Task<bool> RegisterDemoUserAsync(string email, string password, string displayName, List<string> roles, Dictionary<string, string>? claims = null);

    /// <summary>
    /// Get all available demo users for display
    /// </summary>
    Task<List<DemoUserInfo>> GetDemoUsersForDisplayAsync();

    /// <summary>
    /// Initialize default demo users
    /// </summary>
    Task InitializeDemoUsersAsync();

    /// <summary>
    /// Check if demo mode is enabled
    /// </summary>
    bool IsDemoModeEnabled();

    /// <summary>
    /// Get the configured default password for demo users
    /// </summary>
    string GetDefaultPassword();
}

/// <summary>
/// Consolidated Authentication Result with role detection and dashboard routing
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Claims { get; set; } = new();
    
    // Security features
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndTime { get; set; }
    public int? RemainingAttempts { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public string? SecurityMessage { get; set; }
    
    // Role-based dashboard routing
    public string DashboardUrl { get; set; } = "/";
    public string PrimaryRole { get; set; } = string.Empty;
    
    // Role-specific parameters (extracted from claims)
    public string? PropertyCode { get; set; }
    public string? PropertyName { get; set; }
    public string? UnitNumber { get; set; }
    public string? WorkerSpecialization { get; set; }
    
    // Factory methods for easy creation
    public static AuthenticationResult Success(string userId, string email, string displayName, List<string> roles, Dictionary<string, object>? claims = null)
    {
        var result = new AuthenticationResult
        {
            IsSuccess = true,
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            Roles = roles,
            Token = Guid.NewGuid().ToString(), // Simple token for demo
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            PrimaryRole = roles.FirstOrDefault() ?? ""
        };
        
        if (claims != null)
        {
            foreach (var claim in claims)
            {
                result.Claims[claim.Key] = claim.Value;
            }
            
            // Extract role-specific parameters
            ExtractRoleParameters(result, claims);
        }
        
        // Set dashboard URL based on primary role
        result.DashboardUrl = GetDashboardUrlForRole(result.PrimaryRole);
        
        return result;
    }
    
    public static AuthenticationResult Failure(string errorMessage, bool isLockedOut = false, DateTime? lockoutEndTime = null)
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            IsLockedOut = isLockedOut,
            LockoutEndTime = lockoutEndTime
        };
    }
    
    private static void ExtractRoleParameters(AuthenticationResult result, Dictionary<string, object> claims)
    {
        if (claims.TryGetValue("property_code", out var propertyCode))
            result.PropertyCode = propertyCode?.ToString();
            
        if (claims.TryGetValue("property_name", out var propertyName))
            result.PropertyName = propertyName?.ToString();
            
        if (claims.TryGetValue("unit_number", out var unitNumber))
            result.UnitNumber = unitNumber?.ToString();
            
        if (claims.TryGetValue("worker_specialization", out var specialization))
            result.WorkerSpecialization = specialization?.ToString();
    }
    
    private static string GetDashboardUrlForRole(string role)
    {
        return role switch
        {
            "SystemAdmin" => "/",
            "PropertySuperintendent" => "/",
            "Tenant" => "/",
            "Worker" => "/",
            _ => "/"
        };
    }
}

/// <summary>
/// Demo user information for WebUI display and management
/// </summary>
public class DemoUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, string> Claims { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    
    // Additional properties that WebUI might expect
    public string Role => Roles.FirstOrDefault() ?? "";
    public Dictionary<string, string> AdditionalClaims => Claims;
}

/// <summary>
/// Demo user validation result for authentication flows
/// </summary>
public class DemoUserValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndTime { get; set; }
    public DemoUserCredential? User { get; set; }
    
    public static DemoUserValidationResult Success(DemoUserCredential user)
    {
        return new DemoUserValidationResult
        {
            IsValid = true,
            User = user
        };
    }
    
    public static DemoUserValidationResult Failure(string errorMessage, bool isLockedOut = false, DateTime? lockoutEndTime = null)
    {
        return new DemoUserValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            IsLockedOut = isLockedOut,
            LockoutEndTime = lockoutEndTime
        };
    }
}

/// <summary>
/// Demo user credential model for Application layer
/// </summary>
public class DemoUserCredential
{
    public string Email { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, string> Claims { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}