using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.WebUI.Models;
using Mapster;
using RentalRepairs.Application.Commands.Properties.RegisterProperty;

namespace RentalRepairs.WebUI.Pages.Properties;

/// <summary>
/// ? SIMPLIFIED: Property registration using direct MediatR calls
/// ? CSRF PROTECTED: POST operations validate antiforgery tokens
/// Clean, focused implementation following established pattern
/// </summary>
[ValidateAntiForgeryToken] // ? CSRF Protection: Validate tokens on all POST requests
public class RegisterModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IMediator mediator, ILogger<RegisterModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [BindProperty]
    public RegisterPropertyViewModel Property { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData] 
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// ? Simple GET handler - just display the form
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        await Task.CompletedTask; // Keep async for consistency
        return Page();
    }

    /// <summary>
    /// ? CSRF PROTECTED: Register property using direct MediatR command
    /// Antiforgery token validation prevents CSRF attacks
    /// </summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // ? CSRF Protection: Token validation happens automatically
            
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Use Mapster to convert view model to command
            var command = Property.Adapt<RegisterPropertyCommand>();
            var propertyId = await _mediator.Send(command);

            SuccessMessage = $"Property '{Property.Name}' has been registered successfully.";
            
            _logger.LogInformation("Property {PropertyName} with code {PropertyCode} registered successfully with ID {PropertyId}", 
                Property.Name, Property.Code, propertyId);
            
            // Redirect to property details page if it exists, otherwise to a listing page
            return RedirectToPage("/Properties/Index"); // Adjust as needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering property {PropertyName} with code {PropertyCode}", 
                Property.Name, Property.Code);
            ErrorMessage = "An error occurred while registering the property. Please try again.";
            return Page();
        }
    }
}