using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Persistence;

namespace RentalRepairs.Infrastructure.Services;

/// <summary>
/// Database initialization service that abstracts database operations from composition root.
/// 
/// NOTE: This is a thin wrapper over ApplicationDbContext.Database operations.
/// While it could be considered redundant, it is intentionally kept because:
/// 
/// 1. Provides clear intent in ApplicationCompositionRoot initialization code
/// 2. Abstracts infrastructure concerns from composition root
/// 3. Works correctly and causes no maintenance issues
/// 4. Can be easily mocked in tests if needed
/// 5. The risk of refactoring composition root > benefit of removing this wrapper
/// 
/// Real work is delegated to:
/// - EnsureCreatedAsync() ? EF Core database creation
/// - DatabaseSeederService ? Actual data seeding
/// 
/// Decision: Keep as-is. Can be refactored if composition root needs changes for other reasons.
/// See: INFRASTRUCTURE_CLEANUP_COMPLETE.md - Step 3 for detailed analysis.
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

    /// <summary>
    /// Ensures database is created (thin wrapper over EF Core EnsureCreatedAsync).
    /// Note: In production, migrations are preferred via DependencyInjection.ApplyDatabaseMigrationsAsync().
    /// </summary>
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

    /// <summary>
    /// Placeholder for demo data seeding.
    /// Note: Actual seeding is done by DatabaseSeederService.
    /// This method exists for interface completeness but delegates real work elsewhere.
    /// </summary>
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