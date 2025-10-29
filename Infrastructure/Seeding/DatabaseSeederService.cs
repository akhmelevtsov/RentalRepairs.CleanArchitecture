using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using RentalRepairs.Application.Commands.Properties.RegisterProperty;
using RentalRepairs.Application.Commands.Tenants.RegisterTenant;
using RentalRepairs.Application.Commands.Workers.RegisterWorker;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Seeding.Data;
using RentalRepairs.Infrastructure.Seeding.Models;
using System.Text;
using RentalRepairs.Domain.Repositories;

namespace RentalRepairs.Infrastructure.Seeding;

/// <summary>
/// Service responsible for seeding development test data using CQRS commands.
/// Follows single responsibility principle with clear separation of concerns.
/// </summary>
public class DatabaseSeederService : IDatabaseSeederService
{
    private readonly IMediator _mediator;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IWorkerRepository _workerRepository;
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeederService> _logger;
    private readonly SeedingOptions _options;

    public DatabaseSeederService(
        IMediator mediator,
        IPropertyRepository propertyRepository,
        ITenantRepository tenantRepository,
        IWorkerRepository workerRepository,
        IDatabaseInitializer databaseInitializer,
        IServiceProvider serviceProvider,
        ILogger<DatabaseSeederService> logger,
        IOptions<SeedingOptions> options)
    {
        _mediator = mediator;
        _propertyRepository = propertyRepository;
        _tenantRepository = tenantRepository;
        _workerRepository = workerRepository;
        _databaseInitializer = databaseInitializer;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Main seeding orchestration method with proper flow control
    /// </summary>
    public async Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableSeeding)
        {
            _logger.LogInformation("Development data seeding is disabled in configuration");
            return;
        }

        _logger.LogInformation("Starting development data seeding process...");

