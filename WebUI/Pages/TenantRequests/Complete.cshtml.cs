using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using RentalRepairs.Application.Commands.TenantRequests.ReportWorkCompleted;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Page for workers to complete work orders and report completion status.
/// Uses CQRS directly (no service wrapper).
/// CSRF PROTECTED: All POST operations validate antiforgery tokens.
/// </summary>
[Authorize(Roles = "Worker")]
[ValidateAntiForgeryToken]
public class CompleteModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompleteModel> _logger;

    public CompleteModel(
        IMediator mediator,
        ILogger<CompleteModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // Route parameter
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    // Form properties
    [BindProperty] public Guid RequestId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select whether the work was completed successfully")]
    public bool? CompletedSuccessfully { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Completion notes are required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Completion notes must be between 10 and 1000 characters")]
    public string CompletionNotes { get; set; } = string.Empty;

    // Display properties
    public TenantRequestDetailsDto? RequestDetails { get; set; }

    [TempData] public string? SuccessMessage { get; set; }

    [TempData] public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            RequestId = Id;

            // Load request details using CQRS query directly
            var query = new GetTenantRequestByIdQuery(Id) { IncludeBusinessContext = true };
            var result = await _mediator.Send(query);
            RequestDetails = result as TenantRequestDetailsDto;

            if (RequestDetails == null)
            {
                ErrorMessage = "Work order not found.";
                return RedirectToPage("/Index");
            }

            // Verify this is a scheduled request
            if (RequestDetails.Status != "Scheduled")
            {
                ErrorMessage = "This work order is not in a scheduled status and cannot be completed.";
                return RedirectToPage("/tenant-requests/Details", new { id = Id });
            }

            // Verify worker is assigned to this request
            var workerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(workerEmail) ||
                !RequestDetails.AssignedWorkerEmail?.Equals(workerEmail, StringComparison.OrdinalIgnoreCase) == true)
            {
                ErrorMessage = "You are not authorized to complete this work order.";
                return RedirectToPage("/Index");
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading complete page for request {RequestId}", Id);
            ErrorMessage = "Unable to load work order details. Please try again.";
            return RedirectToPage("/Index");
        }
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
// Reload request details for display if validation fails
            var query = new GetTenantRequestByIdQuery(RequestId) { IncludeBusinessContext = true };
            var result = await _mediator.Send(query);
            RequestDetails = result as TenantRequestDetailsDto;

            if (RequestDetails == null)
            {
                ErrorMessage = "Work order not found.";
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid) return Page();

            // Verify worker authorization again
            var workerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(workerEmail) ||
                !RequestDetails.AssignedWorkerEmail?.Equals(workerEmail, StringComparison.OrdinalIgnoreCase) == true)
            {
                ErrorMessage = "You are not authorized to complete this work order.";
                return RedirectToPage("/Index");
            }

            // Create and send the completion command
            var command = new ReportWorkCompletedCommand
            {
                TenantRequestId = RequestId,
                CompletedSuccessfully = CompletedSuccessfully!.Value,
                CompletionNotes = CompletionNotes
            };

            await _mediator.Send(command);

            // Create detailed success message
            var statusText = CompletedSuccessfully.Value ? "completed successfully" : "reported as failed/incomplete";
            var workOrderText = !string.IsNullOrEmpty(RequestDetails.WorkOrderNumber)
                ? $"Work Order {RequestDetails.WorkOrderNumber}"
                : "Work order";

            SuccessMessage =
                $"{workOrderText} {statusText}! Property: {RequestDetails.PropertyName}, Unit: {RequestDetails.TenantUnit}";

            _logger.LogInformation(
                "Work order {RequestId} completed by worker {WorkerEmail} with status: {Status}",
                RequestId, workerEmail, CompletedSuccessfully.Value ? "Success" : "Failed");

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing work order {RequestId}", RequestId);
            ErrorMessage = "An error occurred while submitting your completion report. Please try again.";
            return Page();
        }
    }
}