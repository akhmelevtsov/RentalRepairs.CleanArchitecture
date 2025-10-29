using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RentalRepairs.Infrastructure.Authentication.Models;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Infrastructure.Authentication.Services;

/// <summary>
/// Implementation of demo user service using Application layer interfaces
/// Session management removed in favor of optimistic concurrency for data integrity
/// </summary>
public class DemoUserService : IDemoUserService
{
    private readonly DemoAuthenticationSettings _settings;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<DemoUserService> _logger;
    private readonly Dictionary<string, List<LoginAttempt>> _loginAttempts = new();

    public DemoUserService(
        IOptions<DemoAuthenticationSettings> settings,
        IPasswordService passwordService,
        ILogger<DemoUserService> logger)
    {
        _settings = settings.Value;
        _passwordService = passwordService;
        _logger = logger;
    }

    public bool IsDemoModeEnabled() => _settings.EnableDemoMode;

    public string GetDefaultPassword() => _settings.DefaultPassword;

    public async Task<DemoUserValidationResult> ValidateUserAsync(string email, string password)
    {
        if (!_settings.EnableDemoMode)
        {
            return DemoUserValidationResult.Failure("Demo mode is not enabled");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return DemoUserValidationResult.Failure("Email and password are required");
        }

        // Check for lockout (security measure remains)
        if (IsUserLockedOut(email))
        {
            var lockoutEndTime = GetLockoutEndTime(email);
            return DemoUserValidationResult.Failure(
                $"Account is locked until {lockoutEndTime:HH:mm:ss}",
                true,
                lockoutEndTime);
        }

        var normalizedEmail = email.ToLowerInvariant();
        
        if (!_settings.DemoUsers.TryGetValue(normalizedEmail, out var infrastructureUser))
        {
            RecordFailedAttempt(email, "User not found");
            return DemoUserValidationResult.Failure("Invalid email or password");
        }

        if (!infrastructureUser.IsActive)
        {
            RecordFailedAttempt(email, "User is inactive");
            return DemoUserValidationResult.Failure("Account is disabled");
        }

        if (!_passwordService.VerifyPassword(password, infrastructureUser.HashedPassword))
        {
            RecordFailedAttempt(email, "Invalid password");
            return DemoUserValidationResult.Failure("Invalid email or password");
        }

        // Clear failed attempts on successful login
        ClearFailedAttempts(email);
        RecordSuccessfulAttempt(email);

        _logger.LogInformation("Demo user {Email} authenticated successfully - concurrent logins allowed", email);

        // Convert Infrastructure model to Application model
        var applicationUser = new RentalRepairs.Application.Common.Interfaces.DemoUserCredential
        {
            Email = infrastructureUser.Email,
            HashedPassword = infrastructureUser.HashedPassword,
            DisplayName = infrastructureUser.DisplayName,
            Roles = infrastructureUser.Roles,
            Claims = infrastructureUser.Claims,
            IsActive = infrastructureUser.IsActive,
            Description = infrastructureUser.Description
        };

        return DemoUserValidationResult.Success(applicationUser);
    }

    public async Task<RentalRepairs.Application.Common.Interfaces.DemoUserCredential?> GetDemoUserAsync(string email)
    {
        if (!_settings.EnableDemoMode)
            return null;

        var normalizedEmail = email.ToLowerInvariant();
        
        if (!_settings.DemoUsers.TryGetValue(normalizedEmail, out var infrastructureUser))
            return null;

        // Convert Infrastructure model to Application model
        return new RentalRepairs.Application.Common.Interfaces.DemoUserCredential
        {
            Email = infrastructureUser.Email,
            HashedPassword = infrastructureUser.HashedPassword,
            DisplayName = infrastructureUser.DisplayName,
            Roles = infrastructureUser.Roles,
            Claims = infrastructureUser.Claims,
            IsActive = infrastructureUser.IsActive,
            Description = infrastructureUser.Description
        };
    }

    public async Task<bool> RegisterDemoUserAsync(string email, string password, string displayName, List<string> roles, Dictionary<string, string>? claims = null)
    {
        if (!_settings.EnableDemoMode || !_settings.Security.AllowUserRegistration)
        {
            _logger.LogWarning("Demo user registration attempted but not allowed");
            return false;
        }

        if (!_passwordService.IsValidPassword(password, _settings.Security.MinPasswordLength))
        {
            _logger.LogWarning("Demo user registration failed: password does not meet requirements");
            return false;
        }

        var normalizedEmail = email.ToLowerInvariant();

        if (_settings.DemoUsers.ContainsKey(normalizedEmail))
        {
            _logger.LogWarning("Demo user registration failed: email {Email} already exists", email);
            return false;
        }

        var hashedPassword = _passwordService.HashPassword(password);

        var newUser = new RentalRepairs.Infrastructure.Authentication.Models.DemoUserCredential
        {
            Email = normalizedEmail,
            HashedPassword = hashedPassword,
            DisplayName = displayName,
            Roles = roles ?? new List<string>(),
            Claims = claims ?? new Dictionary<string, string>(),
            IsActive = true,
            Description = "Registered demo user"
        };

        _settings.DemoUsers[normalizedEmail] = newUser;

        _logger.LogInformation("Demo user {Email} registered successfully with roles: {Roles}", 
            email, string.Join(", ", roles ?? new List<string>()));

        return true;
    }