        try
        {
            // Step 1: Ensure database exists (critical first step)
            await EnsureDatabaseIsReadyAsync(cancellationToken);

            // Step 2: Check if seeding is needed
            if (await IsAlreadySeededAsync(cancellationToken))
            {
                _logger.LogInformation("Development data already exists, skipping seeding");
                return;
            }

            // Step 3: Execute seeding in proper order
            await ExecuteSeedingWorkflowAsync(cancellationToken);

            _logger.LogInformation("Development data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed development data");
            throw new InvalidOperationException("Development data seeding failed", ex);
        }
    }

    /// <summary>
    /// Check if data is already seeded (called after database exists)
    /// </summary>
    public async Task<bool> IsDevelopmentDataSeededAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var counts = await GetEntityCountsAsync(cancellationToken);
            var isSeeded = IsDataSufficient(counts);
            
            _logger.LogInformation(
                "Seeding status check: Properties={PropertyCount}, Tenants={TenantCount}, Workers={WorkerCount}, IsSeeded={IsSeeded}",
                counts.Properties, counts.Tenants, counts.Workers, isSeeded);
                
            return isSeeded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine seeding status, assuming not seeded");
            return false;
        }
    }

    /// <summary>
    /// Generate credentials file for development use with proper DbContext handling
    /// </summary>
    public async Task<string> GenerateCredentialsFileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating development credentials file...");

            var (properties, workers) = await LoadCredentialDataWithSeparateScopesAsync(cancellationToken);
            var content = GenerateCredentialsContent(properties, workers);
            var filePath = await SaveCredentialsFileAsync(content, cancellationToken);

            _logger.LogInformation("Credentials file generated: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate credentials file");
            throw new InvalidOperationException("Credentials file generation failed", ex);
        }
    }

    #region Private Workflow Methods

    /// <summary>
    /// Ensures database is created and ready for operations
    /// </summary>
    private async Task EnsureDatabaseIsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Ensuring database is created and accessible...");
            await _databaseInitializer.EnsureDatabaseCreatedAsync();
            _logger.LogDebug("Database is ready for seeding operations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw new InvalidOperationException("Database initialization failed", ex);
        }
    }

    /// <summary>
    /// Check if data already exists (assumes database is ready)
    /// </summary>
    private async Task<bool> IsAlreadySeededAsync(CancellationToken cancellationToken)
    {
        try
        {
            var counts = await GetEntityCountsAsync(cancellationToken);
            return IsDataSufficient(counts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot check seeding status, proceeding with seeding");
            return false; // If we can't check, assume we need to seed
        }
    }

    /// <summary>
    /// Execute the complete seeding workflow
    /// </summary>
    private async Task ExecuteSeedingWorkflowAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing seeding workflow: Properties ? Tenants ? Workers");

        // Generate test data
        var testData = GenerateTestData();
        
        // Seed in proper dependency order
        await SeedPropertiesWithTenantsAsync(testData.Properties, cancellationToken);
        await SeedWorkersAsync(testData.Workers, cancellationToken);

        _logger.LogInformation("Seeding workflow completed successfully");
    }

    /// <summary>
    /// Generate all test data upfront
    /// </summary>
    private (List<TestPropertyData> Properties, List<TestWorkerData> Workers) GenerateTestData()
    {
        _logger.LogDebug("Generating test data: {PropertyCount} properties, {WorkerCount} workers", 
            _options.PropertyCount, _options.WorkerCount);

        var properties = SeedDataGenerator.GenerateProperties(_options.PropertyCount, _options.TenantsPerProperty);
        var workers = SeedDataGenerator.GenerateWorkers(_options.WorkerCount);

        return (properties, workers);
    }

    #endregion

    #region Private Data Access Methods

    /// <summary>
    /// Get current entity counts for seeding status check
    /// </summary>
    private async Task<(int Properties, int Tenants, int Workers)> GetEntityCountsAsync(CancellationToken cancellationToken)
    {
        var propertyCount = await _propertyRepository.CountAsync(cancellationToken);
        var tenantCount = await _tenantRepository.CountAsync(cancellationToken);
        var workerCount = await _workerRepository.CountAsync(cancellationToken);

        return (propertyCount, tenantCount, workerCount);
    }

    /// <summary>
    /// Determine if current data counts are sufficient
    /// </summary>
    private bool IsDataSufficient((int Properties, int Tenants, int Workers) counts)
    {
        var minProperties = Math.Max(3, _options.PropertyCount);
        var minTenants = Math.Max(9, _options.PropertyCount * _options.TenantsPerProperty);
        var minWorkers = Math.Max(10, _options.WorkerCount);

        return counts.Properties >= minProperties && 
               counts.Tenants >= minTenants && 
               counts.Workers >= minWorkers;
    }

    /// <summary>
    /// Load data needed for credentials generation using separate service scopes to avoid DbContext concurrency
    /// </summary>
    private async Task<(IEnumerable<Domain.Entities.Property> Properties, IEnumerable<Domain.Entities.Worker> Workers)> 
        LoadCredentialDataWithSeparateScopesAsync(CancellationToken cancellationToken)
    {
        // Create separate scopes to ensure each repository gets its own DbContext instance
        using var propertyScope = _serviceProvider.CreateScope();
        using var workerScope = _serviceProvider.CreateScope();

        var propertyRepository = propertyScope.ServiceProvider.GetRequiredService<IPropertyRepository>();
        var workerRepository = workerScope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        // Execute operations sequentially with separate DbContext instances
        var properties = await propertyRepository.GetAllAsync(cancellationToken);
        var workers = await workerRepository.GetAllAsync(cancellationToken);

        return (properties, workers);
    }

    #endregion

    #region Private Seeding Methods

    /// <summary>
    /// Seed properties and their associated tenants
    /// </summary>
    private async Task SeedPropertiesWithTenantsAsync(List<TestPropertyData> properties, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding {PropertyCount} properties with tenants...", properties.Count);

        foreach (var propertyData in properties)
        {
            try
            {
                var propertyId = await CreatePropertyAsync(propertyData, cancellationToken);
                await CreateTenantsForPropertyAsync(propertyId, propertyData.Tenants, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed property: {PropertyName}", propertyData.Name);
                // Continue with other properties rather than failing completely
            }
        }

        _logger.LogInformation("Property seeding completed");
    }

    /// <summary>
    /// Create a single property using CQRS command
    /// </summary>
    private async Task<Guid> CreatePropertyAsync(TestPropertyData propertyData, CancellationToken cancellationToken)
    {
        var command = new RegisterPropertyCommand
        {
            Name = propertyData.Name,
            Code = propertyData.Code,
            Address = new PropertyAddressDto
            {
                StreetNumber = propertyData.Address.StreetNumber,
                StreetName = propertyData.Address.StreetName,
                City = propertyData.Address.City,
                PostalCode = propertyData.Address.PostalCode,
                FullAddress = $"{propertyData.Address.StreetNumber} {propertyData.Address.StreetName}, {propertyData.Address.City}, {propertyData.Address.PostalCode}"
            },
            PhoneNumber = propertyData.PhoneNumber,
            Superintendent = new PersonContactInfoDto
            {
                FirstName = propertyData.Superintendent.FirstName,
                LastName = propertyData.Superintendent.LastName,
                EmailAddress = propertyData.Superintendent.Email,
                MobilePhone = propertyData.Superintendent.MobilePhone,
                FullName = $"{propertyData.Superintendent.FirstName} {propertyData.Superintendent.LastName}"
            },
            Units = propertyData.Units,
            NoReplyEmailAddress = propertyData.NoReplyEmail
        };

        var propertyId = await _mediator.Send(command, cancellationToken);
        
        _logger.LogDebug("Created property: {PropertyName} ({PropertyCode}) - ID: {PropertyId}", 
            propertyData.Name, propertyData.Code, propertyId);

        return propertyId;
    }

    /// <summary>
    /// Create tenants for a specific property
    /// </summary>
    private async Task CreateTenantsForPropertyAsync(Guid propertyId, List<TestTenantData> tenants, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating {TenantCount} tenants for property {PropertyId}...", tenants.Count, propertyId);

        foreach (var tenantData in tenants)
        {
            try
            {
                var command = new RegisterTenantCommand
                {
                    PropertyId = propertyId,
                    UnitNumber = tenantData.UnitNumber,
                    ContactInfo = new PersonContactInfoDto
                    {
                        FirstName = tenantData.ContactInfo.FirstName,
                        LastName = tenantData.ContactInfo.LastName,
                        EmailAddress = tenantData.ContactInfo.Email,
                        MobilePhone = tenantData.ContactInfo.MobilePhone,
                        FullName = $"{tenantData.ContactInfo.FirstName} {tenantData.ContactInfo.LastName}"
                    }
                };

                var tenantId = await _mediator.Send(command, cancellationToken);
                
                _logger.LogDebug("Created tenant: {TenantName} in unit {Unit} - ID: {TenantId}", 
                    command.ContactInfo.FullName, tenantData.UnitNumber, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create tenant {Email} in unit {Unit}", 
                    tenantData.ContactInfo.Email, tenantData.UnitNumber);
            }
        }
    }

    /// <summary>
    /// Seed all workers
    /// </summary>
    private async Task SeedWorkersAsync(List<TestWorkerData> workers, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding {WorkerCount} workers...", workers.Count);

        foreach (var workerData in workers)
        {
            try
            {
                var command = new RegisterWorkerCommand
                {
                    ContactInfo = new PersonContactInfoDto
                    {
                        FirstName = workerData.ContactInfo.FirstName,
                        LastName = workerData.ContactInfo.LastName,
                        EmailAddress = workerData.ContactInfo.Email,
                        MobilePhone = workerData.ContactInfo.MobilePhone,
                        FullName = $"{workerData.ContactInfo.FirstName} {workerData.ContactInfo.LastName}"
                    },
                    Specialization = workerData.Specialization
                };

                var workerId = await _mediator.Send(command, cancellationToken);
                
                _logger.LogDebug("Created worker: {WorkerName} ({Specialization}) - ID: {WorkerId}", 
                    command.ContactInfo.FullName, workerData.Specialization, workerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create worker: {Email} ({Specialization})", 
                    workerData.ContactInfo.Email, workerData.Specialization);
            }
        }

        _logger.LogInformation("Worker seeding completed");
    }

    #endregion

    #region Private Credentials Generation Methods

    /// <summary>
    /// Generate the markdown content for credentials file
    /// </summary>
    private string GenerateCredentialsContent(
        IEnumerable<Domain.Entities.Property> properties, 
        IEnumerable<Domain.Entities.Worker> workers)
    {
        var sb = new StringBuilder();
        
        AppendHeader(sb);
        AppendSystemAdmins(sb);
        AppendProperties(sb, properties);
        AppendWorkers(sb, workers);
        AppendFooter(sb);

        return sb.ToString();
    }

    /// <summary>
    /// Save credentials content to file
    /// </summary>
    private async Task<string> SaveCredentialsFileAsync(string content, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _options.CredentialsFileName);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        return filePath;
    }

    private void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine("# Development Test Credentials");
        sb.AppendLine();
        sb.AppendLine($"*Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
        sb.AppendLine();
    }

    private void AppendSystemAdmins(StringBuilder sb)
    {
        sb.AppendLine("## ?? System Administrators");
        sb.AppendLine("- **Email**: `admin@demo.com`");
        sb.AppendLine($"- **Password**: `{_options.DefaultPassword}`");
        sb.AppendLine("- **Role**: SystemAdmin");
        sb.AppendLine();
    }

    private void AppendProperties(StringBuilder sb, IEnumerable<Domain.Entities.Property> properties)
    {
        sb.AppendLine("## ?? Properties & Superintendents");
        sb.AppendLine();

        int index = 1;
        foreach (var property in properties.OrderBy(p => p.Code))
        {
            sb.AppendLine($"### {index}. {property.Name} (`{property.Code}`)");
            sb.AppendLine($"- **Superintendent**: `{property.Superintendent.EmailAddress}` / `{_options.DefaultPassword}`");
            sb.AppendLine($"- **Name**: {property.Superintendent.GetFullName()}");
            sb.AppendLine($"- **Address**: {property.Address.FullAddress}");
            sb.AppendLine($"- **Phone**: {property.PhoneNumber}");
            
            if (property.Tenants.Any())
            {
                sb.AppendLine();
                sb.AppendLine("**Tenants:**");
                foreach (var tenant in property.Tenants.OrderBy(t => t.UnitNumber))
                {
                    sb.AppendLine($"- Unit {tenant.UnitNumber}: `{tenant.ContactInfo.EmailAddress}` / `{_options.DefaultPassword}` ({tenant.ContactInfo.GetFullName()})");
                }
            }
            
            sb.AppendLine();
            index++;
        }
    }

    private void AppendWorkers(StringBuilder sb, IEnumerable<Domain.Entities.Worker> workers)
    {
        sb.AppendLine("## ?? Maintenance Workers");
        sb.AppendLine();
        
        foreach (var worker in workers.OrderBy(w => w.Specialization).ThenBy(w => w.ContactInfo.GetFullName()))
        {
            sb.AppendLine($"- **`{worker.ContactInfo.EmailAddress}`** / `{_options.DefaultPassword}`");
            sb.AppendLine($"  - Name: {worker.ContactInfo.GetFullName()}");
            sb.AppendLine($"  - Specialization: {worker.Specialization}");
            sb.AppendLine($"  - Phone: {worker.ContactInfo.MobilePhone}");
            sb.AppendLine();
        }
    }

    private void AppendFooter(StringBuilder sb)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## ?? Login Instructions");
        sb.AppendLine("1. Navigate to `/Account/Login`");
        sb.AppendLine("2. Use unified login - just enter email and password");
        sb.AppendLine("3. System automatically detects your role and redirects to appropriate dashboard");
        sb.AppendLine();
        sb.AppendLine("## ?? Security Notes");
        sb.AppendLine("- **Development use only** - All accounts use the same password");
        sb.AppendLine("- **Do not commit** this file to source control");
        sb.AppendLine("- **Production deployments** should use proper authentication");
        sb.AppendLine();
    }

    #endregion
}