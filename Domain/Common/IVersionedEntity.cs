namespace RentalRepairs.Domain.Common;

/// <summary>
/// Interface for entities that require concurrency control.
/// Provides optimistic concurrency control through row versioning.
/// </summary>
public interface IVersionedEntity
{
    byte[] RowVersion { get; set; }
}
