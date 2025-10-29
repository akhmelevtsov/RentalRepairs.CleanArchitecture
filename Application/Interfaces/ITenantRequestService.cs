using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Interfaces;

/// <summary>
/// Consolidated Tenant Request Service Interface
/// Absorbs functionality from multiple fine-grained services for better cohesion
/// Simple CRUD operations should still use CQRS directly via IMediator.
/// </summary>
public interface ITenantRequestService
{
    /// <summary>
    /// Business logic: Validates if a tenant request workflow state transition is allowed.
    /// Consolidated from ITenantRequestStatusService.
    /// </summary>
    Task<bool> IsWorkflowTransitionAllowedAsync(
        Guid tenantRequestId,
        string fromStatus,
        string toStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Checks if user is authorized to perform action on request.
    /// Consolidated from ITenantRequestAuthorizationService.
    /// </summary>
    Task<bool> IsUserAuthorizedForRequestAsync(
        Guid tenantRequestId,
        string userEmail,
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Gets detailed request information with business context.
    /// Consolidated from ITenantRequestDetailsService.
    /// </summary>
    Task<TenantRequestDetailsDto> GetRequestDetailsWithContextAsync(
        Guid requestId,
        string? userEmail = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Validates and processes tenant request submission with business rules.
    /// Consolidated from ITenantRequestSubmissionService.
    /// </summary>
    Task<TenantRequestSubmissionResult> ValidateAndSubmitRequestAsync(
        SubmitTenantRequestDto request,
        string tenantEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Business logic: Gets tenant requests with role-based filtering.
    /// Consolidated from IRequestManagementService.
    /// </summary>
    Task<List<TenantRequestSummaryDto>> GetRequestsForUserAsync(
        string userEmail,
        string userRole,
        RequestFilterOptions? filters = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supporting DTOs for consolidated service
/// </summary>
public class TenantRequestDetailsDto : TenantRequestDto
{
    public List<string> AvailableActions { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
    public bool CanAssignWorker { get; set; }
    public string? NextAllowedStatus { get; set; }
}

public class TenantRequestSubmissionResult
{
    public bool IsSuccess { get; set; }
    public Guid? RequestId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

public class SubmitTenantRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = "Normal";
    public string? PreferredContactTime { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
}

public class TenantRequestSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string TenantUnit { get; set; } = string.Empty;
    public bool IsEmergency { get; set; }
}

public class RequestFilterOptions
{
    public string? Status { get; set; }
    public string? UrgencyLevel { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? PropertyCode { get; set; }
}