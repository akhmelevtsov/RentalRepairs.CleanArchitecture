namespace RentalRepairs.Application.Interfaces;

/// <summary>
/// Service for seeding development test data using existing application commands
/// </summary>
public interface IDatabaseSeederService
{
    /// <summary>
    /// Seeds development test data if not already seeded
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if development data has already been seeded
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if data is already seeded, false otherwise</returns>
    Task<bool> IsDevelopmentDataSeededAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and saves credential markdown file
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the generated credentials file</returns>
    Task<string> GenerateCredentialsFileAsync(CancellationToken cancellationToken = default);
}