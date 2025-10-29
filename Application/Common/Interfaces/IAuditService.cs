using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// ? Enhanced audit service interface for comprehensive audit trail management
/// Handles audit information application and audit trail querying
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Apply audit information to entity entries before saving to database
    /// </summary>
    void ApplyAuditInformation(IEnumerable<EntityEntry> entries, string? currentUser = null);

    /// <summary>
    /// Get detailed audit trail for a specific entity
    /// </summary>
    Task<List<AuditEntry>> GetAuditTrailAsync(Type entityType, int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit summary for reporting purposes
    /// </summary>
    Task<AuditSummary> GetAuditSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// ? Audit entry model for tracking individual changes
/// </summary>
public class AuditEntry
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Operation { get; set; } = string.Empty; // Created, Updated, Deleted
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public Dictionary<string, object?> OldValues { get; set; } = new();
    public Dictionary<string, object?> NewValues { get; set; } = new();
    public List<string> ChangedProperties { get; set; } = new();
}

/// <summary>
/// ? Audit summary for reporting and analytics
/// </summary>
public class AuditSummary
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOperations { get; set; }
    public int CreateOperations { get; set; }
    public int UpdateOperations { get; set; }
    public int DeleteOperations { get; set; }
    public Dictionary<string, int> OperationsByEntity { get; set; } = new();
    public Dictionary<string, int> OperationsByUser { get; set; } = new();
}