using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;
using RentalRepairs.Application.Commands.TenantRequests.DeclineTenantRequest;
using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Page model for declining tenant requests with reason.
/// Uses CQRS directly (no service wrapper).
/// CSRF PROTECTED: All POST operations validate antiforgery tokens.
/// </summary>
[Authorize(Roles = "SystemAdmin,PropertySuperintendent")]
[ValidateAntiForgeryToken]
public class DeclineModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<DeclineModel> _logger;

    public DeclineModel(
        IMediator mediator,
        ILogger<DeclineModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // Page properties
    public TenantRequestDetailsDto? Request { get; set; }

    [BindProperty] public Guid RequestId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please provide a reason for declining this request")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 1000 characters")]
    [Display(Name = "Reason for Declining")]
    public string Reason { get; set; } = string.Empty;

    [TempData] public string? SuccessMessage { get; set; }

    [TempData] public string? ErrorMessage { get; set; }

    /// <summary>
    /// Load request details for decline page using CQRS query.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            RequestId = id;

            // Load request details using CQRS query directly
            var query = new GetTenantRequestByIdQuery(id) { IncludeBusinessContext = true };
            var result = await _mediator.Send(query);
            Request = result as TenantRequestDetailsDto;

            if (Request == null)
            {
                ErrorMessage = "Request not found.";
                return RedirectToPage("/TenantRequests/Index");
            }

            // Check if request can be declined
            if (Request.Status != "Submitted")
            {
                ErrorMessage =
                    $"Request cannot be declined. Current status: {Request.Status}. Only submitted requests can be declined.";
                return RedirectToPage("Details", new { id });
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading decline page for request {RequestId}", id);
            ErrorMessage = "An error occurred while loading the request details.";
            return RedirectToPage("/TenantRequests/Index");
        }
    }

    /// <summary>
    /// CSRF PROTECTED: Handle request decline using MediatR.
    /// </summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeclineAsync()
    {
        if (!ModelState.IsValid)
        {
            // Reload request details if validation fails
            var query = new GetTenantRequestByIdQuery(RequestId) { IncludeBusinessContext = true };
            var result = await _mediator.Send(query);
            Request = result as TenantRequestDetailsDto;
            return Page();
        }

        try
        {
            var command = new DeclineTenantRequestCommand
            {
                TenantRequestId = RequestId,
                Reason = Reason.Trim()
            };

            await _mediator.Send(command);

            SuccessMessage = "Request has been declined successfully. The tenant will be notified.";

            _logger.LogInformation(
                "Request {RequestId} declined by user {UserId} with reason: {Reason}",
                RequestId, User.Identity?.Name, Reason);

            return RedirectToPage("Details", new { id = RequestId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unable to decline request {RequestId}: {Message}", RequestId, ex.Message);
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining request {RequestId}", RequestId);
            ErrorMessage = "An error occurred while declining the request. Please try again.";
        }

        // Reload request details on error
        var reloadQuery = new GetTenantRequestByIdQuery(RequestId) { IncludeBusinessContext = true };
        var reloadResult = await _mediator.Send(reloadQuery);
        Request = reloadResult as TenantRequestDetailsDto;
        return Page();
    }
}