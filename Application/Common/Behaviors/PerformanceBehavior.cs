using MediatR;
using Microsoft.Extensions.Logging;

namespace RentalRepairs.Application.Common.Behaviors;

/// <summary>
/// Performance monitoring behavior for tracking slow requests and failures.
/// Follows Clean Architecture patterns with comprehensive logging and error handling.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 500) // Log slow requests
            {
                _logger.LogWarning(
                    "Slow request detected: {RequestName} took {ElapsedMilliseconds}ms {@Request}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    request);
            }
            else if (stopwatch.ElapsedMilliseconds > 100) // Log moderately slow requests at debug level
            {
                _logger.LogDebug(
                    "Request completed: {RequestName} took {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Request {RequestName} failed after {ElapsedMilliseconds}ms {@Request}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                request);
            
            throw;
        }
    }
}