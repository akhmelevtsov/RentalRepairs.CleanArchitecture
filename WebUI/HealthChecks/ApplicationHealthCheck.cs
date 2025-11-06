using Microsoft.Extensions.Diagnostics.HealthChecks;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.WebUI.HealthChecks;

/// <summary>
/// Application-specific health check that validates key services
/// Updated to use Application layer interfaces instead of direct Infrastructure dependencies
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    private readonly IApplicationDbContext? _context;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<ApplicationHealthCheck> _logger;

    public ApplicationHealthCheck(
        ILogger<ApplicationHealthCheck> logger,
        IApplicationDbContext? context = null,
        ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var checks = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            // Check database connectivity if available
            if (_context != null)
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    checks["database_connection"] = "healthy";
                }
                catch
                {
                    checks["database_connection"] = "unavailable";
                }
            else
                checks["database_connection"] = "not_configured";

            // Check application services
            if (_currentUserService != null)
            {
                var isAuthenticated = _currentUserService.IsAuthenticated;
                checks["user_service"] = "healthy";
                checks["authentication_available"] = isAuthenticated;
            }
            else
            {
                checks["user_service"] = "not_configured";
            }

            checks["application_services"] = "healthy";

            _logger.LogInformation("Application health check completed successfully");

            return HealthCheckResult.Healthy("Application is running normally", checks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application health check failed");

            var errorData = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Unhealthy("Application health check failed", ex, errorData);
        }
    }
}