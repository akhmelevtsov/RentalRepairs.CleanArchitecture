using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Common.Exceptions;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Simplified AssignWorker page model using consolidated IWorkerService
/// CSRF PROTECTED: POST operations validate antiforgery tokens
/// Updated to use the consolidated service architecture
/// </summary>
[Authorize(Roles = "PropertySuperintendent,SystemAdmin")]
[ValidateAntiForgeryToken]
public class AssignWorkerModel : PageModel
{
    private readonly IWorkerService _workerService;
    private readonly ILogger<AssignWorkerModel> _logger;

    public AssignWorkerModel(
        IWorkerService workerService,
        ILogger<AssignWorkerModel> logger)
    {
        _workerService = workerService;
        _logger = logger;
    }

    // View model properties
    public WorkerAssignmentContextDto? AssignmentContext { get; set; }
    
    [BindProperty]
    public AssignWorkerRequestDto AssignmentRequest { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData] 
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Load worker assignment context
    /// </summary>
    public async Task<IActionResult> OnGetAsync(Guid requestId, string? returnUrl = null)
    {
        try
        {
            AssignmentContext = await _workerService.GetAssignmentContextAsync(requestId);
            
            // Initialize the form with the request ID
            AssignmentRequest.RequestId = requestId;
            
            // Store return URL for navigation after assignment
            ViewData["ReturnUrl"] = returnUrl;
            
            // Pre-populate scheduled date with first suggested date for better UX
            if (AssignmentContext?.SuggestedDates?.Any() == true)
            {
                AssignmentRequest.ScheduledDate = AssignmentContext.SuggestedDates.First();
            }
            
            return Page();
        }
        catch (NotFoundException)
        {
            TempData["ErrorMessage"] = "Tenant request not found.";
            return RedirectToPageBasedOnRole();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assign worker page for request {RequestId}", requestId);
            TempData["ErrorMessage"] = "Unable to load request assignment page. Please try again.";
            return RedirectToPageBasedOnRole();
        }
    }

    /// <summary>
    /// Handle worker assignment with CSRF protection
    /// </summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        try
        {
            _logger.LogInformation("Processing worker assignment for request {RequestId}. WorkerEmail: {WorkerEmail}", 
                AssignmentRequest.RequestId, AssignmentRequest.WorkerEmail);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for request {RequestId}. Errors: {Errors}", 
                    AssignmentRequest.RequestId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                ErrorMessage = "Please correct the validation errors below and try again.";
                ViewData["ReturnUrl"] = returnUrl; // Preserve ReturnUrl on error
                await ReloadAssignmentContext();
                return Page();
            }

            // Assign worker using consolidated service
            var result = await _workerService.AssignWorkerToRequestAsync(AssignmentRequest);
            
            if (result.IsSuccess)
            {
                SuccessMessage = result.SuccessMessage ?? "Worker assigned successfully.";
                _logger.LogInformation("Worker assignment successful for request {RequestId}", AssignmentRequest.RequestId);
                
                // Pass ReturnUrl to Details page so it knows where to navigate back
                object routeValues = new { id = AssignmentRequest.RequestId };
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    routeValues = new { id = AssignmentRequest.RequestId, returnUrl = returnUrl };
                }
                
                return RedirectToPage("/TenantRequests/Details", routeValues);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to assign worker.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                ViewData["ReturnUrl"] = returnUrl; // Preserve ReturnUrl on error
                await ReloadAssignmentContext();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning worker for request {RequestId}", AssignmentRequest.RequestId);
            ErrorMessage = $"An error occurred while assigning the worker: {ex.Message}";
            ModelState.AddModelError(string.Empty, ex.Message);
            
            ViewData["ReturnUrl"] = returnUrl; // Preserve ReturnUrl on error
            await ReloadAssignmentContext();
            return Page();
        }
    }

    /// <summary>
    /// AJAX endpoint to check if worker is available on the selected date
    /// </summary>
    public async Task<IActionResult> OnGetWorkerAvailabilityAsync(string workerEmail, DateTime date)
    {
        try
        {
            _logger.LogInformation("Checking worker availability for worker {WorkerEmail}, date {Date}", 
                workerEmail, date);

            if (string.IsNullOrWhiteSpace(workerEmail))
            {
                return new JsonResult(new { available = false, message = "Worker email is required" });
            }

            if (date.Date < DateTime.Today)
            {
                return new JsonResult(new { available = false, message = "Date must be in the future" });
            }

            // Check worker availability using consolidated service
            var isAvailable = await _workerService.IsWorkerAvailableAsync(workerEmail, date);
            
            return new JsonResult(new { 
                available = isAvailable,
                message = isAvailable 
                    ? "Worker is available on this date"
                    : "Worker is not available on this date (may already have maximum daily assignments)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking worker availability for worker {WorkerEmail} on {Date}", 
                workerEmail, date);
            return new JsonResult(new { available = false, message = $"Error checking availability: {ex.Message}" });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Redirect to appropriate page based on user role
    /// </summary>
    private IActionResult RedirectToPageBasedOnRole()
    {
        if (User.IsInRole("PropertySuperintendent"))
        {
            return RedirectToPage("/TenantRequests/Manage");
        }
        else if (User.IsInRole("SystemAdmin"))
        {
            return RedirectToPage("/Index");
        }
        else if (User.IsInRole("Worker"))
        {
            return RedirectToPage("/Index");
        }
        else
        {
            return RedirectToPage("/Index");
        }
    }

    /// <summary>
    /// Reload assignment context when errors occur
    /// </summary>
    private async Task ReloadAssignmentContext()
    {
        try
        {
            AssignmentContext = await _workerService.GetAssignmentContextAsync(AssignmentRequest.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading assignment context for request {RequestId}", AssignmentRequest.RequestId);
            // Don't throw here - let the original error be displayed
        }
    }

    #endregion
}