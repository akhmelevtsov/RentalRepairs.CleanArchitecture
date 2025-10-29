using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.SubmitTenantRequest;

/// <summary>
/// Command to submit a tenant request, transitioning it from draft to submitted status.
/// Used when a tenant or administrator formally submits a request for processing.
/// 
/// NOTE: This uses Guid for TenantRequestId to match the domain entity's Id property.
/// Any legacy versions using int are deprecated.
/// </summary>
public sealed class SubmitTenantRequestCommand : ICommand<SubmitTenantRequestResult>
{
    /// <summary>
    /// The unique identifier of the tenant request to submit.
    /// Must be a valid, non-empty Guid that corresponds to an existing TenantRequest entity.
    /// </summary>
    public required Guid TenantRequestId { get; init; }
}

/// <summary>
/// Result of the submit tenant request operation
/// </summary>
public sealed class SubmitTenantRequestResult
{
    public required bool IsSuccess { get; init; }
    public Guid? RequestId { get; init; }
    public string? Status { get; init; }
    public string? ErrorMessage { get; init; }
}