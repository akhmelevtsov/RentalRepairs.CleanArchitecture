using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Common.Interfaces;
using System.Security.Claims;

namespace RentalRepairs.Infrastructure.Authentication;

/// <summary>
/// Consolidated Authentication Service - Implements Application layer interface directly
/// All users authenticate with email/password only
/// System automatically determines role and parameters from stored user profile
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IWorkerService? _workerService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IDemoUserService _demoUserService;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IDemoUserService demoUserService,
        IWorkerService? workerService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _logger = logger;
        _demoUserService = demoUserService;
        _workerService = workerService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Unified authentication method - determines user role and parameters automatically
    /// </summary>
    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Attempting unified authentication for email: {Email}", email);

            // Validate input parameters
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Authentication failed: Email or password is empty");
                return AuthenticationResult.Failure("Email and password are required");
            }

            // Use secure demo user service
            if (_demoUserService.IsDemoModeEnabled())
            {
                var validationResult = await _demoUserService.ValidateUserAsync(email, password);

                if (!validationResult.IsValid)
                    return AuthenticationResult.Failure(
                        validationResult.ErrorMessage ?? "Invalid credentials",
                        validationResult.IsLockedOut,
                        validationResult.LockoutEndTime);

                var user = validationResult.User!;

                // Create claims dictionary for result
                var claimsDict = new Dictionary<string, object>();
                foreach (var claim in user.Claims) claimsDict[claim.Key] = claim.Value;

                // Create successful authentication result with role detection
                var result = AuthenticationResult.Success(
                    user.Email,
                    user.Email,
                    user.DisplayName,
                    user.Roles.ToList(),
                    claimsDict);

                _logger.LogInformation(
                    "User {Email} authenticated successfully with role(s): {Roles}, redirecting to: {DashboardUrl}",
                    email,
                    string.Join(", ", user.Roles),
                    result.DashboardUrl);

                // Log role-specific parameters for debugging
                LogRoleSpecificParameters(result);

                return result;
            }

            // In production, this would integrate with your actual user store
            _logger.LogWarning("Demo mode is disabled and no production authentication configured for {Email}", email);
            return AuthenticationResult.Failure("Authentication service not properly configured");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for {Email}", email);
            return AuthenticationResult.Failure("Authentication failed due to an internal error");
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;

            if (_demoUserService.IsDemoModeEnabled())
            {
                var validationResult = await _demoUserService.ValidateUserAsync(email, password);
                return validationResult.IsValid;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credential validation failed for {Email}", email);
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            // Enhanced token validation (still simple for demo, but better than before)
            if (string.IsNullOrEmpty(token))
                return false;

            // Basic token format validation - in production, use JWT validation
            if (!token.StartsWith("secure_token_") || token.Length < 20)
                return false;

            // Extract timestamp and validate expiration
            var parts = token.Split('_');
            if (parts.Length >= 4 && long.TryParse(parts[3], out var timestamp))
            {
                var tokenTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                var expirationTime = tokenTime.AddHours(8);

                return DateTime.UtcNow <= expirationTime;
            }

            await Task.CompletedTask;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return false;
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User {UserId} signed out", userId);
            }

            // In a real implementation, you'd invalidate the token/session
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign out failed");
        }
    }

    #region Private Methods

    /// <summary>
    /// Log role-specific parameters for debugging and monitoring
    /// </summary>
    private void LogRoleSpecificParameters(AuthenticationResult result)
    {
        switch (result.PrimaryRole)
        {
            case "Tenant":
                _logger.LogDebug("Tenant authenticated - Property: {PropertyCode}, Unit: {UnitNumber}",
                    result.PropertyCode, result.UnitNumber);
                break;

            case "Worker":
                _logger.LogDebug("Worker authenticated - Specialization: {Specialization}",
                    result.WorkerSpecialization);
                break;

            case "PropertySuperintendent":
                _logger.LogDebug("Superintendent authenticated - Property: {PropertyCode}",
                    result.PropertyCode);
                break;

            case "SystemAdmin":
                _logger.LogDebug("System Admin authenticated - Full access granted");
                break;
        }
    }

    #endregion
}