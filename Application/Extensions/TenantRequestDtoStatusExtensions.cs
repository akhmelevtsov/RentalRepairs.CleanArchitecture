using RentalRepairs.Application.DTOs;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Application.Extensions;

/// <summary>
/// Extension methods for TenantRequestDto to provide strongly-typed status operations.
/// Uses domain services directly for business logic, eliminating string-based status comparisons.
/// </summary>
public static class TenantRequestDtoStatusExtensions
{
    // Static instance of domain policy for extension methods
    // In production, consider using a service locator pattern or dependency injection for better testability
    private static readonly TenantRequestStatusPolicy DefaultStatusPolicy = new();

    /// <summary>
    /// Gets the strongly-typed status from the DTO string status.
    /// </summary>
    public static TenantRequestStatus GetTypedStatus(this TenantRequestDto dto)
    {
        return Enum.TryParse<TenantRequestStatus>(dto.Status, true, out var status)
            ? status
            : TenantRequestStatus.Draft;
    }

    /// <summary>
    /// Determines if the request can be edited based on its current status.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanBeEdited(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.CanEditInStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if the request can be cancelled based on its current status.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanBeCancelled(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.CanCancelInStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if the request is currently active.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool IsActive(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.IsActiveStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if the request is completed.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool IsCompleted(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.IsCompletedStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if the request is in a final state.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool IsFinal(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.IsFinalStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if the request requires attention.
    /// Enhanced with domain service business logic.
    /// </summary>
    public static bool RequiresAttention(this TenantRequestDto dto)
    {
        var status = dto.GetTypedStatus();
        return DefaultStatusPolicy.RequiresAttention(status) ||
               (status == TenantRequestStatus.Submitted && dto.CreatedDate <= DateTime.UtcNow.AddDays(-2));
    }

    /// <summary>
    /// Gets the category of the request status.
    /// Uses domain service for business logic.
    /// </summary>
    public static StatusCategory GetStatusCategory(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.GetStatusCategory(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if work can be assigned to this request.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanAssignWorker(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.CanAssignWorkerInStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if work can be scheduled for this request.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanScheduleWork(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.CanScheduleWorkInStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if work can be completed for this request.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanCompleteWork(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.CanCompleteWorkInStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if the request can be declined.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanBeDeclined(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.CanDeclineInStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Determines if the request can be closed.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanBeClosed(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.CanCloseInStatus(dto.GetTypedStatus());
    }

    /// <summary>
    /// Validates if a status transition is allowed for this request.
    /// Uses domain service for business logic.
    /// </summary>
    public static bool CanTransitionTo(this TenantRequestDto dto, TenantRequestStatus targetStatus)
    {
        return DefaultStatusPolicy.IsValidStatusTransition(dto.GetTypedStatus(), targetStatus);
    }

    /// <summary>
    /// Gets all allowed next statuses for this request.
    /// Uses domain service for business logic.
    /// </summary>
    public static List<TenantRequestStatus> GetAllowedNextStatuses(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.GetAllowedNextStatuses(dto.GetTypedStatus());
    }

    /// <summary>
    /// Gets the priority of this request based on its status.
    /// Uses domain service for business logic.
    /// </summary>
    public static int GetStatusPriority(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.GetStatusPriority(dto.GetTypedStatus());
    }

    /// <summary>
    /// Gets user-friendly display name for the status.
    /// Uses domain service for business logic.
    /// </summary>
    public static string GetStatusDisplayName(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.GetStatusDisplayName(dto.GetTypedStatus());
    }

    /// <summary>
    /// Gets CSS class for status display styling.
    /// Uses domain service for business logic.
    /// </summary>
    public static string GetStatusCssClass(this TenantRequestDto dto)
    {
        return DefaultStatusPolicy.GetStatusCssClass(dto.GetTypedStatus());
    }
}

/// <summary>
/// Extension methods for collections of TenantRequestDto.
/// Enhanced with domain service business logic for strongly-typed filtering and grouping operations.
/// </summary>
public static class TenantRequestDtoCollectionExtensions
{
    // Static instance for collection operations
    private static readonly TenantRequestStatusPolicy DefaultStatusPolicy = new();

    /// <summary>
    /// Filters requests by status category.
    /// Uses domain service for categorization.
    /// </summary>
    public static IEnumerable<TenantRequestDto> ByStatusCategory(this IEnumerable<TenantRequestDto> requests,
        StatusCategory category)
    {
        return requests.Where(r => r.GetStatusCategory() == category);
    }

    /// <summary>
    /// Filters requests by strongly-typed status.
    /// </summary>
    public static IEnumerable<TenantRequestDto> ByStatus(this IEnumerable<TenantRequestDto> requests,
        TenantRequestStatus status)
    {
        return requests.Where(r => r.GetTypedStatus() == status);
    }

    /// <summary>
    /// Filters requests by multiple strongly-typed statuses.
    /// </summary>
    public static IEnumerable<TenantRequestDto> ByStatuses(this IEnumerable<TenantRequestDto> requests,
        params TenantRequestStatus[] statuses)
    {
        var statusSet = new HashSet<TenantRequestStatus>(statuses);
        return requests.Where(r => statusSet.Contains(r.GetTypedStatus()));
    }

    /// <summary>
    /// Gets only active requests (submitted or scheduled).
    /// Uses domain service for business logic.
    /// </summary>
    public static IEnumerable<TenantRequestDto> ActiveRequests(this IEnumerable<TenantRequestDto> requests)
    {
        return requests.Where(r => r.IsActive());
    }

    /// <summary>
    /// Gets only completed requests (done or closed).
    /// Uses domain service for business logic.
    /// </summary>
    public static IEnumerable<TenantRequestDto> CompletedRequests(this IEnumerable<TenantRequestDto> requests)
    {
        return requests.Where(r => r.IsCompleted());
    }

    /// <summary>
    /// Gets requests that require attention (failed or old submitted).
    /// Uses domain service for business logic.
    /// </summary>
    public static IEnumerable<TenantRequestDto> RequiringAttention(this IEnumerable<TenantRequestDto> requests)
    {
        return requests.Where(r => r.RequiresAttention());
    }

    /// <summary>
    /// Groups requests by status category.
    /// Uses domain service for categorization.
    /// </summary>
    public static Dictionary<StatusCategory, List<TenantRequestDto>> GroupByStatusCategory(
        this IEnumerable<TenantRequestDto> requests)
    {
        return requests.GroupBy(r => r.GetStatusCategory())
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Groups requests by strongly-typed status.
    /// </summary>
    public static Dictionary<TenantRequestStatus, List<TenantRequestDto>> GroupByStatus(
        this IEnumerable<TenantRequestDto> requests)
    {
        return requests.GroupBy(r => r.GetTypedStatus())
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Sorts requests by status priority (most urgent first).
    /// Uses domain service for priority calculation.
    /// </summary>
    public static IEnumerable<TenantRequestDto> OrderByStatusPriority(this IEnumerable<TenantRequestDto> requests)
    {
        return requests.OrderBy(r => r.GetStatusPriority());
    }
}