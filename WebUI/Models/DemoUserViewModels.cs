using System.ComponentModel.DataAnnotations;
using RentalRepairs.Application.Common.Constants;

namespace RentalRepairs.WebUI.Models;

/// <summary>
/// View model for demo user registration
/// </summary>
public class RegisterDemoUserViewModel
{
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a role")]
    [Display(Name = "User Role")]
    public string SelectedRole { get; set; } = string.Empty;

    // Tenant-specific fields
    [Display(Name = "Property Code")] public string? PropertyCode { get; set; }

    [Display(Name = "Property Name")] public string? PropertyName { get; set; }

    [Display(Name = "Unit Number")] public string? UnitNumber { get; set; }

    // Worker-specific fields
    [Display(Name = "Worker Specialization")]
    public string? WorkerSpecialization { get; set; }

    /// <summary>
    /// Get available roles for selection
    /// </summary>
    public static List<RoleOption> GetAvailableRoles()
    {
        return new List<RoleOption>
        {
            new(UserRoles.SystemAdmin, "System Administrator", "Full system access"),
            new(UserRoles.PropertySuperintendent, "Property Superintendent", "Manage properties and tenants"),
            new(UserRoles.Tenant, "Tenant", "Submit and track maintenance requests"),
            new(UserRoles.Worker, "Maintenance Worker", "Complete assigned work orders")
        };
    }

    /// <summary>
    /// Get available worker specializations
    /// </summary>
    public static List<string> GetWorkerSpecializations()
    {
        return new List<string>
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

    /// <summary>
    /// Validate role-specific requirements
    /// </summary>
    public bool IsValidForRole(out string errorMessage)
    {
        errorMessage = string.Empty;

        switch (SelectedRole)
        {
            case UserRoles.Tenant:
                if (string.IsNullOrWhiteSpace(PropertyCode))
                {
                    errorMessage = "Property code is required for tenants";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(UnitNumber))
                {
                    errorMessage = "Unit number is required for tenants";
                    return false;
                }

                break;

            case UserRoles.Worker:
                if (string.IsNullOrWhiteSpace(WorkerSpecialization))
                {
                    errorMessage = "Specialization is required for workers";
                    return false;
                }

                break;

            case UserRoles.PropertySuperintendent:
                if (string.IsNullOrWhiteSpace(PropertyCode))
                {
                    errorMessage = "Property code is required for superintendents";
                    return false;
                }

                break;
        }

        return true;
    }
}

/// <summary>
/// Role option for dropdown selection
/// </summary>
public record RoleOption(string Value, string DisplayName, string Description);

/// <summary>
/// Demo credentials display model
/// </summary>
public class DemoCredentialsViewModel
{
    public List<DemoUserInfo> AvailableUsers { get; set; } = new();
    public bool ShowCredentials { get; set; }
    public string DefaultPassword { get; set; } = string.Empty;

    public List<DemoUserGroup> GroupedUsers => AvailableUsers
        .GroupBy(u => u.Roles.FirstOrDefault() ?? "Unknown")
        .Select(g => new DemoUserGroup
        {
            RoleName = g.Key,
            RoleDisplayName = UserRoles.GetDisplayName(g.Key),
            Users = g.OrderBy(u => u.DisplayName).ToList()
        })
        .OrderBy(g => GetRoleOrder(g.RoleName))
        .ToList();

    private static int GetRoleOrder(string role)
    {
        return role switch
        {
            UserRoles.SystemAdmin => 1,
            UserRoles.PropertySuperintendent => 2,
            UserRoles.Tenant => 3,
            UserRoles.Worker => 4,
            _ => 5
        };
    }
}

/// <summary>
/// Grouped demo users by role
/// </summary>
public class DemoUserGroup
{
    public string RoleName { get; set; } = string.Empty;
    public string RoleDisplayName { get; set; } = string.Empty;
    public List<DemoUserInfo> Users { get; set; } = new();
}

/// <summary>
/// Demo user information for display
/// </summary>
public class DemoUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string? Description { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();
}