using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// Audit service interface for applying audit information to entities.
/// Simplified to only include actively used functionality.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Apply audit information to entity entries before saving to database.
    /// Sets CreatedBy/CreatedAt for new entities, UpdatedBy/UpdatedAt for modified entities,
    /// and handles soft deletion by setting DeletedBy/DeletedAt.
    /// </summary>
    void ApplyAuditInformation(IEnumerable<EntityEntry> entries, string? currentUser = null);
}