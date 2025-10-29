namespace RentalRepairs.Domain.Entities;

/// <summary>
/// User domain entity representing an authenticated user in the system.
/// Contains user identity and authorization information needed for business rules.
/// </summary>
public class User
{
    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public List<string> Roles { get; private set; }
    public string PrimaryRole { get; private set; }
    public Dictionary<string, string> Claims { get; private set; }
    
    // Role-specific properties extracted from claims
    public string? PropertyCode { get; private set; }
    public string? PropertyName { get; private set; }
    public string? UnitNumber { get; private set; }
    public string? WorkerSpecialization { get; private set; }
    public string? WorkerId { get; private set; }

   

    public User(string userId, string email, string displayName, List<string> roles, Dictionary<string, string>? claims = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(email));
        }

        if (roles == null || !roles.Any())
        {
            throw new ArgumentException("User must have at least one role", nameof(roles));
        }

        UserId = userId;
        Email = email.ToLowerInvariant(); // Normalize email for comparison
        DisplayName = displayName ?? string.Empty;
        Roles = roles.ToList(); // Create defensive copy
        PrimaryRole = roles.First();
        Claims = claims?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();

        // Extract role-specific properties from claims
        ExtractRoleSpecificProperties();
    }

    #region Role Check Methods

    /// <summary>
    /// Checks if user has system administrator role.
    /// </summary>
    public bool IsSystemAdmin() => Roles.Contains("SystemAdmin");

    /// <summary>
    /// Checks if user has property superintendent role.
    /// </summary>
    public bool IsPropertySuperintendent() => Roles.Contains("PropertySuperintendent");

    /// <summary>
    /// Checks if user is a tenant.
    /// </summary>
    public bool IsTenant() => Roles.Contains("Tenant");

    /// <summary>
    /// Checks if user is a maintenance worker.
    /// </summary>
    public bool IsWorker() => Roles.Contains("Worker");

    /// <summary>
    /// Checks if user has the specified role.
    /// </summary>
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if user has any of the specified roles.
    /// </summary>
    public bool HasAnyRole(params string[] roles) => roles.Any(role => HasRole(role));

    #endregion

    #region Claim Access Methods

    /// <summary>
    /// Gets a claim value by key.
    /// </summary>
    public string? GetClaim(string key) => Claims.TryGetValue(key, out string? value) ? value : null;

    /// <summary>
    /// Checks if user has a specific claim.
    /// </summary>
    public bool HasClaim(string key) => Claims.ContainsKey(key);

    /// <summary>
    /// Checks if user has a claim with a specific value.
    /// </summary>
    public bool HasClaim(string key, string value) => 
        Claims.TryGetValue(key, out string? claimValue) && 
        string.Equals(claimValue, value, StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a system admin user for testing/seeding purposes.
    /// </summary>
    public static User CreateSystemAdmin(string email, string displayName = "System Administrator")
    {
        return new User(email, email, displayName, new List<string> { "SystemAdmin" });
    }

    /// <summary>
    /// Creates a tenant user with property information.
    /// </summary>
    public static User CreateTenant(string email, string displayName, string propertyCode, string unitNumber, string? propertyName = null)
    {
        var claims = new Dictionary<string, string>
        {
            ["property_code"] = propertyCode,
            ["unit_number"] = unitNumber
        };
        
        if (!string.IsNullOrEmpty(propertyName))
        {
            claims["property_name"] = propertyName;
        }

        return new User(email, email, displayName, new List<string> { "Tenant" }, claims);
    }

    /// <summary>
    /// Creates a property superintendent user.
    /// </summary>
    public static User CreatePropertySuperintendent(string email, string displayName, string propertyCode, string? propertyName = null)
    {
        var claims = new Dictionary<string, string>
        {
            ["property_code"] = propertyCode
        };
        
        if (!string.IsNullOrEmpty(propertyName))
        {
            claims["property_name"] = propertyName;
        }

        return new User(email, email, displayName, new List<string> { "PropertySuperintendent" }, claims);
    }

    /// <summary>
    /// Creates a worker user with specialization.
    /// </summary>
    public static User CreateWorker(string email, string displayName, string specialization, string? workerId = null)
    {
        var claims = new Dictionary<string, string>
        {
            ["worker_specialization"] = specialization
        };
        
        if (!string.IsNullOrEmpty(workerId))
        {
            claims["worker_id"] = workerId;
        }

        return new User(email, email, displayName, new List<string> { "Worker" }, claims);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Extracts role-specific properties from claims for easy access.
    /// </summary>
    private void ExtractRoleSpecificProperties()
    {
        PropertyCode = GetClaim("property_code");
        PropertyName = GetClaim("property_name");
        UnitNumber = GetClaim("unit_number");
        WorkerSpecialization = GetClaim("worker_specialization");
        WorkerId = GetClaim("worker_id");
    }

    #endregion

    #region Equality and String Representation

    public override bool Equals(object? obj)
    {
        return obj is User other && Email.Equals(other.Email, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Email.ToLowerInvariant().GetHashCode();
    }

    public override string ToString()
    {
        return $"User: {DisplayName} ({Email}) - Roles: {string.Join(", ", Roles)}";
    }

    #endregion
}
