using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// ? Value object for notification data - encapsulates notification business rules
/// </summary>
public record NotificationData
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string RecipientEmail { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;
    
    public static NotificationData CreateTenantSubmissionNotification(TenantRequest request)
    {
        return new NotificationData
        {
            Subject = $"Request Submitted: {request.Title}",
            Body = CreateTenantSubmissionBody(request),
            RecipientEmail = request.TenantEmail,
            Type = NotificationType.TenantRequestSubmitted,
            Priority = request.UrgencyLevel == "Emergency" ? NotificationPriority.High : NotificationPriority.Normal
        };
    }
    
    public static NotificationData CreateSuperintendentNewRequestNotification(TenantRequest request)
    {
        return new NotificationData
        {
            Subject = $"New Request: {request.PropertyName} - Unit {request.TenantUnit}",
            Body = CreateSuperintendentNewRequestBody(request),
            RecipientEmail = request.SuperintendentEmail,
            Type = NotificationType.SuperintendentNewRequest,
            Priority = request.UrgencyLevel == "Emergency" ? NotificationPriority.High : NotificationPriority.Normal
        };
    }
    
    public static NotificationData CreateWorkerAssignmentNotification(TenantRequest request, string workerEmail)
    {
        return new NotificationData
        {
            Subject = $"Work Assignment: {request.PropertyName} - Unit {request.TenantUnit}",
            Body = CreateWorkerAssignmentBody(request),
            RecipientEmail = workerEmail,
            Type = NotificationType.WorkerAssignment,
            Priority = request.UrgencyLevel == "Emergency" ? NotificationPriority.High : NotificationPriority.Normal
        };
    }
    
    private static string CreateTenantSubmissionBody(TenantRequest request)
    {
        return $@"Dear {request.TenantFullName},

Your maintenance request has been submitted for review.

Request Details:
• Code: {request.Code}
• Title: {request.Title}
• Description: {request.Description}
• Urgency: {request.UrgencyLevel}
• Property: {request.PropertyName}
• Unit: {request.TenantUnit}

We will review your request and schedule the necessary work. You will be notified once work is scheduled.

{(request.UrgencyLevel == "Emergency" ? "This is marked as an emergency and will be prioritized." : "")}

Thank you for your patience.

Best regards,
Rental Repairs Team";
    }
    
    private static string CreateSuperintendentNewRequestBody(TenantRequest request)
    {
        return $@"Dear {request.SuperintendentFullName},

A new maintenance request has been submitted and requires your attention.

Request Details:
• Code: {request.Code}
• Title: {request.Title}
• Description: {request.Description}
• Urgency: {request.UrgencyLevel}
• Property: {request.PropertyName}
• Unit: {request.TenantUnit}
• Tenant: {request.TenantFullName} ({request.TenantEmail})
• Submitted: {request.CreatedAt:yyyy-MM-dd HH:mm}

{(request.UrgencyLevel == "Emergency" ? "URGENT: This request requires immediate attention!" : "Please review and schedule the necessary work when convenient.")}

You can access the full request details in the system.

Best regards,
Rental Repairs System";
    }
    
    private static string CreateWorkerAssignmentBody(TenantRequest request)
    {
        return $@"Dear Worker,

You have been assigned to a maintenance request:

Assignment Details:
• Request Code: {request.Code}
• Property: {request.PropertyName}
• Unit: {request.TenantUnit}
• Tenant: {request.TenantFullName}
• Contact: {request.TenantEmail}

Work Details:
• Title: {request.Title}
• Description: {request.Description}
• Urgency: {request.UrgencyLevel}
• Scheduled Date: {request.ScheduledDate?.ToString("yyyy-MM-dd") ?? "TBD"}
• Work Order: {request.WorkOrderNumber}

Please contact the tenant to arrange access and complete the work as scheduled.

{(request.UrgencyLevel == "Emergency" ? "PRIORITY: This is an emergency request!" : "")}

Thank you.

Best regards,
Rental Repairs Scheduling";
    }
}

public enum NotificationType
{
    TenantRequestSubmitted,
    TenantRequestScheduled,
    TenantRequestCompleted,
    SuperintendentNewRequest,
    SuperintendentUrgentRequest,
    WorkerAssignment,
    WorkerCompletionRequired,
    SystemAlert
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}
