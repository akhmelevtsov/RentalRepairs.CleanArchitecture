using FluentValidation;
using RentalRepairs.Application.Commands.TenantRequests;
using RentalRepairs.Application.Commands.TenantRequests.ReportWorkCompleted;
using RentalRepairs.Application.Commands.TenantRequests.ScheduleServiceWork;

namespace RentalRepairs.Application.Validators.TenantRequests;

/// <summary>
/// ? Consolidated validator for TenantRequest commands with proper Guid validation
/// </summary>
public class CreateTenantRequestCommandValidator : AbstractValidator<CreateTenantRequestCommand>
{
    public CreateTenantRequestCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty)
            .WithMessage("TenantId must be a valid Guid");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.UrgencyLevel)
            .NotEmpty()
            .WithMessage("UrgencyLevel is required")
            .Must(BeValidUrgencyLevel)
            .WithMessage("UrgencyLevel must be one of: Low, Normal, High, Critical, Emergency");

        RuleFor(x => x.PropertyId)
            .NotEqual(Guid.Empty)
            .WithMessage("PropertyId must be a valid Guid");

        RuleFor(x => x.TenantEmail)
            .NotEmpty()
            .WithMessage("TenantEmail is required")
            .EmailAddress()
            .WithMessage("TenantEmail must be a valid email address");
    }

    private static bool BeValidUrgencyLevel(string urgencyLevel)
    {
        var validLevels = new[] { "Low", "Normal", "High", "Critical", "Emergency" };
        return validLevels.Contains(urgencyLevel);
    }
}

/// <summary>
/// ? Validator for scheduling service work
/// </summary>
public class ScheduleServiceWorkCommandValidator : AbstractValidator<ScheduleServiceWorkCommand>
{
    public ScheduleServiceWorkCommandValidator()
    {
        RuleFor(x => x.TenantRequestId)
            .NotEqual(Guid.Empty)
            .WithMessage("TenantRequestId must be a valid Guid");

        RuleFor(x => x.ScheduledDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("ScheduledDate must be in the future");

        RuleFor(x => x.WorkerEmail)
            .NotEmpty()
            .WithMessage("WorkerEmail is required")
            .EmailAddress()
            .WithMessage("WorkerEmail must be a valid email address");

        RuleFor(x => x.WorkOrderNumber)
            .NotEmpty()
            .WithMessage("WorkOrderNumber is required")
            .MaximumLength(50)
            .WithMessage("WorkOrderNumber cannot exceed 50 characters");
    }
}

/// <summary>
/// ? Validator for reporting work completion
/// </summary>
public class ReportWorkCompletedCommandValidator : AbstractValidator<ReportWorkCompletedCommand>
{
    public ReportWorkCompletedCommandValidator()
    {
        RuleFor(x => x.TenantRequestId)
            .NotEqual(Guid.Empty)
            .WithMessage("TenantRequestId must be a valid Guid");

        RuleFor(x => x.CompletionNotes)
            .MaximumLength(500)
            .WithMessage("CompletionNotes cannot exceed 500 characters");
    }
}