using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RentalRepairs.CompositionRoot;

/// <summary>
/// Application-level health check for the composition root
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Perform application-level health checks
        var isHealthy = true; // Add actual health check logic here
        
        if (isHealthy)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("Application is running properly."));
        }

        return Task.FromResult(
            new HealthCheckResult(
                context.Registration.FailureStatus, 
                "Application health check failed."));
    }
}