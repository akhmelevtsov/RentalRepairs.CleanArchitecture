using FluentValidation;

namespace RentalRepairs.Application.Commands.TenantRequests.ScheduleServiceWork;

/// <summary>
/// Validator for ScheduleServiceWorkCommand.
/// Moved from WorkerService to proper validation layer.
/// </summary>
public class ScheduleServiceWorkCommandValidator : AbstractValidator<ScheduleServiceWorkCommand>
{
    public ScheduleServiceWorkCommandValidator()
    {
        RuleFor(x => x.TenantRequestId)
            .NotEmpty()
            .WithMessage("Request ID is required");

        RuleFor(x => x.WorkerEmail)
            .NotEmpty()
            .WithMessage("Worker email is required")
            .EmailAddress()
            .WithMessage("Worker email must be a valid email address");

        RuleFor(x => x.ScheduledDate)
            .Must(date => date.Date >= DateTime.Today)
            .WithMessage("Scheduled date must be today or in the future");

        RuleFor(x => x.WorkOrderNumber)
            .NotEmpty()
            .WithMessage("Work order number is required")
            .MaximumLength(50)
            .WithMessage("Work order number must not exceed 50 characters");
    }
}