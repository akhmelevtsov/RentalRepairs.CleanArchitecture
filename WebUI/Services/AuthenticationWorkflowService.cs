using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace RentalRepairs.WebUI.Services;

/// <summary>
/// Service to handle authentication workflows using unified authentication approach.
/// Updated to use single authentication method for all user types.
/// </summary>
public interface IAuthenticationWorkflowService
{
    Task<AuthenticationWorkflowResult> AuthenticateUserAsync(UnifiedLoginRequest request, bool rememberMe);
    Task SignOutAsync();
    Task<bool> IsEmailValidAsync(string email);
}

public class AuthenticationWorkflowService : IAuthenticationWorkflowService
{
    private readonly RentalRepairs.Application.Common.Interfaces.IAuthenticationService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationWorkflowService> _logger;

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext not available");

    public AuthenticationWorkflowService(
        RentalRepairs.Application.Common.Interfaces.IAuthenticationService authService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationWorkflowService> logger)
    {
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AuthenticationWorkflowResult> AuthenticateUserAsync(UnifiedLoginRequest request, bool rememberMe)
    {
        try
        {
            _logger.LogInformation("Processing unified login for email: {Email}", request.Email);

            // Validate email format
            if (!await IsEmailValidAsync(request.Email))
            {
                return AuthenticationWorkflowResult.Failed("Please enter a valid email address");
            }

            // Use unified authentication - system determines role automatically
            var result = await _authService.AuthenticateAsync(request.Email, request.Password);

            if (result.IsSuccess)
            {
                await SignInUserAsync(result, rememberMe);
                _logger.LogInformation("User {Email} logged in successfully as {Role}, redirecting to {DashboardUrl}", 
                    request.Email, result.PrimaryRole, result.DashboardUrl);
                
                return AuthenticationWorkflowResult.Success(
                    $"Login successful! Welcome {result.DisplayName}", 
                    result.DashboardUrl);
            }
            else
            {
                _logger.LogWarning("Login failed for {Email}: {ErrorMessage}", request.Email, result.ErrorMessage);
                return AuthenticationWorkflowResult.Failed(result.ErrorMessage ?? "Invalid login credentials.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during unified login for {Email}", request.Email);
            return AuthenticationWorkflowResult.Failed("An error occurred during login. Please try again.");
        }
    }

    public async Task SignOutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User signed out successfully");
    }

    public Task<bool> IsEmailValidAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(false);

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return Task.FromResult(addr.Address == email);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private async Task SignInUserAsync(RentalRepairs.Application.Common.Interfaces.AuthenticationResult result, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId ?? ""),
            new(ClaimTypes.Name, result.DisplayName ?? ""),
            new(ClaimTypes.Email, result.Email ?? "")
        };

        // Add role claims
        foreach (var role in result.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add custom claims from authentication result
        _logger.LogInformation("Adding {ClaimCount} custom claims for user {Email}", result.Claims.Count, result.Email);
        foreach (var claim in result.Claims)
        {
            claims.Add(new Claim(claim.Key, claim.Value?.ToString() ?? ""));
            _logger.LogInformation("Added claim: {ClaimType} = {ClaimValue}", claim.Key, claim.Value);
        }

        // Add role-specific claims for easy access
        if (!string.IsNullOrEmpty(result.PropertyCode))
            claims.Add(new Claim("property_code", result.PropertyCode));
        if (!string.IsNullOrEmpty(result.PropertyName))
            claims.Add(new Claim("property_name", result.PropertyName));
        if (!string.IsNullOrEmpty(result.UnitNumber))
            claims.Add(new Claim("unit_number", result.UnitNumber));
        if (!string.IsNullOrEmpty(result.WorkerSpecialization))
            claims.Add(new Claim("worker_specialization", result.WorkerSpecialization));

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = result.ExpiresAt != default ? result.ExpiresAt : DateTime.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {Email} signed in successfully with {ClaimCount} claims", result.Email, claims.Count);
    }
}

// Service models
public class AuthenticationWorkflowResult
{
    public bool IsSuccess { get; private set; }
    public string? Message { get; private set; }
    public string? RedirectUrl { get; private set; }
    public string? ErrorMessage => IsSuccess ? null : Message;
    public string? SuccessMessage => IsSuccess ? Message : null;

    private AuthenticationWorkflowResult(bool isSuccess, string? message = null, string? redirectUrl = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        RedirectUrl = redirectUrl;
    }

    public static AuthenticationWorkflowResult Success(string? message = null, string? redirectUrl = null) => 
        new(true, message, redirectUrl);
    public static AuthenticationWorkflowResult Failed(string message) => new(false, message);
}

public class UnifiedLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Legacy request models - marked as obsolete but kept for backward compatibility
[Obsolete("Use UnifiedLoginRequest instead - system now determines user role automatically")]
public class AdminLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

[Obsolete("Use UnifiedLoginRequest instead - system now determines user role automatically")]
public class TenantLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
}

[Obsolete("Use UnifiedLoginRequest instead - system now determines user role automatically")]
public class WorkerLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
}