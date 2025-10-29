using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.Domain.Common;

/// <summary>
/// Enhanced BaseEntity with proper audit support and Guid-based identity.
/// Implements comprehensive audit tracking, soft deletion, and concurrency control.
/// </summary>
public abstract class BaseEntity : IAuditableEntity, ISoftDeletableEntity, IVersionedEntity
{
    #region Private Fields

    private readonly List<BaseEvent> _domainEvents = new();

    #endregion

    #region Constructors

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Properties

    public Guid Id { get; protected set; }
    
    // Audit fields - CreatedBy is now required for data integrity
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Soft delete support - allows logical deletion
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Concurrency control - prevents concurrent update conflicts
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    #endregion

    #region Domain Events

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    #endregion

    #region Business Methods

    /// <summary>
    /// Business method to perform soft deletion.
    /// Marks entity as deleted without removing from database.
    /// </summary>
    /// <param name="deletedBy">The user who performed the deletion.</param>
    public virtual void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Business method to restore soft-deleted entity.
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    #endregion

    #region Equality and Hash Code

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id == other.Id && Id != Guid.Empty;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BaseEntity? left, BaseEntity? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
