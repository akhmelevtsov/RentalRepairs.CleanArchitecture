using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RentalRepairs.Infrastructure.Persistence;
using RentalRepairs.Infrastructure.Seeding;

namespace RentalRepairs.Tools;

/// <summary>
/// Tool for resetting the database during development.
/// Provides clean database state for testing and development.
/// </summary>
public class ResetDatabase
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("?? Starting Database Reset Tool...");
            
            // Build the host (same as WebUI but without web components)
            var host = CreateHost(args);
            
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeederService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ResetDatabase>>();
            
            // Reset the database
            await ResetDatabaseAsync(context, seeder, logger);
            
            Console.WriteLine("? Database reset completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Database reset failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }
    
    private static IHost CreateHost(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add Entity Framework
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("DefaultConnection") ?? 
                        "Server=(localdb)\\MSSQLLocalDB;Database=RentalRepairsDb;Trusted_Connection=true;MultipleActiveResultSets=true"));
                
                // Add seeding service
                services.AddScoped<DatabaseSeederService>();
                
                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();
    }
    
    private static async Task ResetDatabaseAsync(
        ApplicationDbContext context, 
        DatabaseSeederService seeder,
        ILogger<ResetDatabase> logger)
    {
        Console.WriteLine("???  Dropping existing database...");
        await context.Database.EnsureDeletedAsync();
        
        Console.WriteLine("???  Creating fresh database...");
        await context.Database.EnsureCreatedAsync();
        
        Console.WriteLine("?? Seeding database with test data...");
        await seeder.SeedAsync();
        
        logger.LogInformation("Database reset and seeded successfully");
    }
}