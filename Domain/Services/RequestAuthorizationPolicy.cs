using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for request authorization business rules.
/// PHASE 2 MIGRATION: Moves authorization business logic from Application layer to Domain layer.
/// Focuses purely on business rules, not infrastructure concerns like user lookup.
/// </summary>
public class RequestAuthorizationPolicy
{
    /// <summary>
    /// Business rule: Determines if a user role can edit requests in a given status.
    /// Moved from Application layer TenantRequestAuthorizationService.
    /// </summary>
    public bool CanRoleEditRequestInStatus(string userRole, TenantRequestStatus status)
    {
        return userRole switch
        {
            "SystemAdmin" => CanEditInStatus(status),
            "PropertySuperintendent" => CanEditInStatus(status),
            "Tenant" => status == TenantRequestStatus.Draft, // Tenants can only edit drafts
            "Worker" => false, // Workers cannot edit requests
            _ => false
        };
    }

    /// <summary>
    /// Business rule: Determines if a user role can cancel requests in a given status.
    /// Moved from Application layer TenantRequestAuthorizationService.
    /// </summary>
    public bool CanRoleCancelRequestInStatus(string userRole, TenantRequestStatus status)
    {
        if (!CanCancelInStatus(status))
        {
            return false; // Status must allow cancellation first
        }

        return userRole switch
        {
            "SystemAdmin" => true,
            "PropertySuperintendent" => true,
            "Tenant" => true, // Tenants can cancel their own requests
            "Worker" => false,
            _ => false
        };
    }

    /// <summary>
    /// Business rule: Gets available actions for a user role in a given request status.
    /// Moved from Application layer TenantRequestAuthorizationService.
    /// </summary>
    public List<RequestAction> GetAvailableActionsForRole(string userRole, TenantRequestStatus status)
    {
        return userRole switch
        {
            "SystemAdmin" => GetSystemAdminActions(status),
            "PropertySuperintendent" => GetSuperintendentActions(status),
            "Worker" => GetWorkerActions(status),
            "Tenant" => GetTenantActions(status),
            _ => new List<RequestAction>()
        };
    }

    /// <summary>
    /// Business rule: Determines if a role can perform a specific action.
    /// Centralized action permission logic.
    /// </summary>
    public bool CanRolePerformAction(string userRole, RequestAction action, TenantRequestStatus status)
    {
        List<RequestAction> availableActions = GetAvailableActionsForRole(userRole, status);
        return availableActions.Contains(action);
    }

    /// <summary>
    /// Business rule: Determines if a status transition is allowed for a role.
    /// Combines status validation with role permissions.
    /// </summary>
    public bool CanRoleTransitionStatus(string userRole, TenantRequestStatus fromStatus, TenantRequestStatus toStatus)
    {
        // First check if the status transition itself is valid (domain rule)
        var statusPolicy = new TenantRequestStatusPolicy();
        if (!statusPolicy.IsValidStatusTransition(fromStatus, toStatus))
        {
            return false;
        }

        // Then check role permissions for this transition
        RequestAction? requiredAction = GetRequiredActionForTransition(toStatus);
        if (requiredAction == null)
        {
            return false;
        }

        return CanRolePerformAction(userRole, requiredAction.Value, fromStatus);
    }

    /// <summary>
    /// Business rule: Gets the hierarchy of user roles.
    /// Determines role precedence for authorization decisions.
    /// </summary>
    public int GetRolePriority(string userRole)
    {
        return userRole switch
        {
            "SystemAdmin" => 1, // Highest priority
            "PropertySuperintendent" => 2,
            "Worker" => 3,
            "Tenant" => 4, // Lowest priority
            _ => int.MaxValue // Unknown roles have no priority
        };
    }

    /// <summary>
    /// Business rule: Determines if one role has higher privileges than another.
    /// Used for hierarchical authorization decisions.
    /// </summary>
    public bool HasHigherPrivileges(string userRole, string comparedToRole)
    {
        return GetRolePriority(userRole) < GetRolePriority(comparedToRole);
    }

    #region Private Helper Methods

    private static bool CanEditInStatus(TenantRequestStatus status)
    {
        return status is not (TenantRequestStatus.Done or TenantRequestStatus.Closed or TenantRequestStatus.Declined);
    }

    private static bool CanCancelInStatus(TenantRequestStatus status)
    {
        return status is TenantRequestStatus.Draft or TenantRequestStatus.Submitted;
    }

    private List<RequestAction> GetSystemAdminActions(TenantRequestStatus status)
    {
        var actions = new List<RequestAction>();

        if (CanEditInStatus(status))
        {
            actions.Add(RequestAction.Edit);
        }

        if (CanCancelInStatus(status))
        {
            actions.Add(RequestAction.Cancel);
        }

        if (status == TenantRequestStatus.Submitted)
        {
            actions.Add(RequestAction.AssignWorker);
            actions.Add(RequestAction.Decline);
        }

        if (status is TenantRequestStatus.Submitted or TenantRequestStatus.Failed)
        {
            actions.Add(RequestAction.Schedule);
        }

        if (status == TenantRequestStatus.Scheduled)
        {
            actions.Add(RequestAction.Reschedule);
        }

        if (status is TenantRequestStatus.Done or TenantRequestStatus.Declined)
        {
            actions.Add(RequestAction.Close);
        }

        return actions;
    }

    private List<RequestAction> GetSuperintendentActions(TenantRequestStatus status)
    {
        // Superintendents have same privileges as admins for their properties
        return GetSystemAdminActions(status);
    }

    private List<RequestAction> GetWorkerActions(TenantRequestStatus status)
    {
        var actions = new List<RequestAction>();

        if (status == TenantRequestStatus.Scheduled)
        {
            actions.Add(RequestAction.CompleteWork);
            actions.Add(RequestAction.ReportIssue);
        }

        return actions;
    }

    private List<RequestAction> GetTenantActions(TenantRequestStatus status)
    {
        var actions = new List<RequestAction>();

        if (status == TenantRequestStatus.Draft)
        {
            actions.Add(RequestAction.Edit);
            actions.Add(RequestAction.Submit);
            actions.Add(RequestAction.Cancel);
        }

        if (status is TenantRequestStatus.Draft or TenantRequestStatus.Submitted)
        {
            actions.Add(RequestAction.Cancel);
        }

        return actions;
    }

    private RequestAction? GetRequiredActionForTransition(TenantRequestStatus toStatus)
    {
        return toStatus switch
        {
            TenantRequestStatus.Submitted => RequestAction.Submit,
            TenantRequestStatus.Scheduled => RequestAction.AssignWorker,
            TenantRequestStatus.Done => RequestAction.CompleteWork,
            TenantRequestStatus.Declined => RequestAction.Decline,
            TenantRequestStatus.Closed => RequestAction.Close,
            TenantRequestStatus.Failed => RequestAction.ReportIssue,
            _ => null
        };
    }

    #endregion
}

/// <summary>
/// Domain enum for request actions.
/// Represents business actions that can be performed on requests.
/// </summary>
public enum RequestAction
{
    Edit,
    Submit,
    Cancel,
    AssignWorker,
    Schedule,
    Reschedule,
    CompleteWork,
    ReportIssue,
    Decline,
    Close
}
