namespace RentalRepairs.Domain.Services;

/// <summary>
/// Pure domain service for user role business logic.
/// FIXED: Contains only business rules without infrastructure dependencies.
/// Email pattern matching removed - that's an infrastructure/application concern.
/// </summary>
public class UserRoleDomainService
{
    private readonly Dictionary<string, List<string>> _rolePermissions;
    private readonly List<string> _validRoles;

    public UserRoleDomainService()
    {
        // Business rules for valid roles
        _validRoles = new List<string>
        {
            "SystemAdmin",
            "PropertySuperintendent", 
            "Worker",
            "Tenant"
        };

        // Business rules for role permissions
        _rolePermissions = new Dictionary<string, List<string>>
        {
            ["SystemAdmin"] = new List<string>
            {
                "ViewAllRequests", "EditAllRequests", "DeleteRequests", 
                "ManageUsers", "ManageProperties", "ViewReports",
                "DeclineRequests", "AssignWorkers", "ApproveRequests"
            },
            ["PropertySuperintendent"] = new List<string>
            {
                "ViewPropertyRequests", "AssignWorkers", "ApproveRequests",
                "ViewPropertyReports", "ManageProperty", "DeclineRequests"
            },
            ["Worker"] = new List<string>
            {
                "ViewAssignedRequests", "UpdateWorkStatus", "CompleteWork",
                "ViewSchedule"
            },
            ["Tenant"] = new List<string>
            {
                "CreateRequest", "ViewOwnRequests", "UpdateOwnRequests"
            }
        };
    }

    /// <summary>
    /// REMOVED: Email pattern matching is NOT domain business logic.
    /// This belongs in infrastructure/application layers that handle user authentication.
    /// </summary>
    // REMOVED: DetermineRoleFromEmail method - this was wrong architectural choice

    /// <summary>
    /// Pure business logic: Returns permissions for a role.
    /// </summary>
    public List<string> GetPermissionsForRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return new List<string>();
        }

        return _rolePermissions.ContainsKey(role) 
            ? new List<string>(_rolePermissions[role])
            : new List<string>();
    }

    /// <summary>
    /// Pure business logic: Validates if role is valid.
    /// </summary>
    public bool IsValidRole(string role)
    {
        return !string.IsNullOrWhiteSpace(role) && _validRoles.Contains(role);
    }

    /// <summary>
    /// Pure business logic: Determines if one role can assign work to another role.
    /// Business rule for role hierarchy.
    /// </summary>
    public bool CanRoleAssignWork(string assignerRole, string assigneeRole)
    {
        if (!IsValidRole(assignerRole) || !IsValidRole(assigneeRole))
        {
            return false;
        }

        // Business rules for work assignment
        return assignerRole switch
        {
            "SystemAdmin" => true, // Admins can assign to anyone
            "PropertySuperintendent" => assigneeRole == "Worker", // Supers can assign to workers
            _ => false // Workers and tenants cannot assign work
        };
    }

    /// <summary>
    /// Pure business logic: Determines role priority for escalation scenarios.
    /// Higher number = higher priority.
    /// </summary>
    public int GetRolePriority(string role)
    {
        return role switch
        {
            "SystemAdmin" => 100,
            "PropertySuperintendent" => 75,
            "Worker" => 50,
            "Tenant" => 25,
            _ => 0
        };
    }

    /// <summary>
    /// Pure business logic: Determines if role can escalate to another role.
    /// </summary>
    public bool CanEscalateToRole(string fromRole, string toRole)
    {
        int fromPriority = GetRolePriority(fromRole);
        int toPriority = GetRolePriority(toRole);
        
        return toPriority > fromPriority; // Can only escalate to higher priority roles
    }

    /// <summary>
    /// Pure business logic: Gets all valid roles.
    /// </summary>
    public List<string> GetAllValidRoles()
    {
        return new List<string>(_validRoles);
    }

    /// <summary>
    /// ADDED: Business logic - check if a role can perform decline action.
    /// </summary>
    public bool CanRoleDeclineRequests(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        var permissions = GetPermissionsForRole(role);
        return permissions.Contains("DeclineRequests");
    }

    /// <summary>
    /// ADDED: Business logic - check if a role has specific permission.
    /// </summary>
    public bool RoleHasPermission(string role, string permission)
    {
        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(permission))
            return false;

        var permissions = GetPermissionsForRole(role);
        return permissions.Contains(permission);
    }
}
