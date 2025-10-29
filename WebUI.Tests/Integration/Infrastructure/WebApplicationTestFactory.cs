using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Persistence;

namespace RentalRepairs.WebUI.Tests.Integration.Infrastructure;

/// <summary>
/// ? CLEAN: WebApplicationFactory for Integration Tests with In-Memory Database
/// Provides isolated test environment for WebUI integration tests
/// Maintains Clean Architecture by using Application interfaces where possible
/// </summary>
public class WebApplicationTestFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing database registrations
            RemoveDbContextRegistrations(services);
            
            // Add in-memory database for testing
            AddInMemoryDatabase(services);
            
            // Configure logging for tests (reduce noise)
            ConfigureTestLogging(services);
            
            // Add required services for testing
            AddRequiredTestServices(services);
        });

        // Set test environment
        builder.UseEnvironment("Testing");
        
        // Build and initialize test database
        builder.ConfigureServices(services =>
        {
            var serviceProvider = services.BuildServiceProvider();
            InitializeTestDatabase(serviceProvider);
        });
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                       d.ServiceType == typeof(IApplicationDbContext) ||
                       d.ServiceType == typeof(ApplicationDbContext))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static void AddInMemoryDatabase(IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase($"WebUITests_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
            options.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());
    }

    private static void ConfigureTestLogging(IServiceCollection services)
    {
        // Configure minimal logging for tests to reduce noise
        services.AddLogging(builder => 
        {
            builder.ClearProviders()
                   .SetMinimumLevel(LogLevel.Warning)
                   .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Error)
                   .AddFilter("Microsoft.AspNetCore", LogLevel.Error)
                   .AddFilter("Microsoft.Extensions.Hosting", LogLevel.Error)
                   .AddConsole();
        });
    }

    private static void AddRequiredTestServices(IServiceCollection services)
    {
        // Add basic health checks (required since Program.cs maps the endpoint)
        services.AddHealthChecks()
            .AddCheck("test", () => 
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Test application is running"));
        
        // Ensure HttpContextAccessor is available for WebUI services
        if (!services.Any(d => d.ServiceType == typeof(IHttpContextAccessor)))
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }
    }

    private static void InitializeTestDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        try
        {
            var context = scope.ServiceProvider.GetService<IApplicationDbContext>();
            
            // Basic initialization - create database structure
            if (context is ApplicationDbContext dbContext)
            {
                dbContext.Database.EnsureCreated();
                
                // Don't initialize demo data in tests to avoid conflicts
                // Tests should set up their own test data as needed
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<WebApplicationTestFactory<TProgram>>>();
            logger?.LogWarning(ex, "Warning during test database initialization");
            
            // Don't throw here - some tests may not need database functionality
            // Let the tests handle database requirements individually
        }
    }
}