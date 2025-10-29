using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace RentalRepairs.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext to support EF migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use SQL Server for migrations (connection string will be updated in appsettings)
        optionsBuilder.UseSqlServer("Server=(localdb)\\CleanDB;Database=CleanDB1;Trusted_Connection=true;MultipleActiveResultSets=true");
        
        // Create a simple logger for design-time operations
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ApplicationDbContext>();
        
        // Use the correct constructor signature with logger as required parameter
        return new ApplicationDbContext(optionsBuilder.Options, logger);
    }
}