using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.CreateAndSubmitTenantRequest;

/// <summary>
/// Comprehensive command to create and submit a tenant request in one operation
/// Demonstrates CQRS command pattern replacing service orchestration methods
/// Contains business validation rules that were previously in TenantRequestService
/// </summary>
public class CreateAndSubmitTenantRequestCommand : ICommand<CreateAndSubmitTenantRequestResult>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string UrgencyLevel { get; init; } = "Normal";
    public string? PreferredContactTime { get; init; }
    public string PropertyCode { get; init; } = string.Empty;
    public string UnitNumber { get; init; } = string.Empty;
    public string TenantEmail { get; init; } = string.Empty;
    
    /// <summary>
    /// Business rule parameters
    /// </summary>
    public bool SkipRateLimitValidation { get; init; } = false;
    public bool AllowDuplicateRequests { get; init; } = false;
    public string? SuperintendentOverride { get; init; }
}

/// <summary>
/// Result of create and submit operation
/// </summary>
public class CreateAndSubmitTenantRequestResult
{
    public bool IsSuccess { get; init; }
    public Guid? RequestId { get; init; }
    public string? RequestCode { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public List<string> BusinessRuleWarnings { get; init; } = new();
    
    /// <summary>
    /// Business context
    /// </summary>
    public DateTime? EstimatedResponseTime { get; init; }
    public string? AssignedSuperintendent { get; init; }
    public List<string> NextSteps { get; init; } = new();
}