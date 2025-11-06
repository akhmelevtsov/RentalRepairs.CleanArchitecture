using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using FluentValidation;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Common.Exceptions;
using RentalRepairs.Application.Commands.TenantRequests.ScheduleServiceWork;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Page model for assigning workers to tenant requests.
/// Uses CQRS directly - calls command via MediatR (no service wrapper).
/// CSRF PROTECTED: POST operations validate antiforgery tokens.
/// </summary>
[Authorize(Roles = "PropertySuperintendent,SystemAdmin")]
[ValidateAntiForgeryToken]
public class AssignWorkerModel : PageModel
{
    private readonly IWorkerService _workerService;
    private readonly IMediator _mediator;
    private readonly ILogger<AssignWorkerModel> _logger;

    public AssignWorkerModel(
        IWorkerService workerService,
        IMediator mediator,
        ILogger<AssignWorkerModel> logger)
    {
        _workerService = workerService;
        _mediator = mediator;
        _logger = logger;
    }

    // View model properties
    public WorkerAssignmentContextDto? AssignmentContext { get; set; }

    [BindProperty] public Guid RequestId { get; set; }

    [BindProperty] public string WorkerEmail { get; set; } = string.Empty;

    [BindProperty] public DateTime ScheduledDate { get; set; }

    [BindProperty] public string WorkOrderNumber { get; set; } = string.Empty;

    [TempData] public string? SuccessMessage { get; set; }

    [TempData] public string? ErrorMessage { get; set; }

    public DateTime DataLoadedAt { get; set; }
    public bool IsDataStale => (DateTime.UtcNow - DataLoadedAt).TotalMinutes > 5;

    /// <summary>
    /// Load worker assignment context.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(Guid requestId, string? returnUrl = null)
    {
        try
        {
            _logger.LogInformation("Loading worker assignment context for request {RequestId}", requestId);

            AssignmentContext = await _workerService.GetAssignmentContextAsync(requestId);

            // Initialize form properties
            RequestId = requestId;
            DataLoadedAt = DateTime.UtcNow;

            // Pre-populate scheduled date with first suggested date for better UX
            if (AssignmentContext?.SuggestedDates?.Any() == true)
                ScheduledDate = AssignmentContext.SuggestedDates.First();

            ViewData["ReturnUrl"] = returnUrl;

            _logger.LogInformation(
                "Loaded {WorkerCount} workers for request {RequestId}, emergency={IsEmergency}",
                AssignmentContext?.AvailableWorkers?.Count ?? 0,
                requestId,
                AssignmentContext?.IsEmergencyRequest ?? false);

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
    /// Handle worker assignment with CSRF protection.
    /// Uses CQRS directly - calls command via MediatR.
    /// </summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        try
        {
            // Convert local date to UTC for consistent timezone handling
            var localDate = ScheduledDate.Date;
            var utcDate = DateTime.SpecifyKind(localDate, DateTimeKind.Utc);

            _logger.LogInformation(
                "Processing worker assignment: Request={RequestId}, Worker={WorkerEmail}, Date={ScheduledDate}",
                RequestId, WorkerEmail, utcDate);

            // Validate model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Model state is invalid for request {RequestId}. Errors: {Errors}",
                    RequestId,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                ErrorMessage = "Please correct the validation errors below and try again.";
                ViewData["ReturnUrl"] = returnUrl;
                await ReloadAssignmentContext();
                return Page();
            }

            // Create and send command via MediatR (TRUE CQRS)
            var command = new ScheduleServiceWorkCommand
            {
                TenantRequestId = RequestId,
                WorkerEmail = WorkerEmail,
                ScheduledDate = utcDate,
                WorkOrderNumber = WorkOrderNumber
            };

            await _mediator.Send(command);

            // Success - set message and redirect
            TempData.Remove("ErrorMessage"); // Clear any previous errors
            SuccessMessage = $"Work successfully assigned to {WorkerEmail} for {utcDate:yyyy-MM-dd}";

            _logger.LogInformation(
                "Worker assignment successful for request {RequestId}",
                RequestId);

            // Navigate to details page
            object routeValues;
            if (!string.IsNullOrEmpty(returnUrl))
                routeValues = new { id = RequestId, returnUrl = returnUrl };
            else
                routeValues = new { id = RequestId };

            return RedirectToPage("/TenantRequests/Details", routeValues);
        }
        catch (FluentValidation.ValidationException ex)
        {
            // FluentValidation errors
            var errors = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning(
                "Validation failed for request {RequestId}: {Errors}",
                RequestId, errors);

            ErrorMessage = $"Validation failed: {errors}";
            ViewData["ReturnUrl"] = returnUrl;
            await ReloadAssignmentContext();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning worker for request {RequestId}", RequestId);
            ErrorMessage = "An error occurred while assigning the worker. Please try again.";
            ViewData["ReturnUrl"] = returnUrl;
            await ReloadAssignmentContext();
            return Page();
        }
    }

    #region Private Helper Methods

    private IActionResult RedirectToPageBasedOnRole()
    {
        if (User.IsInRole("PropertySuperintendent"))
            return RedirectToPage("/TenantRequests/Manage");
        if (User.IsInRole("SystemAdmin"))
            return RedirectToPage("/Index");
        if (User.IsInRole("Worker"))
            return RedirectToPage("/Index");

        return RedirectToPage("/Index");
    }

    private async Task ReloadAssignmentContext()
    {
        try
        {
            var previousLoadTime = DataLoadedAt;
            AssignmentContext = await _workerService.GetAssignmentContextAsync(RequestId);

            // Preserve original load time (don't reset staleness timer)
            DataLoadedAt = previousLoadTime != default ? previousLoadTime : DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading assignment context for request {RequestId}", RequestId);
            // Don't throw - let original error be displayed
        }
    }

    #endregion
}