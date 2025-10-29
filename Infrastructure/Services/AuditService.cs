using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Common;

namespace RentalRepairs.Infrastructure.Services;

/// <summary>
/// ? INTERNAL: Enhanced audit service implementation - WebUI cannot access this directly
/// Handles audit information application and provides audit trail functionality
/// WebUI accesses through Application.Common.Interfaces.IAuditService
/// </summary>
internal class AuditService : IAuditService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ICurrentUserService currentUserService, IDateTime dateTime, ILogger<AuditService> logger)
    {
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <summary>
    /// ? Apply comprehensive audit information to all auditable entities
    /// Handles creation, modification, and soft deletion audit trails
    /// </summary>
    public void ApplyAuditInformation(IEnumerable<EntityEntry> entries, string? currentUser = null)
    {
        var auditUser = currentUser ?? _currentUserService.UserId ?? "system";
        var auditTime = _dateTime.UtcNow;
        var entriesList = entries.ToList();

        _logger.LogDebug("Applying audit information for {EntryCount} entities by user {User}", 
            entriesList.Count, auditUser);

        foreach (var entry in entriesList)
        {
            try
            {
                ApplyAuditToEntry(entry, auditUser, auditTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply audit information to entity {EntityType} with ID {EntityId}",
                    entry.Entity.GetType().Name, GetEntityId(entry));
                throw;
            }
        }

        _logger.LogDebug("Successfully applied audit information to {EntryCount} entities", entriesList.Count);
    }

    /// <summary>
    /// ? Get comprehensive audit trail for specific entity
    /// Returns detailed change history with old/new values
    /// </summary>
    public async Task<List<AuditEntry>> GetAuditTrailAsync(Type entityType, int entityId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving audit trail for {EntityType} with ID {EntityId}", entityType.Name, entityId);
        
        // This would typically query an AuditLog table
        // For now, return empty list as audit log table needs to be implemented
        await Task.CompletedTask;
        return new List<AuditEntry>();
    }

    /// <summary>
    /// ? Get audit summary for reporting and analytics
    /// </summary>
    public async Task<AuditSummary> GetAuditSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating audit summary from {FromDate} to {ToDate}", fromDate, toDate);
        
        // This would typically aggregate from AuditLog table
        // For now, return empty summary as audit log table needs to be implemented
        await Task.CompletedTask;
        
        return new AuditSummary
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalOperations = 0,
            CreateOperations = 0,
            UpdateOperations = 0,
            DeleteOperations = 0
        };
    }

    #region Private Methods

    private void ApplyAuditToEntry(EntityEntry entry, string auditUser, DateTime auditTime)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                ApplyCreationAudit(entry, auditUser, auditTime);
                break;

            case EntityState.Modified:
                ApplyModificationAudit(entry, auditUser, auditTime);
                break;

            case EntityState.Deleted:
                ApplySoftDeletion(entry, auditUser, auditTime);
                break;
        }
    }

    private void ApplyCreationAudit(EntityEntry entry, string auditUser, DateTime auditTime)
    {
        if (entry.Entity is not IAuditableEntity auditableEntity) return;

        auditableEntity.CreatedAt = auditTime;
        auditableEntity.CreatedBy = auditUser;

        _logger.LogTrace("Applied creation audit to {EntityType}: CreatedBy={User}, CreatedAt={Time}",
            entry.Entity.GetType().Name, auditUser, auditTime);
    }

    private void ApplyModificationAudit(EntityEntry entry, string auditUser, DateTime auditTime)
    {
        if (entry.Entity is not IAuditableEntity auditableEntity) return;

        // ? Prevent modification of creation audit fields
        if (entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified)
            entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
            
        if (entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified)
            entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;

        // Update modification audit fields
        auditableEntity.UpdatedAt = auditTime;
        auditableEntity.UpdatedBy = auditUser;

        _logger.LogTrace("Applied modification audit to {EntityType}: UpdatedBy={User}, UpdatedAt={Time}",
            entry.Entity.GetType().Name, auditUser, auditTime);
    }

    private void ApplySoftDeletion(EntityEntry entry, string auditUser, DateTime auditTime)
    {
        if (entry.Entity is not ISoftDeletableEntity softDeletable) return;

        // ? Convert hard delete to soft delete
        entry.State = EntityState.Modified;
        
        softDeletable.IsDeleted = true;
        softDeletable.DeletedAt = auditTime;
        softDeletable.DeletedBy = auditUser;

        // Also update modification audit fields if entity is auditable
        if (entry.Entity is IAuditableEntity auditableEntity)
        {
            auditableEntity.UpdatedAt = auditTime;
            auditableEntity.UpdatedBy = auditUser;
        }

        _logger.LogTrace("Applied soft deletion audit to {EntityType}: DeletedBy={User}, DeletedAt={Time}",
            entry.Entity.GetType().Name, auditUser, auditTime);
    }

    private static object? GetEntityId(EntityEntry entry)
    {
        return entry.Property("Id")?.CurrentValue;
    }

    #endregion
}