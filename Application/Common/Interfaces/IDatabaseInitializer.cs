namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// Interface for database initialization without exposing concrete DbContext to composition root.
/// 
/// NOTE: This is a simple abstraction that was considered for removal during dead code cleanup
/// but was intentionally kept. See Infrastructure.Services.DatabaseInitializer for detailed rationale.
/// 
/// Purpose:
/// - Abstracts database initialization operations from composition root
/// - Provides testable interface for database setup
/// - Keeps composition root code clean and focused
/// 
/// Implementation delegates to:
/// - EF Core Database.EnsureCreatedAsync() for database creation
/// - DatabaseSeederService for actual data seeding
/// 
/// Decision: Keep as-is. Benefits of abstraction outweigh modest code reduction from removal.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Ensures database is created and schema is up to date.
    /// Used during application startup for initial database setup.
    /// </summary>
    Task EnsureDatabaseCreatedAsync();

    /// <summary>
    /// Seeds demo/development data into the database.
    /// Implementation delegates to DatabaseSeederService for actual seeding.
    /// </summary>
    Task SeedDemoDataAsync();
}