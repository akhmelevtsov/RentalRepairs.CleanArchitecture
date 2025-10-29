using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Index page for TenantRequests - redirects to List page
/// This page handles the /TenantRequests route and redirects to /TenantRequests/List
/// </summary>
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // Redirect /TenantRequests to /TenantRequests/List
        return RedirectToPage("List");
    }
}