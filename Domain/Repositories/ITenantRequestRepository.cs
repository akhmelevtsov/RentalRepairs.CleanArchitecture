using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Specifications;

namespace RentalRepairs.Domain.Repositories;

/// <summary>
/// ? Clean ITenantRequestRepository interface - no ambiguous methods or duplicate signatures
/// Clear contract that implementations can properly fulfill
/// </summary>
public interface ITenantRequestRepository : IRepository<TenantRequest>
{
    #region ? Core Query Methods - Clear Naming

    /// <summary>
    /// Get tenant request by unique code
    /// </summary>
    Task<TenantRequest?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if tenant request exists by code (renamed to avoid ambiguity with base ExistsAsync)
    /// </summary>
    Task<bool> ExistsByCodeAsync(string requestCode, CancellationToken cancellationToken = default);

    #endregion

    #region ? Status-Based Queries - Clear Interface Contract

    /// <summary>
    /// Get all tenant requests with specific status
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetByStatusAsync(TenantRequestStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Count tenant requests by status
    /// </summary>
    Task<int> CountByStatusAsync(TenantRequestStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all pending (submitted) tenant requests
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region ? Entity Relationship Queries - Clear Interface Contract

    /// <summary>
    /// Get all tenant requests for specific tenant
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all tenant requests for specific property
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all tenant requests assigned to specific worker
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetByWorkerEmailAsync(string workerEmail, CancellationToken cancellationToken = default);

    #endregion

    #region ? Business Logic Queries - Clear Interface Contract

    /// <summary>
    /// Get tenant requests by urgency level
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetByUrgencyLevelAsync(string urgencyLevel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get overdue tenant requests older than specified threshold
    /// ? Fixed: Single clear method with explicit parameter - no ambiguous overloads
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetOverdueRequestsAsync(int daysThreshold, CancellationToken cancellationToken = default);

    #endregion

    #region ? Specification Pattern

    /// <summary>
    /// Get tenant requests using specification pattern for complex queries
    /// </summary>
    Task<IEnumerable<TenantRequest>> GetBySpecificationAsync(ISpecification<TenantRequest> specification, CancellationToken cancellationToken = default);

    #endregion

    #region ? Removed: Ambiguous/Duplicate Methods

    // ? Removed GetOverdueRequestsAsync() - ambiguous overload
    // ? Removed methods that duplicate base IRepository<T> functionality
    // ? Removed methods with unclear naming (GetByTenantAsync vs GetByTenantIdAsync)
    // ? Use specification pattern for complex queries instead of many specific methods

    #endregion
}
