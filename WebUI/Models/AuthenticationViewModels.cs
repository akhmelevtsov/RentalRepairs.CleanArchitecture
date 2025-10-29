using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.WebUI.Models;

/// <summary>
/// Unified login view model - all users authenticate with email/password only
/// System determines role and parameters automatically
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Enhanced dashboard view model with role detection
/// </summary>
public class DashboardViewModel
{
    public UserRole UserRole { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    // Property superintendent dashboard
    public int TotalProperties { get; set; }
    public int TotalTenants { get; set; }
    public int PendingRequests { get; set; }
    public int EmergencyRequests { get; set; }
    public List<TenantRequestSummaryViewModel> RecentRequests { get; set; } = new();

    // Tenant dashboard
    public string? PropertyName { get; set; }
    public string? UnitNumber { get; set; }
    public List<TenantRequestSummaryViewModel> MyRequests { get; set; } = new();

    // Worker dashboard
    public int AssignedRequests { get; set; }
    public int CompletedThisMonth { get; set; }
    public List<TenantRequestSummaryViewModel> UpcomingWork { get; set; } = new();

    // System admin dashboard
    public int TotalSystemProperties { get; set; }
    public int TotalSystemRequests { get; set; }
    public int ActiveWorkers { get; set; }
    public List<PropertySummaryViewModel> RecentProperties { get; set; } = new();
    
    // Enhanced System admin dashboard with unit statistics
    public int TotalSystemUnits { get; set; }
    public int TotalOccupiedUnits { get; set; }
    public int TotalVacantUnits { get; set; }
    public double SystemOccupancyRate { get; set; }
    public int ActiveSystemRequests { get; set; }
    public int TotalSystemRequestsAllTime { get; set; }
}

/// <summary>
/// User roles for the presentation layer
/// </summary>
public enum UserRole
{
    SystemAdmin,
    PropertySuperintendent,
    Tenant,
    Worker
}

/// <summary>
/// Navigation menu item
/// </summary>
public class NavigationItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<NavigationItem> SubItems { get; set; } = new();
}

// Legacy view models - kept for backward compatibility during transition
// TODO: Remove these after all references are updated

/// <summary>
/// DEPRECATED: Use unified LoginViewModel instead
/// </summary>
[Obsolete("Use unified LoginViewModel instead")]
public class TenantLoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
    
    // Property for simplified login mode (no longer used)
    public bool UseSimplifiedLogin { get; set; } = true;
}

/// <summary>
/// DEPRECATED: Use unified LoginViewModel instead
/// </summary>
[Obsolete("Use unified LoginViewModel instead")]
public class WorkerLoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }

    public List<string> AvailableSpecializations { get; set; } = new()
    {
        "Plumbing",
        "Electrical", 
        "HVAC",
        "General Maintenance",
        "Carpentry",
        "Painting",
        "Locksmith",
        "Appliance Repair"
    };
}