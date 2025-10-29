using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.CompositionRoot;

/// <summary>
/// Helper class for logging in ApplicationCompositionRoot
/// </summary>
public class ApplicationInitializer
{
}

/// <summary>
/// Application-level composition root for initialization logic
/// </summary>
public static class ApplicationCompositionRoot
{
    /// <summary>
    /// Initialize application-level services and data
    /// </summary>
    public static async Task InitializeApplicationAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        try
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationInitializer>>();
            logger.LogInformation("Starting application initialization...");

            // Initialize database if needed
            var databaseInitializer = scope.ServiceProvider
                .GetRequiredService<IDatabaseInitializer>();
            
            await databaseInitializer.EnsureDatabaseCreatedAsync();

            // Use the proper seeding service instead of the empty method
            var seederService = scope.ServiceProvider
                .GetRequiredService<IDatabaseSeederService>();
            
            logger.LogInformation("Checking if database seeding is needed...");
            if (!await seederService.IsDevelopmentDataSeededAsync())
            {
                logger.LogInformation("Database appears empty, starting seeding process...");
                await seederService.SeedDevelopmentDataAsync();
                
                // Generate credentials file
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
            else
            {
                logger.LogInformation("Database already contains data, skipping seeding");
            }

            // Initialize demo users for authentication
            var demoUserService = scope.ServiceProvider
                .GetRequiredService<IDemoUserService>();
            
            if (demoUserService.IsDemoModeEnabled())
            {
                logger.LogInformation("Demo mode is enabled, initializing demo users...");
                await demoUserService.InitializeDemoUsersAsync();
                logger.LogInformation("Demo users initialized successfully");
            }
            else
            {
                logger.LogInformation("Demo mode is disabled");
            }

            logger.LogInformation("Application initialization completed successfully");
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationInitializer>>();
            logger.LogError(ex, "Error during application initialization");
            throw;
        }
    }
}