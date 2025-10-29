using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Commands.TenantRequests;
using RentalRepairs.Application.Queries.Properties.GetPropertyByCode;
using RentalRepairs.Application.Queries.Tenants.GetTenantByPropertyAndUnit;
using RentalRepairs.WebUI.Models;
using RentalRepairs.WebUI.Helpers;
using RentalRepairs.WebUI.Services;
using RentalRepairs.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// ? SIMPLIFIED: Submit page model using direct Application layer calls
/// ? CSRF PROTECTED: POST operations validate antiforgery tokens
/// ? UI HELPER INTEGRATED: Uses centralized TenantRequestUIHelper for UI logic
/// ? DEMO MODE ENHANCED: Includes random request generation for demo purposes
/// Clean, focused implementation following established pattern
/// </summary>
[Authorize(Roles = "Tenant")]
[ValidateAntiForgeryToken] // ? CSRF Protection: Validate tokens on all POST requests
public class SubmitModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IDemoUserService _demoUserService;
    private readonly ILogger<SubmitModel> _logger;

    public SubmitModel(IMediator mediator, IDemoUserService demoUserService, ILogger<SubmitModel> logger)
    {
        _mediator = mediator;
        _demoUserService = demoUserService;
        _logger = logger;
    }

    [BindProperty]
    public SubmitTenantRequestPageViewModel TenantRequest { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData] 
    public string? ErrorMessage { get; set; }

    // Display properties for the Razor page
    public string DisplayPropertyCode { get; set; } = string.Empty;
    public string DisplayPropertyName { get; set; } = string.Empty;
    public string DisplayUnitNumber { get; set; } = string.Empty;
    public string DisplayTenantName { get; set; } = string.Empty;
    public string DisplayTenantEmail { get; set; } = string.Empty;
    public string DisplayTenantPhone { get; set; } = string.Empty; // Added for read-only display

    // ? DEMO MODE: Check if demo mode is enabled for showing random generation button
    public bool IsDemoModeEnabled => _demoUserService.IsDemoModeEnabled();

    /// <summary>
    /// ? Load page with tenant information from authentication claims and database
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            await PopulateTenantInfoFromClaims();
            await PopulateTenantPhoneFromDatabase(); // Add this to get phone from database
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading submit page for user {Email}", User.Identity?.Name);
            ErrorMessage = "Unable to load the submission form. Please try again.";
            return RedirectToPage("/Index");
        }
    }

    /// <summary>
    /// ? DEMO MODE: Generate random request data for demonstration purposes
    /// </summary>
    public IActionResult OnGetGenerateRandom()
    {
        if (!IsDemoModeEnabled)
        {
            return BadRequest("Random generation only available in demo mode");
        }

        try
        {
            var randomData = RandomTenantRequestGenerator.GenerateRandomRequest();
            return new JsonResult(new
            {
                success = true,
                data = new
                {
                    problemDescription = randomData.ProblemDescription,
                    urgencyLevel = randomData.UrgencyLevel,
                    preferredContactTime = randomData.PreferredContactTime
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random request data");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to generate random request data"
            });
        }
    }

    /// <summary>
    /// ? CSRF PROTECTED: Submit tenant request using direct MediatR calls
    /// Antiforgery token validation prevents CSRF attacks
    /// </summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // ? CSRF Protection: Token validation happens automatically
            
            // Ensure tenant info is populated from claims for security
            await PopulateTenantInfoFromClaims();
            await PopulateTenantPhoneFromDatabase(); // Ensure phone is populated

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for tenant request submission by {Email}. Errors: {Errors}", 
                    TenantRequest.TenantEmail, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            // Verify property exists
            var property = await _mediator.Send(new GetPropertyByCodeQuery(TenantRequest.PropertyCode));
            if (property == null)
            {
                _logger.LogError("Property not found for code {PropertyCode} during request submission by {Email}", 
                    TenantRequest.PropertyCode, TenantRequest.TenantEmail);
                ModelState.AddModelError(string.Empty, "Invalid property code.");
                return Page();
            }

            // Verify tenant exists and matches authenticated user
            var tenant = await _mediator.Send(new GetTenantByPropertyAndUnitQuery(property.Id, TenantRequest.UnitNumber));
            if (tenant == null)
            {
                _logger.LogError("Tenant not found for property {PropertyId} and unit {Unit} during request submission by {Email}", 
                    property.Id, TenantRequest.UnitNumber, TenantRequest.TenantEmail);
                ModelState.AddModelError(string.Empty, "Tenant not found for this property and unit.");
                return Page();
            }

            // Security check: Verify email matches authenticated user
            if (!tenant.ContactInfo.EmailAddress.Equals(TenantRequest.TenantEmail, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Email mismatch for tenant request submission: {AuthEmail} vs {TenantEmail}", 
                    TenantRequest.TenantEmail, tenant.ContactInfo.EmailAddress);
                ModelState.AddModelError(string.Empty, "Authentication error. Please log out and log back in.");
                return Page();
            }

            _logger.LogInformation("Creating tenant request for {Email} with urgency {Urgency} and description length {DescLength}", 
                TenantRequest.TenantEmail, TenantRequest.UrgencyLevel, TenantRequest.ProblemDescription?.Length ?? 0);

            // Create tenant request using Application layer command with all required properties
            var command = new CreateTenantRequestCommand
            {
                PropertyId = property.Id, // Set the PropertyId from the retrieved property
                TenantId = tenant.Id,
                TenantEmail = tenant.ContactInfo.EmailAddress, // Set TenantEmail from tenant data
                Title = CreateRequestTitle(TenantRequest.ProblemDescription),
                Description = TenantRequest.ProblemDescription,
                UrgencyLevel = TenantRequest.UrgencyLevel,
                
                // Additional required properties for complete tenant request
                TenantFullName = tenant.ContactInfo.FullName, // Use FullName property directly
                TenantUnit = tenant.UnitNumber,
                PropertyName = property.Name,
                PropertyPhone = property.PhoneNumber ?? "Not available", // Use PhoneNumber property
                SuperintendentFullName = property.Superintendent?.FullName ?? "Not assigned", // Use Superintendent.FullName
                SuperintendentEmail = property.Superintendent?.EmailAddress ?? "Not available" // Use Superintendent.EmailAddress
            };

            var requestId = await _mediator.Send(command);

            SuccessMessage = "Your maintenance request has been submitted successfully. You will receive a confirmation email shortly.";
            
            _logger.LogInformation("Tenant request {RequestId} created successfully for tenant {TenantEmail}", 
                requestId, TenantRequest.TenantEmail);
                
            // Redirect to index page which automatically shows correct role-based dashboard
            return RedirectToPage("/Index");
        }
        catch (RentalRepairs.Domain.Exceptions.MaxPendingRequestsExceededException ex)
        {
            _logger.LogWarning("Tenant {Email} exceeded maximum pending requests limit: {Message}", 
                TenantRequest.TenantEmail, ex.Message);
            
            ErrorMessage = $"You have reached the maximum number of pending requests ({ex.MaxAllowed}). " +
                          $"Please wait for some of your existing requests to be processed before submitting new ones. " +
                          $"Current pending requests: {ex.CurrentCount}";
            
            return Page();
        }
        catch (RentalRepairs.Application.Common.Exceptions.ValidationException ex)
        {
            _logger.LogWarning("Validation failed for tenant request submission by {Email}: {ValidationErrors}", 
                TenantRequest.TenantEmail, string.Join("; ", ex.Errors.SelectMany(kvp => kvp.Value).ToArray()));
            
            // Add validation errors to ModelState
            foreach (var error in ex.Errors)
            {
                foreach (var errorMessage in error.Value)
                {
                    ModelState.AddModelError(error.Key, errorMessage);
                }
            }
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting tenant request for {TenantEmail}. Request details: Title='{Title}', Urgency='{Urgency}', Description length={DescLength}", 
                TenantRequest.TenantEmail, 
                CreateRequestTitle(TenantRequest.ProblemDescription ?? ""), 
                TenantRequest.UrgencyLevel,
                TenantRequest.ProblemDescription?.Length ?? 0);
                
            // Provide more specific error message based on exception type
            ErrorMessage = ex switch
            {
                ArgumentException argEx => $"Invalid request data: {argEx.Message}",
                InvalidOperationException invOpEx => $"Cannot submit request: {invOpEx.Message}",
                _ => "An error occurred while submitting your request. Please try again. If the problem persists, please contact support."
            };
            
            return Page();
        }
    }

    #region ? CENTRALIZED UI HELPERS: Use TenantRequestUIHelper for consistent UI logic

    /// <summary>
    /// ? UI HELPER: Get urgency badge class for visual urgency display
    /// </summary>
    public string GetUrgencyBadgeClass(string urgencyLevel)
    {
        return TenantRequestUIHelper.GetUrgencyBadgeClass(urgencyLevel);
    }

    /// <summary>
    /// ? UI HELPER: Get urgency icon for visual urgency display
    /// </summary>
    public string GetUrgencyIcon(string urgencyLevel)
    {
        return TenantRequestUIHelper.GetUrgencyIcon(urgencyLevel);
    }

    /// <summary>
    /// ? UI HELPER: Check if urgency level is emergency
    /// </summary>
    public bool IsEmergencyUrgency(string urgencyLevel)
    {
        return urgencyLevel?.ToLowerInvariant() is "critical" or "emergency";
    }

    /// <summary>
    /// ? UI HELPER: Get urgency warning message for critical requests
    /// </summary>
    public string GetUrgencyWarningMessage()
    {
        return "Critical requests will be prioritized and you will be contacted as soon as possible. For immediate life-threatening situations, please call 911.";
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// ? Populate tenant information from authentication claims (security-focused)
    /// </summary>
    private async Task PopulateTenantInfoFromClaims()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User must be authenticated");
        }

        // Extract information from authentication claims
        var tenantEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var tenantName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
        var propertyCode = User.FindFirst("property_code")?.Value ?? "";
        var propertyName = User.FindFirst("property_name")?.Value ?? "";
        var unitNumber = User.FindFirst("unit_number")?.Value ?? "";

        if (string.IsNullOrEmpty(tenantEmail) || string.IsNullOrEmpty(propertyCode))
        {
            throw new InvalidOperationException("Missing required tenant claims");
        }

        // Set display properties for Razor page
        DisplayTenantEmail = tenantEmail;
        DisplayTenantName = tenantName;
        DisplayPropertyCode = propertyCode;
        DisplayPropertyName = propertyName;
        DisplayUnitNumber = unitNumber;

        // Populate model with authenticated user information
        TenantRequest.TenantEmail = tenantEmail;
        TenantRequest.PropertyCode = propertyCode;
        TenantRequest.UnitNumber = unitNumber;

        // Parse name into first/last name components
        var nameParts = tenantName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        TenantRequest.TenantFirstName = nameParts.Length > 0 ? nameParts[0] : "";
        TenantRequest.TenantLastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

        await Task.CompletedTask;
    }

    /// <summary>
    /// ? Populate tenant phone from database for read-only display
    /// </summary>
    private async Task PopulateTenantPhoneFromDatabase()
    {
        try
        {
            // Get property first
            var property = await _mediator.Send(new GetPropertyByCodeQuery(TenantRequest.PropertyCode));
            if (property != null)
            {
                // Get tenant details including phone
                var tenant = await _mediator.Send(new GetTenantByPropertyAndUnitQuery(property.Id, TenantRequest.UnitNumber));
                if (tenant != null)
                {
                    DisplayTenantPhone = tenant.ContactInfo.MobilePhone ?? "Not provided";
                    // Pre-populate the phone field with database value
                    TenantRequest.TenantPhone = tenant.ContactInfo.MobilePhone ?? "";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load tenant phone for {Email}", TenantRequest.TenantEmail);
            DisplayTenantPhone = "Not available";
        }
    }

    /// <summary>
    /// ? Create appropriate title from problem description
    /// </summary>
    private static string CreateRequestTitle(string problemDescription)
    {
        if (string.IsNullOrWhiteSpace(problemDescription))
            return "Maintenance Request";

        // Create title from first part of description, truncated appropriately
        var title = problemDescription.Length <= 50 
            ? problemDescription 
            : problemDescription[..47] + "...";

        return title;
    }

    #endregion
}