using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RentalRepairs.WebUI.Pages.Account;

/// <summary>
/// Simple logout without session management - supports concurrent logins
/// </summary>
public class LogoutModel : PageModel
{
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(ILogger<LogoutModel> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await ProcessLogoutAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await ProcessLogoutAsync();
    }

    private async Task<IActionResult> ProcessLogoutAsync()
    {
        try
        {
            // Clear authentication (no session management needed)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("User logged out successfully - concurrent logins supported");

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToPage("/Account/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout process");
            TempData["Error"] = "Logout encountered an error, but you have been signed out.";
            return RedirectToPage("/Account/Login");
        }
    }
}