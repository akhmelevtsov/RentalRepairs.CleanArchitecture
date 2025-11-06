using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Infrastructure.Authentication.Models;
using RentalRepairs.Infrastructure.Authentication.Services;
using RentalRepairs.Infrastructure.Configuration;
using RentalRepairs.Infrastructure.Persistence;
using RentalRepairs.Infrastructure.Persistence.Repositories;
using RentalRepairs.Infrastructure.Services;
using RentalRepairs.Infrastructure.Services.Email;
using RentalRepairs.Infrastructure.Seeding;
using RentalRepairs.Infrastructure.Seeding.Models;
using RentalRepairs.Domain.Repositories;

namespace RentalRepairs.Infrastructure;

/// <summary>
/// ONLY PUBLIC API: Single entry point for Infrastructure composition
/// All Infrastructure types are internal - WebUI can NEVER directly reference them
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// THE ONLY PUBLIC METHOD: Composition root for Infrastructure services
    /// This is the ONLY way WebUI can access Infrastructure functionality
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Configuration
        services.Configure<InfrastructureOptions>(configuration.GetSection("Infrastructure"));
        services.Configure<DemoAuthenticationSettings>(
            configuration.GetSection(DemoAuthenticationSettings.SectionName));
        services.Configure<SeedingOptions>(configuration.GetSection(SeedingOptions.SectionName));

        // Database Context (internal implementation, public interface)
        AddDatabaseContext(services, configuration, environment);

        // Repositories - all INTERNAL implementations registered to PUBLIC interfaces
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantRequestRepository, TenantRequestRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();

        // Authentication Services - Consolidated registration
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<DemoUserService>();

        // Register the consolidated authentication service that implements Application interface directly
        services.AddScoped<IAuthenticationService, Authentication.AuthenticationService>();
        services.AddScoped<IDemoUserService, DemoUserService>();

        // Domain Services for business logic (moved from Infrastructure concerns)
        services.AddScoped<RentalRepairs.Domain.Services.AuthorizationDomainService>();

        // Core Infrastructure Services - all INTERNAL implementations
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTime, DateTimeService>();

        // Email Services - using MockEmailService for development
        services.AddScoped<IEmailService, MockEmailService>();

        // Infrastructure Service Abstractions for WebUI
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        // Database Seeding Services
        services.AddScoped<IDatabaseSeederService, DatabaseSeederService>();

        // Caching
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// PUBLIC METHOD: Initialize demo data and seeding (composition root concern)
    /// </summary>
    public static async Task InitializeDemoDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        try
        {
            logger.LogInformation("Starting application data initialization...");

            // 1. Initialize demo users (in-memory authentication data)
            var demoUserService = scope.ServiceProvider.GetService<IDemoUserService>();
            if (demoUserService?.IsDemoModeEnabled() == true)
            {
                await demoUserService.InitializeDemoUsersAsync();
                logger.LogInformation("Demo authentication data initialized");
            }

            // 2. Attempt database seeding with graceful fallback
            var seederService = scope.ServiceProvider.GetService<IDatabaseSeederService>();
            if (seederService != null) await TryDatabaseInitializationAsync(seederService, logger);

            logger.LogInformation("Application data initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize application data");

            // In development, continue running even if database initialization fails
            // This allows the application to start and authentication to work
            var environment = scope.ServiceProvider.GetService<IWebHostEnvironment>();
            if (environment?.IsDevelopment() == true)
            {
                logger.LogWarning(
                    "Continuing application startup despite database initialization failure in development mode");
                logger.LogWarning("Database features will not be available until connection issues are resolved");
                return; // Don't throw, let the app continue
            }

            throw; // In production, fail fast
        }
    }

    /// <summary>
    /// Attempt database initialization with comprehensive error handling
    /// </summary>
    private static async Task TryDatabaseInitializationAsync(IDatabaseSeederService seederService, ILogger logger)
    {
        try
        {
            await seederService.SeedDevelopmentDataAsync();

            // Generate credentials file if seeding succeeded
            try
            {
                var credentialsPath = await seederService.GenerateCredentialsFileAsync();
                logger.LogInformation("Development credentials available at: {CredentialsPath}", credentialsPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to generate credentials file, but seeding was successful");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (
            sqlEx.Number == 17892 || // Logon failed due to trigger execution
            sqlEx.Number == 4060 || // Database does not exist
            sqlEx.Number == 2) // Timeout/connection issues
        {
            logger.LogWarning("Database connection issue detected: {ErrorMessage}", sqlEx.Message);
            logger.LogWarning("Common solutions:");
            logger.LogWarning(
                "1. Restart LocalDB: 'sqllocaldb stop MSSQLLocalDB' then 'sqllocaldb start MSSQLLocalDB'");
            logger.LogWarning("2. Delete and recreate LocalDB instance");
            logger.LogWarning("3. Check Windows Authentication permissions");
            logger.LogInformation("Application will continue without database features");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database seeding failed, application will continue without seeded data");
            logger.LogInformation("You can manually create data through the application UI");
        }
    }

    /// <summary>
    /// PUBLIC METHOD: Apply pending database migrations
    /// Fixed to handle in-memory databases used in testing
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        try
        {
            logger.LogInformation("Checking for pending database migrations...");

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Skip migration operations for in-memory databases (used in testing)
            if (context.Database.IsInMemory())
            {
                logger.LogInformation("In-memory database detected - skipping migration operations");
                await context.Database.EnsureCreatedAsync();
                return;
            }

            // Check if migration history table exists
            var historyTableExists = await context.Database.CanConnectAsync();
            if (historyTableExists)
                try
                {
                    var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

                    logger.LogInformation("Applied migrations: {AppliedCount}", appliedMigrations.Count());
                    logger.LogInformation("Pending migrations: {PendingCount}", pendingMigrations.Count());

                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                            pendingMigrations.Count(),
                            string.Join(", ", pendingMigrations));

                        await context.Database.MigrateAsync();
                        logger.LogInformation("Database migrations applied successfully");
                    }
                    else
                    {
                        logger.LogInformation("Database is up to date - no pending migrations");
                    }
                }
                catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2714) // Object already exists
                {
                    logger.LogWarning("Migration conflict detected - tables exist but migration history is incomplete");
                    logger.LogWarning(
                        "This typically happens when database was created with EnsureCreated() instead of migrations");
                    logger.LogWarning("Consider running: ResetDatabaseForDevelopmentAsync() to resolve conflicts");
                    logger.LogWarning("Application will continue but database schema may be inconsistent");
                }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (
            sqlEx.Number == 17892 || // Logon failed due to trigger execution
            sqlEx.Number == 4060 || // Database does not exist
            sqlEx.Number == 2) // Timeout/connection issues
        {
            logger.LogWarning("Database migration failed - connection issue: {ErrorMessage}", sqlEx.Message);
            logger.LogWarning("Common solutions:");
            logger.LogWarning(
                "1. Restart LocalDB: 'sqllocaldb stop MSSQLLocalDB' then 'sqllocaldb start MSSQLLocalDB'");
            logger.LogWarning("2. Check Windows Authentication permissions");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("relational database"))
        {
            logger.LogInformation(
                "Non-relational database detected (likely in-memory for testing) - skipping migration operations");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations");

            // In test environments, don't throw - let tests continue
            var environment = scope.ServiceProvider.GetService<IWebHostEnvironment>();
            if (environment?.EnvironmentName == "Testing")
            {
                logger.LogWarning("Test environment detected - continuing despite migration failure");
                return;
            }

            throw;
        }
    }

    /// <summary>
    /// PUBLIC METHOD: Reset database for development (removes all data and recreates with migrations)
    /// WARNING: This will delete all data - only use in development
    /// </summary>
    public static async Task ResetDatabaseForDevelopmentAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Database reset is only allowed in development environment");

        try
        {
            logger.LogWarning("DEVELOPMENT ONLY: Resetting database - ALL DATA WILL BE LOST");

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Handle in-memory databases
            if (context.Database.IsInMemory())
            {
                logger.LogInformation("In-memory database - using EnsureCreated instead of migrations");
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                return;
            }

            // Delete the database completely
            await context.Database.EnsureDeletedAsync();
            logger.LogInformation("Existing database deleted");

            // Apply all migrations from scratch
            await context.Database.MigrateAsync();
            logger.LogInformation("Database recreated with all migrations applied");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset database");
            throw;
        }
    }

    // ALL METHODS BELOW ARE PRIVATE/INTERNAL - WebUI CANNOT ACCESS

    private static void AddDatabaseContext(IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (environment.IsDevelopment())
                options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                    })
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            else
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
    }
}