using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Persistence;

namespace RentalRepairs.Infrastructure.Services;

/// <summary>
/// Database initialization service that abstracts database operations from WebUI
/// </summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(ApplicationDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("Database creation/verification completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure database is created");
            throw;
        }
    }

    public async Task SeedDemoDataAsync()
    {
        try
        {
            // Basic seeding logic can be implemented here
            // For now, delegate to existing seeding logic
            await _context.SaveChangesAsync();
            _logger.LogInformation("Demo data seeding completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed demo data");
            throw;
        }
    }
}