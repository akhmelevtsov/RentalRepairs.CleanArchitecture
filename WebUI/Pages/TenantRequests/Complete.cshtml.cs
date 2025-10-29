using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using RentalRepairs.Application.Commands.TenantRequests.ReportWorkCompleted;
using RentalRepairs.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Page for workers to complete work orders and report completion status
/// CSRF PROTECTED: All POST operations validate antiforgery tokens
/// Updated to use consolidated ITenantRequestService
/// </summary>
[Authorize(Roles = "Worker")]
[ValidateAntiForgeryToken]
public class CompleteModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ITenantRequestService _tenantRequestService;
    private readonly ILogger<CompleteModel> _logger;

    public CompleteModel(
        IMediator mediator,
        ITenantRequestService tenantRequestService,
        ILogger<CompleteModel> logger)
    {
        _mediator = mediator;
        _tenantRequestService = tenantRequestService;
        _logger = logger;
    }

    // Route parameter
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    // Form properties
    [BindProperty]
    public Guid RequestId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select whether the work was completed successfully")]
    public bool? CompletedSuccessfully { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Completion notes are required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Completion notes must be between 10 and 1000 characters")]
    public string CompletionNotes { get; set; } = string.Empty;

    // Display properties using consolidated service DTOs
    public TenantRequestDetailsDto? RequestDetails { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            RequestId = Id;
            
            // Load request details using consolidated service
            var workerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "anonymous";
            RequestDetails = await _tenantRequestService.GetRequestDetailsWithContextAsync(Id, workerEmail);
            
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
            if (string.IsNullOrEmpty(workerEmail) || !RequestDetails.AssignedWorkerEmail?.Equals(workerEmail, StringComparison.OrdinalIgnoreCase) == true)
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
            var workerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "anonymous";
            RequestDetails = await _tenantRequestService.GetRequestDetailsWithContextAsync(RequestId, workerEmail);
            
            if (RequestDetails == null)
            {
                ErrorMessage = "Work order not found.";
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Verify worker authorization again
            if (string.IsNullOrEmpty(workerEmail) || !RequestDetails.AssignedWorkerEmail?.Equals(workerEmail, StringComparison.OrdinalIgnoreCase) == true)
            {
                ErrorMessage = "You are not authorized to complete this work order.";
                return RedirectToPage("/Index");
            }

            // Create and send the completion command
            var command = new ReportWorkCompletedCommand
            {
                TenantRequestId = RequestId,
                CompletedSuccessfully = CompletedSuccessfully!.Value, // Safe because of validation
                CompletionNotes = CompletionNotes
            };

            await _mediator.Send(command);

            // Create detailed success message for dashboard display
            var statusText = CompletedSuccessfully.Value ? "completed successfully" : "reported as failed/incomplete";
            var workOrderText = !string.IsNullOrEmpty(RequestDetails.WorkOrderNumber) 
                ? $"Work Order {RequestDetails.WorkOrderNumber}" 
                : "Work order";
            
            SuccessMessage = $"{workOrderText} {statusText}! Property: {RequestDetails.PropertyName}, Unit: {RequestDetails.TenantUnit}";

            _logger.LogInformation("Work order {RequestId} completed by worker {WorkerEmail} with status: {Status}", 
                RequestId, workerEmail, CompletedSuccessfully.Value ? "Success" : "Failed");

            // Redirect to dashboard for optimal UX workflow
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