using RentalRepairs.Application.Services.TenantRequestDetailsService.Models;

namespace RentalRepairs.WebUI.Models.TenantRequestDetailsViewModels;

/// <summary>
/// View model for tenant request details - optimized for UI display.
/// Uses clean business actions from application layer with UI presentation handled separately.
/// </summary>
public class TenantRequestDetailsViewModel
{
    public TenantRequestDetailsContext Context { get; set; } = new();
    
    // UI-specific properties derived from business context
    public bool CanEdit => Context.CanEdit;
    public bool CanCancel => Context.CanCancel;
    
    // Business action availability (derived from clean business actions)
    public bool CanAssignWorker => Context.HasBusinessAction(TenantRequestActionType.AssignWorker);
    public bool CanComplete => Context.HasBusinessAction(TenantRequestActionType.Complete);
    public bool CanDecline => Context.HasBusinessAction(TenantRequestActionType.Decline);
    public bool CanClose => Context.HasBusinessAction(TenantRequestActionType.Close);
    public bool CanSubmit => Context.HasBusinessAction(TenantRequestActionType.Submit);
    public bool CanReschedule => Context.HasBusinessAction(TenantRequestActionType.Reschedule);
    public bool CanReportIssue => Context.HasBusinessAction(TenantRequestActionType.ReportIssue);
    
    // Helper methods for UI logic
    public bool HasAnyActions => Context.AvailableBusinessActions.Any();
    public bool HasWorkflowActions => Context.GetWorkflowManagementActions().Any();
    public bool HasWorkActions => Context.GetWorkExecutionActions().Any();
    public bool HasTenantActions => Context.GetRequestManagementActions().Any();
    
    // Primary action for UI emphasis
    public TenantRequestBusinessAction? PrimaryAction => Context.GetPrimaryBusinessAction();
    
    // Status-based UI properties using simple logic
    public bool IsActive => Context.Request.Status == Domain.Enums.TenantRequestStatus.Submitted || 
                           Context.Request.Status == Domain.Enums.TenantRequestStatus.Scheduled;
    
    public bool IsCompleted => Context.Request.Status == Domain.Enums.TenantRequestStatus.Done || 
                              Context.Request.Status == Domain.Enums.TenantRequestStatus.Closed;
    
    public bool RequiresAttention => Context.Request.Status == Domain.Enums.TenantRequestStatus.Failed;
}