using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RentalRepairs.Application.Common.Behaviors;

/// <summary>
/// Application validation pipeline - handles input format validation only.
/// Business validation is handled by domain entities.
/// Follows Clean Architecture patterns with comprehensive logging and error handling.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        
        // Run all validators in parallel for performance
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );
        
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToArray();

        if (failures.Any())
        {
            _logger.LogWarning(
                "Validation failed for {RequestType} with {FailureCount} failures: {Failures}",
                typeof(TRequest).Name,
                failures.Length,
                string.Join("; ", failures.Select(f => f.ErrorMessage))
            );
            
            throw new Common.Exceptions.ValidationException(failures);
        }

        return await next();
    }
}