using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Reschedule page that redirects to AssignWorker with appropriate context
/// This provides a clear user experience for rescheduling existing assignments
/// Handles different user roles appropriately
/// </summary>
[Authorize] // Allow any authenticated user to access this page initially
public class RescheduleModel : PageModel
{
    private readonly ILogger<RescheduleModel> _logger;

    public RescheduleModel(ILogger<RescheduleModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles reschedule requests with proper role-based authorization
    /// </summary>
    public IActionResult OnGet(Guid id)
    {
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown";
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown";

        _logger.LogInformation(
            "User {UserEmail} with role claim {RoleClaim} attempting to reschedule request {RequestId}",
            userEmail, roleClaim, id);

        // Use ASP.NET Core's built-in User.IsInRole() method which handles the claim type correctly
        var hasPropertySuperintendentRole = User.IsInRole("PropertySuperintendent");
        var hasSystemAdminRole = User.IsInRole("SystemAdmin");

        _logger.LogInformation(
            "Role checks: PropertySuperintendent={PropertySuperintendent}, SystemAdmin={SystemAdmin}",
            hasPropertySuperintendentRole, hasSystemAdminRole);

        // Check if user has required role for rescheduling
        if (!hasPropertySuperintendentRole && !hasSystemAdminRole)
        {
            _logger.LogWarning(
                "User {UserEmail} with role {RoleClaim} denied access to reschedule request {RequestId} - insufficient permissions",
                userEmail, roleClaim, id);

            // Set appropriate error message based on user role
            if (User.IsInRole("Worker"))
                TempData["ErrorMessage"] =
                    "Workers cannot reschedule requests. Please contact your property superintendent if the schedule needs to be changed.";
            else if (User.IsInRole("Tenant"))
                TempData["ErrorMessage"] =
                    "Tenants cannot reschedule work assignments. Please contact your property management office if you need to change the scheduled work time.";
            else
                TempData["ErrorMessage"] =
                    $"You don't have permission to reschedule requests. Your current role is '{roleClaim}'. Only property superintendents and system administrators can reschedule work assignments.";

            // Redirect back to request details instead of showing generic access denied
            return RedirectToPage("/TenantRequests/Details", new { id = id });
        }

        // Set a TempData message to indicate this is a reschedule operation
        TempData["RescheduleContext"] =
            "This request is being rescheduled. Update the worker assignment and scheduled date as needed.";
        TempData["IsReschedule"] = true;

        _logger.LogInformation("Redirecting user {UserEmail} to reschedule (assign worker) for request {RequestId}",
            userEmail, id);

        // Redirect to the AssignWorker page which handles all the assignment logic
        return RedirectToPage("/TenantRequests/AssignWorker", new { requestId = id });
    }
}