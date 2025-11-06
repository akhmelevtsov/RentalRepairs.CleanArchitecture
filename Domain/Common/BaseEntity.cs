using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.Domain.Common;

/// <summary>
/// Enhanced BaseEntity with proper audit support and Guid-based identity.
/// Implements comprehensive audit tracking, soft deletion, and concurrency control.
/// Audit fields use explicit interface implementation to protect domain integrity
/// while allowing infrastructure layer access.
/// </summary>
public abstract class BaseEntity : IAuditableEntity, ISoftDeletableEntity, IVersionedEntity
{
    #region Private Fields

    private readonly List<BaseEvent> _domainEvents = new();
    private DateTime _createdAt;
    private string _createdBy = string.Empty;
    private DateTime? _updatedAt;
    private string? _updatedBy;
    private bool _isDeleted;
    private DateTime? _deletedAt;
    private string? _deletedBy;
    private byte[] _rowVersion = Array.Empty<byte>();

    #endregion

    #region Constructors

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        _createdAt = DateTime.UtcNow;
        _updatedAt = DateTime.UtcNow;
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
        _createdAt = DateTime.UtcNow;
        _updatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Entity identifier - immutable after construction.
    /// Uses init-only setter to prevent modification after initialization.
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Audit fields - exposed as read-only properties for domain code.
    /// Infrastructure layer can set via explicit interface implementation.
    /// </summary>
    public DateTime CreatedAt => _createdAt;

    public string CreatedBy => _createdBy;
    public DateTime? UpdatedAt => _updatedAt;
    public string? UpdatedBy => _updatedBy;

    /// <summary>
    /// Soft delete support - exposed as read-only for domain queries.
    /// Infrastructure layer can set via explicit interface implementation.
    /// </summary>
    public bool IsDeleted => _isDeleted;

    public DateTime? DeletedAt => _deletedAt;
    public string? DeletedBy => _deletedBy;

    /// <summary>
    /// Concurrency control - managed by infrastructure.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion => _rowVersion;

    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    #endregion

    #region Explicit Interface Implementation - Infrastructure Access

    /// <summary>
    /// Explicit interface implementations allow EF Core and infrastructure
    /// to set audit properties while keeping them read-only for domain code.
    /// </summary>
    DateTime IAuditableEntity.CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value;
    }

    string IAuditableEntity.CreatedBy
    {
        get => _createdBy;
        set => _createdBy = value ?? string.Empty;
    }

    DateTime? IAuditableEntity.UpdatedAt
    {
        get => _updatedAt;
        set => _updatedAt = value;
    }

    string? IAuditableEntity.UpdatedBy
    {
        get => _updatedBy;
        set => _updatedBy = value;
    }

    bool ISoftDeletableEntity.IsDeleted
    {
        get => _isDeleted;
        set => _isDeleted = value;
    }

    DateTime? ISoftDeletableEntity.DeletedAt
    {
        get => _deletedAt;
        set => _deletedAt = value;
    }

    string? ISoftDeletableEntity.DeletedBy
    {
        get => _deletedBy;
        set => _deletedBy = value;
    }

    byte[] IVersionedEntity.RowVersion
    {
        get => _rowVersion;
        set => _rowVersion = value ?? Array.Empty<byte>();
    }

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
    /// Domain code can call this to trigger soft delete behavior.
    /// </summary>
    /// <param name="deletedBy">The user who performed the deletion.</param>
    public virtual void SoftDelete(string deletedBy)
    {
        _isDeleted = true;
        _deletedAt = DateTime.UtcNow;
        _deletedBy = deletedBy;
    }

    /// <summary>
    /// Business method to restore soft-deleted entity.
    /// </summary>
    public virtual void Restore()
    {
        _isDeleted = false;
        _deletedAt = null;
        _deletedBy = null;
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
