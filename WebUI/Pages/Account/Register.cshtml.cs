using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Constants;
using RentalRepairs.WebUI.Models;

namespace RentalRepairs.WebUI.Pages.Account;

/// <summary>
/// Demo user registration page for portfolio demonstration
/// Only available when demo mode is enabled
/// Updated to use Application layer interfaces only (Clean Architecture)
/// </summary>
public class RegisterModel : PageModel
{
    private readonly IDemoUserService _demoUserService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IDemoUserService demoUserService, ILogger<RegisterModel> logger)
    {
        _demoUserService = demoUserService;
        _logger = logger;
    }

    [BindProperty] public RegisterDemoUserViewModel Register { get; set; } = new();

    // ? Use Application layer type instead of Infrastructure type
    public List<Application.Common.Interfaces.DemoUserInfo> ExistingDemoUsers { get; set; } = new();

    // ? Expose demo mode status for the Razor page
    public bool IsDemoModeEnabled => _demoUserService.IsDemoModeEnabled();

    // Property to determine if registration is allowed
    public bool IsRegistrationDisabled => IsDemoModeEnabled; // In demo mode, registration is disabled

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_demoUserService.IsDemoModeEnabled()) return NotFound("Demo registration is not available");

        await LoadExistingDemoUsers();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ? Check if registration is disabled (demo mode)
        if (IsRegistrationDisabled)
        {
            ModelState.AddModelError(string.Empty, "User registration is disabled in demo mode.");
            await LoadExistingDemoUsers();
            return Page();
        }

        await LoadExistingDemoUsers();

        if (!ModelState.IsValid) return Page();

        try
        {
            var claims = new Dictionary<string, string>();

            // Add role-specific claims using Application layer constants
            switch (Register.SelectedRole)
            {
                case UserRoles.Tenant:
                    if (!string.IsNullOrWhiteSpace(Register.PropertyCode))
                        claims["property_code"] = Register.PropertyCode;
                    if (!string.IsNullOrWhiteSpace(Register.UnitNumber))
                        claims["unit_number"] = Register.UnitNumber;
                    if (!string.IsNullOrWhiteSpace(Register.PropertyName))
                        claims["property_name"] = Register.PropertyName;
                    break;

                case UserRoles.Worker:
                    if (!string.IsNullOrWhiteSpace(Register.WorkerSpecialization))
                        claims["worker_specialization"] = Register.WorkerSpecialization;
                    break;

                case UserRoles.PropertySuperintendent:
                    if (!string.IsNullOrWhiteSpace(Register.PropertyCode))
                        claims["property_code"] = Register.PropertyCode;
                    if (!string.IsNullOrWhiteSpace(Register.PropertyName))
                        claims["property_name"] = Register.PropertyName;
                    break;
            }

            var success = await _demoUserService.RegisterDemoUserAsync(
                Register.Email,
                Register.Password,
                Register.DisplayName,
                new List<string> { Register.SelectedRole },
                claims);

            if (success)
            {
                TempData["Success"] =
                    $"Demo user '{Register.Email}' has been registered successfully! You can now login with these credentials.";
                return RedirectToPage("/Account/Login");
            }
            else
            {
                ModelState.AddModelError(string.Empty,
                    "Failed to register demo user. The email may already be in use or demo registration may be disabled.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering demo user {Email}", Register.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
            return Page();
        }
    }

    private async Task LoadExistingDemoUsers()
    {
        try
        {
            // Only load demo users if in demo mode
            if (IsDemoModeEnabled)
                // ? Use Application layer interface method
                ExistingDemoUsers = await _demoUserService.GetDemoUsersForDisplayAsync();
            else
                ExistingDemoUsers = new List<Application.Common.Interfaces.DemoUserInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load existing demo users for display");
            ExistingDemoUsers = new List<Application.Common.Interfaces.DemoUserInfo>();
        }
    }
}