using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.WebUI.Models;
using System.Security.Claims;

namespace RentalRepairs.WebUI.Pages.Account;

/// <summary>
/// Unified Login page model - all users authenticate with email/password only
/// System automatically determines role and redirects to appropriate dashboard
/// Supports concurrent logins with optimistic concurrency for data integrity
/// </summary>
public class LoginModel : PageModel
{
    private readonly RentalRepairs.Application.Common.Interfaces.IAuthenticationService _authService;
    private readonly RentalRepairs.Application.Common.Interfaces.IDemoUserService _demoUserService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        RentalRepairs.Application.Common.Interfaces.IAuthenticationService authService,
        RentalRepairs.Application.Common.Interfaces.IDemoUserService demoUserService,
        ILogger<LoginModel> logger)
    {
        _authService = authService;
        _demoUserService = demoUserService;
        _logger = logger;
    }

    [BindProperty]
    public LoginViewModel Login { get; set; } = new();

    // Demo credentials for display
    public DemoCredentialsViewModel DemoCredentials { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData] 
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Load unified login page with demo credentials if available
    /// </summary>
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        try
        {
            _logger.LogInformation("Unified login page GET request - ReturnUrl: {ReturnUrl}", returnUrl);

            // Initialize model
            Login ??= new LoginViewModel();
            Login.ReturnUrl = returnUrl;

            // Load demo credentials for display
            await LoadDemoCredentials();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading login page");
            ErrorMessage = "Unable to load login page. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// CSRF BYPASSED: Unified login that allows concurrent sessions with optimistic concurrency
    /// </summary>
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("Unified login POST request received");

        try
        {
            if (Login == null || !ValidateLoginModel())
            {
                await LoadDemoCredentials();
                return Page();
            }

            _logger.LogInformation("Processing unified login for email: {Email} - concurrent sessions enabled", Login.Email);
            var result = await _authService.AuthenticateAsync(Login.Email, Login.Password);

            if (result.IsSuccess)
            {
                await SignInUserAsync(result, Login.RememberMe);
                
                _logger.LogInformation("User {Email} logged in successfully as {Role}, redirecting to: {DashboardUrl}", 
                    Login.Email, result.PrimaryRole, result.DashboardUrl);

                // Redirect to role-specific dashboard or return URL
                var redirectUrl = Login.ReturnUrl ?? result.DashboardUrl;
                return LocalRedirect(redirectUrl);
            }
            else
            {
                HandleAuthenticationFailure(result);
                await LoadDemoCredentials();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during unified login for {Email}", Login?.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            await LoadDemoCredentials();
            return Page();
        }
    }

    #region Private Helper Methods

    private async Task SignInUserAsync(RentalRepairs.Application.Common.Interfaces.AuthenticationResult result, bool rememberMe)
    {
        // Ensure we're completely signed out before signing in with new identity
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId ?? ""),
            new(ClaimTypes.Name, result.DisplayName ?? result.UserId ?? ""),
            new(ClaimTypes.Email, result.Email ?? "")
        };

        // Add role claims
        foreach (var role in result.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add custom claims from authentication result
        foreach (var claim in result.Claims)
        {
            claims.Add(new Claim(claim.Key, claim.Value?.ToString() ?? ""));
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
            ExpiresUtc = result.ExpiresAt != default ? result.ExpiresAt : DateTime.UtcNow.AddHours(8),
            AllowRefresh = true,
            IssuedUtc = DateTimeOffset.UtcNow
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {Email} signed in successfully with {ClaimCount} claims - concurrent sessions supported", result.Email, claims.Count);
    }

    private void HandleAuthenticationFailure(RentalRepairs.Application.Common.Interfaces.AuthenticationResult result)
    {
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, 
                $"Account is temporarily locked. Please try again after {result.LockoutEndTime:HH:mm:ss}.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid login credentials.");
        }
    }

    private async Task LoadDemoCredentials()
    {
        if (_demoUserService.IsDemoModeEnabled())
        {
            try
            {
                var demoUsers = await _demoUserService.GetDemoUsersForDisplayAsync();

                DemoCredentials = new DemoCredentialsViewModel
                {
                    AvailableUsers = demoUsers.Select(u => new RentalRepairs.WebUI.Models.DemoUserInfo
                    {
                        Email = u.Email,
                        DisplayName = u.DisplayName,
                        Roles = u.Roles,
                        Description = u.Description ?? $"{u.Role} - Demo User",
                        Claims = u.Claims
                    }).ToList(),
                    ShowCredentials = true,
                    DefaultPassword = _demoUserService.GetDefaultPassword()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load demo credentials for display");
                DemoCredentials = new DemoCredentialsViewModel();
            }
        }
    }

    private bool ValidateLoginModel()
    {
        var isValid = true;

        if (string.IsNullOrEmpty(Login.Email))
        {
            ModelState.AddModelError(nameof(Login.Email), "Email is required");
            isValid = false;
        }
        else if (!IsValidEmail(Login.Email))
        {
            ModelState.AddModelError(nameof(Login.Email), "Please enter a valid email address");
            isValid = false;
        }

        if (string.IsNullOrEmpty(Login.Password))
        {
            ModelState.AddModelError(nameof(Login.Password), "Password is required");
            isValid = false;
        }

        return isValid;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}