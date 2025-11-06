using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Entities;
using System.Reflection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace RentalRepairs.Infrastructure.Persistence;

/// <summary>
/// ? PUBLIC FOR INFRASTRUCTURE USE: Enhanced ApplicationDbContext
/// WARNING: This is public for Infrastructure layer access only
/// WebUI should NEVER directly use this class - use IApplicationDbContext interface instead
/// All Infrastructure repositories and services need access to this for proper functioning
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IAuditService? _auditService;
    private readonly IDomainEventPublisher? _eventPublisher;
    private readonly ILogger<ApplicationDbContext> _logger;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ILogger<ApplicationDbContext> logger,
        IAuditService? auditService = null,
        IDomainEventPublisher? eventPublisher = null) : base(options)
    {
        _auditService = auditService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    // DbSets for Domain Entities - All using Guid IDs
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantRequest> TenantRequests => Set<TenantRequest>();
    public DbSet<Worker> Workers => Set<Worker>();

    /// <summary>
    /// ? Enhanced SaveChangesAsync with comprehensive audit and event handling
    /// Provides transaction safety and proper error handling
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Apply auditing before saving (within transaction boundary)
            await ApplyAuditingAsync(cancellationToken);

            // 2. Save changes within transaction
            var result = await base.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully saved {ChangeCount} changes to database", result);

            // 3. Publish domain events after successful save
            await PublishDomainEventsAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during SaveChangesAsync operation");
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Apply entity configurations
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure audit fields globally
        ConfigureAuditFields(builder);

        // Configure soft delete filters globally
        ConfigureSoftDeleteFilter(builder);

        // Configure concurrency tokens
        ConfigureConcurrencyTokens(builder);

        base.OnModelCreating(builder);
    }

    #region Private Methods

    private async Task ApplyAuditingAsync(CancellationToken cancellationToken)
    {
        if (_auditService == null)
        {
            _logger.LogDebug("No audit service available, using basic auditing");
            ApplyBasicAuditing();
            return;
        }

        var auditableEntries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted)
            .ToList();

        if (auditableEntries.Any())
        {
            _logger.LogDebug("Applying audit information to {EntryCount} entities", auditableEntries.Count);
            _auditService.ApplyAuditInformation(auditableEntries);
        }

        await Task.CompletedTask;
    }

    private void ApplyBasicAuditing()
    {
        var auditTime = DateTime.UtcNow;
        const string systemUser = "system";

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is IAuditableEntity addedEntity)
                    {
                        addedEntity.CreatedAt = auditTime;
                        addedEntity.CreatedBy = systemUser;
                    }

                    break;

                case EntityState.Modified:
                    if (entry.Entity is IAuditableEntity modifiedEntity)
                    {
                        // Prevent modification of creation audit fields
                        entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;

                        modifiedEntity.UpdatedAt = auditTime;
                        modifiedEntity.UpdatedBy = systemUser;
                    }

                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDeletableEntity softDeletable)
                    {
                        // Convert to soft delete
                        entry.State = EntityState.Modified;
                        softDeletable.IsDeleted = true;
                        softDeletable.DeletedAt = auditTime;
                        softDeletable.DeletedBy = systemUser;
                    }

                    break;
            }
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (_eventPublisher == null)
        {
            _logger.LogDebug("No domain event publisher available, skipping event dispatch");
            return;
        }

        try
        {
            var pendingEventCount = _eventPublisher.GetPendingEventCount(this);
            if (pendingEventCount > 0)
            {
                _logger.LogDebug("Publishing {EventCount} domain events", pendingEventCount);
                await _eventPublisher.PublishEventsAsync(this, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain events");
            throw;
        }
    }

    #endregion

    #region Model Configuration Methods

    private static void ConfigureAuditFields(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Configure creation audit fields
                builder.Entity(entityType.ClrType)
                    .Property<DateTime>(nameof(IAuditableEntity.CreatedAt))
                    .HasDefaultValueSql("GETUTCDATE()")
                    .IsRequired();

                builder.Entity(entityType.ClrType)
                    .Property<string>(nameof(IAuditableEntity.CreatedBy))
                    .HasMaxLength(256)
                    .IsRequired();

                // Configure modification audit fields
                builder.Entity(entityType.ClrType)
                    .Property<DateTime?>(nameof(IAuditableEntity.UpdatedAt));

                builder.Entity(entityType.ClrType)
                    .Property<string>(nameof(IAuditableEntity.UpdatedBy))
                    .HasMaxLength(256);

                // Add indexes for audit queries
                builder.Entity(entityType.ClrType)
                    .HasIndex(nameof(IAuditableEntity.CreatedAt))
                    .HasDatabaseName($"IX_{entityType.ClrType.Name}_CreatedAt");

                builder.Entity(entityType.ClrType)
                    .HasIndex(nameof(IAuditableEntity.CreatedBy))
                    .HasDatabaseName($"IX_{entityType.ClrType.Name}_CreatedBy");
            }

            if (typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Configure soft delete fields
                builder.Entity(entityType.ClrType)
                    .Property<bool>(nameof(ISoftDeletableEntity.IsDeleted))
                    .HasDefaultValue(false)
                    .IsRequired();

                builder.Entity(entityType.ClrType)
                    .Property<DateTime?>(nameof(ISoftDeletableEntity.DeletedAt));

                builder.Entity(entityType.ClrType)
                    .Property<string>(nameof(ISoftDeletableEntity.DeletedBy))
                    .HasMaxLength(256);

                // Add index for soft delete queries
                builder.Entity(entityType.ClrType)
                    .HasIndex(nameof(ISoftDeletableEntity.IsDeleted))
                    .HasDatabaseName($"IX_{entityType.ClrType.Name}_IsDeleted");
            }
        }
    }

    private static void ConfigureSoftDeleteFilter(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
            if (typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Configure global query filter for soft deleted entities
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(GetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                var filter = method.Invoke(null, Array.Empty<object>());
                entityType.SetQueryFilter((LambdaExpression)filter!);
            }
    }

    private static void ConfigureConcurrencyTokens(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
            if (typeof(IVersionedEntity).IsAssignableFrom(entityType.ClrType))
                builder.Entity(entityType.ClrType)
                    .Property<byte[]>(nameof(IVersionedEntity.RowVersion))
                    .IsRowVersion()
                    .IsConcurrencyToken();
    }

    private static LambdaExpression GetSoftDeleteFilter<TEntity>()
        where TEntity : class, ISoftDeletableEntity
    {
        Expression<Func<TEntity, bool>> filter = x => !x.IsDeleted;
        return filter;
    }

    #endregion
}