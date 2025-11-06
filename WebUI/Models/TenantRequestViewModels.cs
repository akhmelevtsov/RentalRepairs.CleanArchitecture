using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.WebUI.Models;

/// <summary>
/// ? Clean presentation model - UI formatting and user experience only
/// Business validation handled by domain entities
/// </summary>
public class SubmitTenantRequestViewModel
{
    // These fields are populated from authentication claims and not directly editable by user
    public string PropertyCode { get; set; } = string.Empty;

    public string UnitNumber { get; set; } = string.Empty;

    public string TenantFirstName { get; set; } = string.Empty;

    public string TenantLastName { get; set; } = string.Empty;

    public string TenantEmail { get; set; } = string.Empty;

    // ? UI-focused validation only - user experience and formatting
    [Display(Name = "Phone Number")] public string TenantPhone { get; set; } = string.Empty;

    [Display(Name = "Problem Description")]
    public string ProblemDescription { get; set; } = string.Empty;

    [Display(Name = "Urgency Level")] public string UrgencyLevel { get; set; } = "Normal";

    [Display(Name = "Is this an emergency?")]
    public bool IsEmergency { get; set; }

    [Display(Name = "Preferred Contact Time")]
    public string? PreferredContactTime { get; set; }

    // ? UI helper properties
    public List<SelectListItem> UrgencyLevelOptions => new()
    {
        new SelectListItem { Value = "Low", Text = "Low Priority" },
        new SelectListItem { Value = "Normal", Text = "Normal Priority", Selected = true },
        new SelectListItem { Value = "High", Text = "High Priority" },
        new SelectListItem { Value = "Critical", Text = "Critical Priority" },
        new SelectListItem { Value = "Emergency", Text = "Emergency" }
    };

    public List<SelectListItem> ContactTimeOptions => new()
    {
        new SelectListItem { Value = "", Text = "Any time" },
        new SelectListItem { Value = "Morning (8AM-12PM)", Text = "Morning (8AM-12PM)" },
        new SelectListItem { Value = "Afternoon (12PM-5PM)", Text = "Afternoon (12PM-5PM)" },
        new SelectListItem { Value = "Evening (5PM-8PM)", Text = "Evening (5PM-8PM)" }
    };
}

/// <summary>
/// ? Presentation model for tenant request details - display only
/// </summary>
public class TenantRequestDetailsViewModel
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsEmergency { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? WorkerName { get; set; }
    public string? WorkerEmail { get; set; }
    public string? CompletionNotes { get; set; }
    public string? PreferredContactTime { get; set; }

    // ? UI helper properties
    public string StatusDisplayClass => Status.ToLowerInvariant() switch
    {
        "draft" => "badge bg-secondary",
        "submitted" => "badge bg-primary",
        "scheduled" => "badge bg-info",
        "done" => "badge bg-success",
        "failed" => "badge bg-warning",
        "declined" => "badge bg-danger",
        "closed" => "badge bg-dark",
        _ => "badge bg-light text-dark"
    };

    public string UrgencyDisplayClass => IsEmergency ? "text-danger fw-bold" : "text-muted";

    public bool CanSchedule => Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase);
    public bool CanComplete => Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase);

    public bool CanClose => Status.Equals("Done", StringComparison.OrdinalIgnoreCase) ||
                            Status.Equals("Failed", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Presentation model for tenant request list
/// </summary>
public class TenantRequestListViewModel
{
    public List<TenantRequestSummaryViewModel> Requests { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? StatusFilter { get; set; }
    public string? PropertyFilter { get; set; }
    public bool EmergencyOnly { get; set; }
    public string SortBy { get; set; } = "SubmittedDate";
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Presentation model for tenant request summary in list
/// </summary>
public class TenantRequestSummaryViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public bool IsEmergency { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Worker assignment information
    public string? AssignedWorkerName { get; set; }
    public string? AssignedWorkerEmail { get; set; }
    public string? WorkOrderNumber { get; set; }
}

/// <summary>
/// ? Presentation model for scheduling - UI validation only
/// </summary>
public class ScheduleServiceWorkViewModel
{
    public Guid RequestId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;

    [Display(Name = "Assigned Worker")] public Guid WorkerId { get; set; }

    [Display(Name = "Scheduled Date")]
    [DataType(DataType.DateTime)]
    public DateTime ScheduledDate { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Service Notes")] public string? ServiceNotes { get; set; }

    public List<WorkerOptionViewModel> AvailableWorkers { get; set; } = new();

    // ? UI helper properties
    public string MinDateForPicker => DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
    public bool HasAvailableWorkers => AvailableWorkers.Any();
}

/// <summary>
/// Presentation model for worker options in dropdowns
/// </summary>
public class WorkerOptionViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

/// <summary>
/// ? Client-side validation helper - user experience only
/// </summary>
public static class TenantRequestClientValidation
{
    /// <summary>
    /// ? Returns validation rules for client-side validation (UX enhancement)
    /// Business validation still enforced server-side by domain entities
    /// </summary>
    public static Dictionary<string, object> GetValidationRules()
    {
        return new Dictionary<string, object>
        {
            ["ProblemDescription"] = new
            {
                required = true,
                minlength = 10,
                maxlength = 1000,
                placeholder = "Please describe the maintenance issue in detail..."
            },
            ["UrgencyLevel"] = new
            {
                required = true
            },
            ["TenantPhone"] = new
            {
                pattern = @"^[\d\s\-\(\)]+$",
                placeholder = "e.g., (555) 123-4567"
            }
        };
    }

    /// <summary>
    /// ? Returns client-side error messages for better UX
    /// </summary>
    public static Dictionary<string, string> GetErrorMessages()
    {
        return new Dictionary<string, string>
        {
            ["ProblemDescription.required"] = "Please describe the maintenance issue",
            ["ProblemDescription.minlength"] = "Please provide more details (at least 10 characters)",
            ["ProblemDescription.maxlength"] = "Description is too long (maximum 1000 characters)",
            ["UrgencyLevel.required"] = "Please select an urgency level",
            ["TenantPhone.pattern"] = "Please enter a valid phone number"
        };
    }
}

// ? Helper class for dropdown lists
public class SelectListItem
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; } = false;
}