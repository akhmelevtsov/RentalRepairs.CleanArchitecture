using RentalRepairs.Domain.Enums;

namespace RentalRepairs.WebUI.Helpers;

/// <summary>
/// ? CENTRALIZED UI HELPER: All tenant request UI logic consolidated
/// Replaces duplicate methods in page models with reusable, consistent UI helpers
/// Handles status badges, urgency styling, action buttons, and other presentation logic
/// </summary>
public static class TenantRequestUIHelper
{
    #region Status and Badge Helpers

    /// <summary>
    /// ? CONSOLIDATED: Get Bootstrap badge class for tenant request status
    /// Replaces duplicate GetStatusBadgeClass methods in page models
    /// </summary>
    public static string GetStatusBadgeClass(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Draft => "badge bg-secondary",
            TenantRequestStatus.Submitted => "badge bg-primary",
            TenantRequestStatus.Scheduled => "badge bg-warning text-dark",
            TenantRequestStatus.Done => "badge bg-success",
            TenantRequestStatus.Failed => "badge bg-danger",
            TenantRequestStatus.Declined => "badge bg-dark",
            TenantRequestStatus.Closed => "badge bg-info",
            _ => "badge bg-secondary"
        };
    }

    /// <summary>
    /// ? CONSOLIDATED: Get Bootstrap badge class using status display name (for compatibility)
    /// </summary>
    public static string GetStatusBadgeClass(string statusDisplayName)
    {
        return statusDisplayName switch
        {
            "Draft" => "badge bg-secondary",
            "Submitted" => "badge bg-primary",
            "Scheduled" => "badge bg-warning text-dark",
            "Done" => "badge bg-success",
            "Failed" => "badge bg-danger",
            "Declined" => "badge bg-dark",
            "Closed" => "badge bg-info",
            _ => "badge bg-secondary"
        };
    }

    /// <summary>
    /// ? CONSOLIDATED: Get urgency styling class
    /// Replaces duplicate GetUrgencyClass methods in page models
    /// </summary>
    public static string GetUrgencyClass(bool isEmergency)
    {
        return isEmergency ? "text-danger fw-bold" : "";
    }

    /// <summary>
    /// ? ENHANCED: Get urgency badge class for detailed urgency display
    /// </summary>
    public static string GetUrgencyBadgeClass(string urgencyLevel)
    {
        return urgencyLevel?.ToLowerInvariant() switch
        {
            "low" => "badge bg-light text-dark",
            "normal" => "badge bg-secondary",
            "high" => "badge bg-warning text-dark",
            "critical" or "emergency" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    /// <summary>
    /// ? ENHANCED: Get urgency icon for visual representation
    /// </summary>
    public static string GetUrgencyIcon(string urgencyLevel)
    {
        return urgencyLevel?.ToLowerInvariant() switch
        {
            "low" => "fas fa-circle text-success",
            "normal" => "fas fa-circle text-secondary",
            "high" => "fas fa-exclamation-circle text-warning",
            "critical" or "emergency" => "fas fa-exclamation-triangle text-danger",
            _ => "fas fa-circle text-secondary"
        };
    }

    #endregion

    #region Progress and Timeline Helpers

    /// <summary>
    /// ? NEW: Get progress percentage for tenant request workflow
    /// </summary>
    public static int GetProgressPercentage(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Draft => 10,
            TenantRequestStatus.Submitted => 30,
            TenantRequestStatus.Scheduled => 60,
            TenantRequestStatus.Done => 90,
            TenantRequestStatus.Closed => 100,
            TenantRequestStatus.Declined => 0,
            TenantRequestStatus.Failed => 75,
            _ => 0
        };
    }

    /// <summary>
    /// ? NEW: Get progress bar class for visual workflow representation
    /// </summary>
    public static string GetProgressBarClass(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Done => "progress-bar bg-success",
            TenantRequestStatus.Closed => "progress-bar bg-success",
            TenantRequestStatus.Failed => "progress-bar bg-danger",
            TenantRequestStatus.Declined => "progress-bar bg-dark",
            _ => "progress-bar bg-primary"
        };
    }

    /// <summary>
    /// ? NEW: Get timeline marker class for change history display
    /// </summary>
    public static string GetTimelineMarkerClass(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Draft => "timeline-marker bg-secondary",
            TenantRequestStatus.Submitted => "timeline-marker bg-primary",
            TenantRequestStatus.Scheduled => "timeline-marker bg-warning",
            TenantRequestStatus.Done => "timeline-marker bg-success",
            TenantRequestStatus.Failed => "timeline-marker bg-danger",
            TenantRequestStatus.Declined => "timeline-marker bg-dark",
            TenantRequestStatus.Closed => "timeline-marker bg-info",
            _ => "timeline-marker bg-secondary"
        };
    }

    #endregion

    #region Conditional Display Helpers

    /// <summary>
    /// ? NEW: Check if request should show emergency alert
    /// </summary>
    public static bool ShouldShowEmergencyAlert(bool isEmergency, TenantRequestStatus status)
    {
        return isEmergency &&
               (status == TenantRequestStatus.Draft ||
                status == TenantRequestStatus.Submitted);
    }

    /// <summary>
    /// ? NEW: Check if request should show overdue warning
    /// </summary>
    public static bool ShouldShowOverdueWarning(DateTime? scheduledDate, TenantRequestStatus status)
    {
        if (scheduledDate == null) return false;

        return scheduledDate < DateTime.Now &&
               status != TenantRequestStatus.Done &&
               status != TenantRequestStatus.Closed;
    }

    /// <summary>
    /// ? NEW: Check if request can be edited by current status
    /// </summary>
    public static bool CanEditRequest(TenantRequestStatus status)
    {
        return status == TenantRequestStatus.Draft;
    }

    /// <summary>
    /// ? NEW: Check if work assignment section should be visible
    /// </summary>
    public static bool ShouldShowWorkAssignment(string? assignedWorkerEmail)
    {
        return !string.IsNullOrEmpty(assignedWorkerEmail);
    }

    #endregion

    #region Formatting Helpers

    /// <summary>
    /// ? NEW: Format date for consistent display across the application
    /// </summary>
    public static string FormatDate(DateTime date)
    {
        return date.ToString("MMM dd, yyyy h:mm tt");
    }

    /// <summary>
    /// ? NEW: Format date with null handling
    /// </summary>
    public static string FormatDate(DateTime? date)
    {
        return date?.ToString("MMM dd, yyyy h:mm tt") ?? "Not set";
    }

    /// <summary>
    /// ? NEW: Format relative time (e.g., "2 hours ago")
    /// </summary>
    public static string FormatRelativeTime(DateTime date)
    {
        var timeSpan = DateTime.Now - date;

        return timeSpan switch
        {
            { TotalMinutes: < 1 } => "Just now",
            { TotalMinutes: < 60 } => $"{(int)timeSpan.TotalMinutes} minutes ago",
            { TotalHours: < 24 } => $"{(int)timeSpan.TotalHours} hours ago",
            { TotalDays: < 7 } => $"{(int)timeSpan.TotalDays} days ago",
            { TotalDays: < 30 } => $"{(int)(timeSpan.TotalDays / 7)} weeks ago",
            _ => FormatDate(date)
        };
    }

    /// <summary>
    /// ? NEW: Get user-friendly description for status
    /// </summary>
    public static string GetStatusDescription(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Draft => "Request is being prepared",
            TenantRequestStatus.Submitted => "Request has been submitted for review",
            TenantRequestStatus.Scheduled => "Work has been scheduled",
            TenantRequestStatus.Done => "Work has been completed",
            TenantRequestStatus.Failed => "Work could not be completed",
            TenantRequestStatus.Declined => "Request was declined",
            TenantRequestStatus.Closed => "Request has been closed",
            _ => "Status unknown"
        };
    }

    #endregion

    #region Alert and Notification Helpers

    /// <summary>
    /// ? NEW: Get alert class for status-based notifications
    /// </summary>
    public static string GetStatusAlertClass(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Done => "alert alert-success",
            TenantRequestStatus.Failed => "alert alert-danger",
            TenantRequestStatus.Declined => "alert alert-warning",
            TenantRequestStatus.Submitted when DateTime.Now.Hour >= 17 => "alert alert-info", // After hours
            _ => ""
        };
    }

    /// <summary>
    /// ? NEW: Get alert message for status changes
    /// </summary>
    public static string GetStatusAlertMessage(TenantRequestStatus status)
    {
        return status switch
        {
            TenantRequestStatus.Done => "Work has been completed successfully! Please review the completion notes.",
            TenantRequestStatus.Failed =>
                "Unfortunately, the work could not be completed. Please check the notes for details.",
            TenantRequestStatus.Declined =>
                "This request has been declined. Contact your property manager for more information.",
            _ => ""
        };
    }

    #endregion

    #region Action Button Helpers

    /// <summary>
    /// ? NEW: Get available quick actions for status with role-based filtering
    /// Returns simple action names for basic operations (not full UI mapping)
    /// </summary>
    public static List<string> GetQuickActions(TenantRequestStatus status, string? userRole = null)
    {
        var baseActions = status switch
        {
            TenantRequestStatus.Draft => new List<string> { "Submit", "Edit", "Delete" },
            TenantRequestStatus.Submitted => new List<string> { "Assign Worker", "Decline" },
            TenantRequestStatus.Scheduled => new List<string> { "Complete", "Reschedule", "Report Issue" },
            TenantRequestStatus.Done => new List<string> { "Close", "Reopen" },
            TenantRequestStatus.Failed => new List<string>
                { "Reschedule", "Close" }, // Allow rescheduling of failed work
            _ => new List<string>()
        };

        // Filter actions based on user role if provided
        if (!string.IsNullOrEmpty(userRole)) return FilterActionsByRole(baseActions, userRole);

        return baseActions;
    }

    /// <summary>
    /// Filter actions based on user role permissions
    /// </summary>
    private static List<string> FilterActionsByRole(List<string> actions, string userRole)
    {
        var filteredActions = new List<string>();

        foreach (var action in actions)
        {
            var canPerformAction = action switch
            {
                "Submit" => userRole == "Tenant" || userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                "Edit" => userRole == "Tenant" || userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                "Delete" => userRole == "Tenant" || userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                "Assign Worker" => userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                "Decline" => userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                "Complete" => userRole == "Worker" || userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                "Reschedule" => userRole == "PropertySuperintendent" ||
                                userRole == "SystemAdmin", // Only superintendents and admins can reschedule
                "Report Issue" => true, // Anyone can report issues
                "Close" => userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                "Reopen" => userRole == "PropertySuperintendent" || userRole == "SystemAdmin",
                _ => false
            };

            if (canPerformAction) filteredActions.Add(action);
        }

        return filteredActions;
    }

    /// <summary>
    /// ? NEW: Get button class for specific actions
    /// </summary>
    public static string GetActionButtonClass(string actionName)
    {
        return actionName switch
        {
            "Submit" => "btn btn-primary",
            "Edit" => "btn btn-outline-primary",
            "Delete" => "btn btn-outline-danger",
            "Assign Worker" => "btn btn-primary",
            "Decline" => "btn btn-outline-danger",
            "Complete" => "btn btn-success",
            "Reschedule" => "btn btn-warning",
            "Report Issue" => "btn btn-outline-warning",
            "Close" => "btn btn-success",
            "Reopen" => "btn btn-outline-primary",
            _ => "btn btn-secondary"
        };
    }

    #endregion
}