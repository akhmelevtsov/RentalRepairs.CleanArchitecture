using MediatR.Pipeline;
using Microsoft.Extensions.Logging;

namespace RentalRepairs.Application.Common.Behaviors;

/// <summary>
/// MediatR behavior that logs requests for debugging and monitoring.
/// Follows Clean Architecture patterns with standardized American English spelling.
/// </summary>
public class LoggingBehavior<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger _logger;

    public LoggingBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Clean Architecture Request: {Name} {@Request}",
            requestName, request);

        await Task.CompletedTask;
    }
}