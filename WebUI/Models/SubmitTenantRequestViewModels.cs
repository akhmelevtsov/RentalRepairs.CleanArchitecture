using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.WebUI.Models;

/// <summary>
/// Presentation model specifically designed for the TenantRequest Submit page.
/// Contains only the data and validation rules needed for that specific use case.
/// </summary>
public class SubmitTenantRequestPageViewModel
{
    // Phone is now read-only from tenant profile - no validation needed
    [Display(Name = "Phone Number")]
    public string TenantPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Problem description is required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
    [Display(Name = "Problem Description")]
    public string ProblemDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an urgency level")]
    [Display(Name = "Urgency Level")]
    public string UrgencyLevel { get; set; } = "Normal";

    [Display(Name = "Preferred Contact Time")]
    public string? PreferredContactTime { get; set; } // ? Added missing property

    // Display-only properties populated from user context
    public string TenantEmail { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string TenantFirstName { get; set; } = string.Empty;
    public string TenantLastName { get; set; } = string.Empty;
    public List<string> AvailableUrgencyLevels { get; set; } = new();
}