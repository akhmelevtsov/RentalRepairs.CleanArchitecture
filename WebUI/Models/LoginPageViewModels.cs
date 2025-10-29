using System.ComponentModel.DataAnnotations;
using RentalRepairs.WebUI.Services;

namespace RentalRepairs.WebUI.Models;

/// <summary>
/// Presentation models specifically designed for the Login page.
/// Contains only the data and validation rules needed for each login type.
/// </summary>
public class LoginPageViewModel
{
    public AdminLoginPageModel AdminLogin { get; set; } = new();
    public TenantLoginPageModel TenantLogin { get; set; } = new();
    public WorkerLoginPageModel WorkerLogin { get; set; } = new();
    public string ActiveTab { get; set; } = "admin";
    public string? ReturnUrl { get; set; }

    public void InitializeReturnUrls(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        AdminLogin.ReturnUrl = returnUrl;
        TenantLogin.ReturnUrl = returnUrl;
        WorkerLogin.ReturnUrl = returnUrl;
    }
}

public class AdminLoginPageModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

}

public class TenantLoginPageModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Property code is required")]
    [Display(Name = "Property Code")]
    public string PropertyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit number is required")]
    [Display(Name = "Unit Number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

}


public class WorkerLoginPageModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Specialization is required")]
    [Display(Name = "Specialization")]
    public string Specialization { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public List<string> AvailableSpecializations { get; set; } = new()
    {
        "General Maintenance",
        "Plumbing",
        "Electrical",
        "HVAC",
        "Painting",
        "Carpentry",
        "Locksmith",
        "Appliance Repair"
    };

 
}