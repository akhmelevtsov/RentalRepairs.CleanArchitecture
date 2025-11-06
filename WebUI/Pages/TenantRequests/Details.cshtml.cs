using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using RentalRepairs.Application.Commands.TenantRequests.SubmitTenantRequest;
using RentalRepairs.Application.Commands.TenantRequests.CloseRequest;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using RentalRepairs.WebUI.Helpers;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Simplified page model using CQRS directly (no service wrapper).
/// Query handler enriches DTO with business context when requested.
/// CSRF PROTECTED: All POST operations validate antiforgery tokens.
/// </summary>
[Authorize]
[ValidateAntiForgeryToken]
public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IMediator mediator,
        ILogger<DetailsModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // View model properties
    public TenantRequestDetailsDto? TenantRequest { get; set; }

    [TempData] public string? SuccessMessage { get; set; }

    [TempData] public string? ErrorMessage { get; set; }

    /// <summary>
    /// Load tenant request details using CQRS query directly.
    /// Query handler enriches with business context (authorization, available actions).
    /// </summary>
    public async Task<IActionResult> OnGetAsync(Guid id, string? returnUrl = null)
    {
        try
        {
            // Store ReturnUrl in ViewData for use in the view
            ViewData["ReturnUrl"] = returnUrl;

            // Use CQRS query directly with business context enrichment
            var query = new GetTenantRequestByIdQuery(id) { IncludeBusinessContext = true };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                ErrorMessage = "Tenant request not found.";
                return RedirectToPage("/TenantRequests/Index");
            }

            // Cast to enriched DTO type
            TenantRequest = result as TenantRequestDetailsDto;

            if (TenantRequest == null)
            {
                _logger.LogError(
                    "Query returned TenantRequestDto instead of TenantRequestDetailsDto for request {RequestId}",
                    id);
                ErrorMessage = "Unable to load request details with business context.";
                return RedirectToPage("/TenantRequests/Index");
            }

            _logger.LogDebug(
                "Loaded request details for {RequestId}. Tenant: {TenantName}, Email: {TenantEmail}, Unit: {Unit}, Property: {Property}",
                id, TenantRequest.TenantFullName, TenantRequest.TenantEmail, TenantRequest.TenantUnit,
                TenantRequest.PropertyName);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenant request details for {RequestId}", id);
            ErrorMessage = "Unable to load tenant request details. Please try again.";
            return RedirectToPage("/TenantRequests/Index");
        }
    }

    /// <summary>
    /// CSRF PROTECTED: Handle request submission using MediatR directly
    /// </summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostSubmitAsync(Guid id)
    {
        try
        {
            var command = new SubmitTenantRequestCommand
            {
                TenantRequestId = id
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                SuccessMessage = "Request submitted successfully.";
                _logger.LogInformation("Request {RequestId} submitted successfully by user {UserId}",
                    id, User.Identity?.Name);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to submit request.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting request {RequestId}", id);
            ErrorMessage = "An error occurred while submitting the request.";
        }

        return RedirectToPage(new { id });
    }

    /// <summary>
    /// CSRF PROTECTED: Handle request closure using MediatR directly
    /// </summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostCloseAsync(Guid id, string closureNotes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(closureNotes))
            {
                ErrorMessage = "Closure notes are required.";
                return RedirectToPage(new { id });
            }

            var command = new CloseRequestCommand
            {
                TenantRequestId = id,
                ClosureNotes = closureNotes
            };

            await _mediator.Send(command);
            SuccessMessage = "Request closed successfully.";

            _logger.LogInformation("Request {RequestId} closed successfully by user {UserId} with notes: {Notes}",
                id, User.Identity?.Name, closureNotes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing request {RequestId}", id);
            ErrorMessage = "An error occurred while closing the request.";
        }

        return RedirectToPage(new { id });
    }

    #region UI Helper Methods

    /// <summary>
    /// Get Bootstrap badge class for status display
    /// </summary>
    public string GetStatusBadgeClass()
    {
        if (TenantRequest == null) return TenantRequestUIHelper.GetStatusBadgeClass("Unknown");
        return TenantRequestUIHelper.GetStatusBadgeClass(TenantRequest.Status);
    }

    /// <summary>
    /// Get urgency styling class
    /// </summary>
    public string GetUrgencyClass()
    {
        if (TenantRequest == null) return "";
        return TenantRequestUIHelper.GetUrgencyClass(TenantRequest.UrgencyLevel == "Emergency");
    }

    /// <summary>
    /// Check if emergency alert should be shown
    /// </summary>
    public bool ShouldShowEmergencyAlert()
    {
        return TenantRequest?.UrgencyLevel == "Emergency";
    }

    /// <summary>
    /// Check if overdue warning should be shown
    /// </summary>
    public bool ShouldShowOverdueWarning()
    {
        if (TenantRequest == null) return false;

        // Check if request is overdue (simple logic)
        return TenantRequest.Status == "Submitted" &&
               TenantRequest.CreatedDate < DateTime.Now.AddDays(-3);
    }

    /// <summary>
    /// Check if work assignment section should be visible
    /// </summary>
    public bool ShouldShowWorkAssignment()
    {
        if (TenantRequest == null) return false;
        return TenantRequest.Status == "Scheduled" ||
               TenantRequest.Status == "Done" ||
               !string.IsNullOrEmpty(TenantRequest.AssignedWorkerEmail);
    }

    /// <summary>
    /// Get progress percentage for request workflow
    /// </summary>
    public int GetProgressPercentage()
    {
        if (TenantRequest == null) return 0;

        // Convert string status to enum if possible, otherwise use string-based logic
        return TenantRequest.Status switch
        {
            "Draft" => 10,
            "Submitted" => 25,
            "Scheduled" => 50,
            "Done" => 90,
            "Closed" => 100,
            "Declined" => 100,
            "Failed" => 40,
            _ => 0
        };
    }

    /// <summary>
    /// Get formatted creation date
    /// </summary>
    public string GetFormattedCreatedDate()
    {
        if (TenantRequest == null) return "Unknown";
        return TenantRequestUIHelper.FormatDate(TenantRequest.CreatedDate);
    }

    /// <summary>
    /// Get formatted scheduled date
    /// </summary>
    public string GetFormattedScheduledDate()
    {
        if (TenantRequest == null) return "Not scheduled";
        return TenantRequestUIHelper.FormatDate(TenantRequest.ScheduledDate);
    }

    /// <summary>
    /// Get status description for user
    /// </summary>
    public string GetStatusDescription()
    {
        if (TenantRequest == null) return "Status unknown";

        return TenantRequest.Status switch
        {
            "Draft" => "Request is being prepared",
            "Submitted" => "Request has been submitted and is awaiting review",
            "Scheduled" => "Work has been scheduled with a worker",
            "Done" => "Work has been completed",
            "Closed" => "Request has been closed",
            "Declined" => "Request was declined",
            "Failed" => "Work attempt failed and needs rescheduling",
            _ => $"Status: {TenantRequest.Status}"
        };
    }

    /// <summary>
    /// Get available quick actions for current status and user role
    /// </summary>
    public List<string> GetAvailableQuickActions()
    {
        if (TenantRequest == null) return new List<string>();

        var actions = new List<string>();

        // Get user role and determine available actions
        if (User.IsInRole("SystemAdmin"))
        {
            actions.AddRange(new[] { "Edit", "Assign Worker", "Close", "View History" });
        }
        else if (User.IsInRole("PropertySuperintendent"))
        {
            if (TenantRequest.Status == "Submitted")
                actions.AddRange(new[] { "Assign Worker", "Decline" });
            else if (TenantRequest.Status == "Done")
                actions.Add("Close");
        }
        else if (User.IsInRole("Worker"))
        {
            if (TenantRequest.Status == "Scheduled")
                actions.Add("Report Work Completed");
        }
        else if (User.IsInRole("Tenant"))
        {
            if (TenantRequest.Status == "Draft")
                actions.AddRange(new[] { "Submit", "Edit" });
        }

        return actions;
    }

    #endregion
}