    public async Task<List<RentalRepairs.Application.Common.Interfaces.DemoUserInfo>> GetDemoUsersForDisplayAsync()
    {
        if (!_settings.EnableDemoMode || !_settings.Security.ShowDemoCredentials)
            return new List<RentalRepairs.Application.Common.Interfaces.DemoUserInfo>();

        return _settings.DemoUsers.Values
            .Where(u => u.IsActive)
            .Select(u => new RentalRepairs.Application.Common.Interfaces.DemoUserInfo
            {
                Email = u.Email,
                DisplayName = u.DisplayName,
                Roles = u.Roles,
                Description = u.Description,
                IsActive = u.IsActive,
                Claims = u.Claims
            })
            .OrderBy(u => u.DisplayName)
            .ToList();
    }

    public async Task InitializeDemoUsersAsync()
    {
        if (!_settings.EnableDemoMode)
        {
            _logger.LogInformation("Demo mode disabled, skipping demo user initialization");
            return;
        }

        if (_settings.DemoUsers.Any())
        {
            _logger.LogInformation("Demo users already configured ({Count} users) - concurrent logins enabled", _settings.DemoUsers.Count);
            return;
        }

        _logger.LogInformation("Initializing default demo users with concurrent login support");

        // Hash the default password
        var hashedPassword = _passwordService.HashPassword(_settings.DefaultPassword);

        // Create default demo users using Infrastructure models
        var defaultUsers = new Dictionary<string, RentalRepairs.Infrastructure.Authentication.Models.DemoUserCredential>
        {
            // System Administrator
            ["admin@demo.com"] = new()
            {
                Email = "admin@demo.com",
                HashedPassword = hashedPassword,
                DisplayName = "Demo Administrator",
                Roles = new List<string> { "SystemAdmin" },
                Claims = new Dictionary<string, string>(),
                IsActive = true,
                Description = "System Administrator with full access"
            },

            // Property Superintendents
            ["super.sun001@rentalrepairs.com"] = new()
            {
                Email = "super.sun001@rentalrepairs.com",
                HashedPassword = hashedPassword,
                DisplayName = "Robert Martinez",
                Roles = new List<string> { "PropertySuperintendent" },
                Claims = new Dictionary<string, string>
                {
                    ["property_code"] = "SUN001",
                    ["property_name"] = "Sunset Apartments"
                },
                IsActive = true,
                Description = "Superintendent for Sunset Apartments"
            },
            ["super.map001@rentalrepairs.com"] = new()
            {
                Email = "super.map001@rentalrepairs.com",
                HashedPassword = hashedPassword,
                DisplayName = "Linda Thompson",
                Roles = new List<string> { "PropertySuperintendent" },
                Claims = new Dictionary<string, string>
                {
                    ["property_code"] = "MAP001",
                    ["property_name"] = "Maple Grove Condos"
                },
                IsActive = true,
                Description = "Superintendent for Maple Grove Condos"
            },
            ["super.oak001@rentalrepairs.com"] = new()
            {
                Email = "super.oak001@rentalrepairs.com",
                HashedPassword = hashedPassword,
                DisplayName = "Michael Anderson",
                Roles = new List<string> { "PropertySuperintendent" },
                Claims = new Dictionary<string, string>
                {
                    ["property_code"] = "OAK001",
                    ["property_name"] = "Oak Hill Residences"
                },
                IsActive = true,
                Description = "Superintendent for Oak Hill Residences"
            },

            // Sample Tenant
            ["tenant1.unit101@sunset.com"] = new()
            {
                Email = "tenant1.unit101@sunset.com",
                HashedPassword = hashedPassword,
                DisplayName = "John Smith",
                Roles = new List<string> { "Tenant" },
                Claims = new Dictionary<string, string>
                {
                    ["property_code"] = "SUN001",
                    ["property_name"] = "Sunset Apartments",
                    ["unit_number"] = "101",
                    ["tenant_id"] = "1"
                },
                IsActive = true,
                Description = "Tenant in Sunset Apartments Unit 101"
            },

            // Maintenance Workers - All workers with unique names
            ["appliancerepair.davis@workers.com"] = new()
            {
                Email = "appliancerepair.davis@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Anna Davis",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Appliance Repair",
                    ["worker_id"] = "1"
                },
                IsActive = true,
                Description = "Appliance Repair Specialist"
            },
            ["carpenter.garcia@workers.com"] = new()
            {
                Email = "carpenter.garcia@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Emma Garcia",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Carpenter",
                    ["worker_id"] = "2"
                },
                IsActive = true,
                Description = "Carpenter Specialist"
            },
            ["electrician.martinez@workers.com"] = new()
            {
                Email = "electrician.martinez@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Jessica Martinez",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Electrician",
                    ["worker_id"] = "3"
                },
                IsActive = true,
                Description = "Electrician Specialist"
            },
            ["electrician.johnson@workers.com"] = new()
            {
                Email = "electrician.johnson@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Sarah Johnson",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Electrician",
                    ["worker_id"] = "4"
                },
                IsActive = true,
                Description = "Electrician Specialist"
            },
            ["generalmaintenance.brown@workers.com"] = new()
            {
                Email = "generalmaintenance.brown@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Lisa Brown",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "General Maintenance",
                    ["worker_id"] = "5"
                },
                IsActive = true,
                Description = "General Maintenance Specialist"
            },
            ["hvac.williams@workers.com"] = new()
            {
                Email = "hvac.williams@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Tom Williams",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "HVAC",
                    ["worker_id"] = "6"
                },
                IsActive = true,
                Description = "HVAC Specialist"
            },
            ["locksmith.miller@workers.com"] = new()
            {
                Email = "locksmith.miller@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "James Miller",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Locksmith",
                    ["worker_id"] = "7"
                },
                IsActive = true,
                Description = "Locksmith Specialist"
            },
            ["painter.jones@workers.com"] = new()
            {
                Email = "painter.jones@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "David Jones",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Painter",
                    ["worker_id"] = "8"
                },
                IsActive = true,
                Description = "Painter Specialist"
            },
            ["plumber.smith@workers.com"] = new()
            {
                Email = "plumber.smith@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Mike Smith",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Plumber",
                    ["worker_id"] = "9"
                },
                IsActive = true,
                Description = "Plumber Specialist"
            },
            ["plumber.rodriguez@workers.com"] = new()
            {
                Email = "plumber.rodriguez@workers.com",
                HashedPassword = hashedPassword,
                DisplayName = "Robert Rodriguez",
                Roles = new List<string> { "Worker" },
                Claims = new Dictionary<string, string>
                {
                    ["worker_specialization"] = "Plumber",
                    ["worker_id"] = "10"
                },
                IsActive = true,
                Description = "Plumber Specialist"
            }
        };

        foreach (var (email, user) in defaultUsers)
        {
            _settings.DemoUsers[email] = user;
        }

        _logger.LogInformation("Initialized {Count} default demo users with concurrent login support", defaultUsers.Count);
    }

    #region Private Login Attempt Tracking (Security - Keep for rate limiting)

    private bool IsUserLockedOut(string email)
    {
        if (!_loginAttempts.ContainsKey(email))
            return false;

        var attempts = _loginAttempts[email];
        var cutoffTime = DateTime.UtcNow.AddMinutes(-_settings.Security.LockoutDurationMinutes);
        
        // Remove old attempts
        attempts.RemoveAll(a => a.AttemptTime < cutoffTime);

        var recentFailedAttempts = attempts
            .Where(a => !a.IsSuccessful && a.AttemptTime > cutoffTime)
            .Count();

        return recentFailedAttempts >= _settings.Security.MaxLoginAttempts;
    }

    private DateTime? GetLockoutEndTime(string email)
    {
        if (!_loginAttempts.ContainsKey(email))
            return null;

        var attempts = _loginAttempts[email];
        var lastFailedAttempt = attempts
            .Where(a => !a.IsSuccessful)
            .OrderByDescending(a => a.AttemptTime)
            .FirstOrDefault();

        return lastFailedAttempt?.AttemptTime.AddMinutes(_settings.Security.LockoutDurationMinutes);
    }

    private void RecordFailedAttempt(string email, string reason)
    {
        if (!_loginAttempts.ContainsKey(email))
            _loginAttempts[email] = new List<LoginAttempt>();

        _loginAttempts[email].Add(new LoginAttempt
        {
            Email = email,
            AttemptTime = DateTime.UtcNow,
            IsSuccessful = false,
            FailureReason = reason
        });

        _logger.LogWarning("Failed login attempt for {Email}: {Reason}", email, reason);
    }

    private void RecordSuccessfulAttempt(string email)
    {
        if (!_loginAttempts.ContainsKey(email))
            _loginAttempts[email] = new List<LoginAttempt>();

        _loginAttempts[email].Add(new LoginAttempt
        {
            Email = email,
            AttemptTime = DateTime.UtcNow,
            IsSuccessful = true
        });
    }

    private void ClearFailedAttempts(string email)
    {
        if (_loginAttempts.ContainsKey(email))
        {
            _loginAttempts[email].Clear();
        }
    }

    #endregion
}