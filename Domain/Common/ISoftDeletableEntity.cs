namespace RentalRepairs.Domain.Common;

/// <summary>
/// Interface for entities that support soft deletion.
/// Allows logical deletion without removing data from database.
/// </summary>
public interface ISoftDeletableEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
