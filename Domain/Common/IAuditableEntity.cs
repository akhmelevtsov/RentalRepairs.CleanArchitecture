namespace RentalRepairs.Domain.Common;

/// <summary>
/// Interface for entities that require audit tracking.
/// Provides standardized audit fields across all domain entities.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
