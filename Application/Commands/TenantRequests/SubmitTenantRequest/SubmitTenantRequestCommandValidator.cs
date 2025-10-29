using FluentValidation;

namespace RentalRepairs.Application.Commands.TenantRequests.SubmitTenantRequest;

/// <summary>
/// Validator for SubmitTenantRequestCommand
/// Ensures basic input validation before reaching the domain layer
///
/// Validates the Guid-based version of SubmitTenantRequestCommand.
/// Any int-based versions are legacy and should not be used.
/// </summary>
public sealed class SubmitTenantRequestCommandValidator : AbstractValidator<SubmitTenantRequestCommand>
{
    public SubmitTenantRequestCommandValidator()
    {
        RuleFor(x => x.TenantRequestId)
            .NotEmpty()
            .WithMessage("Tenant request ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Tenant request ID must be a valid, non-empty GUID")
            .WithName("TenantRequestId");
    }
}