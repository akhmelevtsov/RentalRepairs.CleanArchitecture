namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// Interface for database initialization without exposing concrete DbContext
/// </summary>
public interface IDatabaseInitializer
{
    Task EnsureDatabaseCreatedAsync();
    Task SeedDemoDataAsync();
}