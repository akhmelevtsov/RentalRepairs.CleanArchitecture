using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for authorization business rules.
/// Contains pure business logic without infrastructure dependencies.
/// Follows Domain-Driven Design principles - no external dependencies, only domain concepts.
/// </summary>
public class AuthorizationDomainService
{
    /// <summary>
    /// Determines if a user can access a specific property based on business rules.
    /// Business Rule: System admins can access all properties, superintendents can access their properties,
    /// tenants can access their property only.
    /// </summary>
    public bool CanUserAccessProperty(User user, Property property)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(property);

        // Business Rule: System administrators have full access
        if (user.IsSystemAdmin())
        {
            return true;
        }

        // Business Rule: Property superintendents can access their assigned properties
        if (user.IsPropertySuperintendent() && user.Email.Equals(property.Superintendent.EmailAddress, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Business Rule: Tenants can only access their own property
        if (user.IsTenant() && user.PropertyCode == property.Code)
        {
            return true;
        }

        // Business Rule: Workers cannot directly access property information
        return false;
    }

    /// <summary>
    /// Determines if a user can manage workers based on business rules.
    /// Business Rule: Only system administrators can manage workers.
    /// </summary>
    public bool CanUserManageWorkers(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Business Rule: Only system administrators can manage workers
        return user.IsSystemAdmin();
    }



}
