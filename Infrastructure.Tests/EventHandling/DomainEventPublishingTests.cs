using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Infrastructure.Persistence;
using RentalRepairs.Application.EventHandlers.Notifications;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Application.Services;
using RentalRepairs.Application.Interfaces;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Services;

namespace RentalRepairs.Infrastructure.Tests.EventHandling;

/// <summary>
/// Tests to verify that domain events are properly published and handled
/// </summary>
public class DomainEventPublishingTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;

    public DomainEventPublishingTests()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add MediatR with proper assembly scanning
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(TenantRequestNotificationHandler).Assembly, // Application assembly
                typeof(TenantRequestSubmittedEvent).Assembly // Domain assembly
            );
        });

        // Add required Infrastructure services
        services.AddScoped<IAuditService, TestAuditService>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<IDateTime, DateTimeService>();
        services.AddScoped<ICurrentUserService, TestCurrentUserService>();

        // Add email service (required by TenantRequestNotificationHandler)
        services.AddScoped<IEmailService, MockEmailService>();

        // Add domain services (required by NotifyPartiesService)
        services.AddScoped<RentalRepairs.Domain.Services.SpecializationDeterminationService>();

        // Add notification service
        services.AddScoped<INotifyPartiesService, NotifyPartiesService>();

        // Add in-memory database with proper dependency injection
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task PropertyRegistration_Should_PublishPropertyRegisteredEvent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DomainEventPublishingTests>>();

        var address = new PropertyAddress("123", "Main Street", "Test City", "12345");
        var superintendent = new PersonContactInfo("John", "Doe", "john@example.com", "555-1234");
        var units = new List<string> { "101", "102" };

        // Act
        var property = new Property("Test Property", "TP001", address, "555-1234", superintendent, units,
            "noreply@test.com");

        // Verify events were added during creation
        property.DomainEvents.Should().NotBeEmpty("Property creation should generate domain events");

        // Add to context and save (this should trigger domain event publishing)
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        // Assert
        property.DomainEvents.Should().BeEmpty("Events should be cleared after publishing");
        logger.LogInformation("Property registration test completed successfully");
    }

    [Fact]
    public async Task TenantRegistration_Should_PublishTenantRegisteredEvent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DomainEventPublishingTests>>();

        var address = new PropertyAddress("456", "Oak Avenue", "Test City", "67890");
        var superintendent = new PersonContactInfo("Jane", "Smith", "jane@example.com", "555-5678");
        var units = new List<string> { "201", "202" };

        var property = new Property("Oak Property", "OP001", address, "555-5678", superintendent, units,
            "noreply@oak.com");
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        var tenantContact = new PersonContactInfo("Bob", "Johnson", "bob@example.com", "555-9999");

        // Act
        var tenant = property.RegisterTenant(tenantContact, "201");

        // Verify events were added during tenant registration
        tenant.DomainEvents.Should().NotBeEmpty("Tenant registration should generate domain events");

        await _context.SaveChangesAsync();

        // Assert
        tenant.DomainEvents.Should().BeEmpty("Events should be cleared after publishing");
        property.DomainEvents.Should().BeEmpty("Events should be cleared after publishing");
        logger.LogInformation("Tenant registration test completed successfully");
    }

    [Fact]
    public async Task TenantRequestCreation_Should_PublishTenantRequestCreatedEvent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DomainEventPublishingTests>>();

        // Create property and tenant
        var address = new PropertyAddress("789", "Pine Street", "Test City", "11111");
        var superintendent = new PersonContactInfo("Alice", "Brown", "alice@example.com", "555-1111");
        var units = new List<string> { "301", "302" };

        var property = new Property("Pine Property", "PP001", address, "555-1111", superintendent, units,
            "noreply@pine.com");
        var tenantContact = new PersonContactInfo("Charlie", "Davis", "charlie@example.com", "555-2222");
        var tenant = property.RegisterTenant(tenantContact, "301");

        _context.Properties.Add(property);
        await _context.SaveChangesAsync(); // This will publish PropertyRegisteredEvent and TenantRegisteredEvent

        // Act
        var tenantRequest = tenant.CreateRequest("Leaky Faucet", "Kitchen faucet is dripping", "Normal");

        // Verify events were added during request creation
        tenantRequest.DomainEvents.Should().NotBeEmpty("Tenant request creation should generate domain events");

        await _context.SaveChangesAsync(); // This should publish TenantRequestCreatedEvent

        // Assert
        tenantRequest.DomainEvents.Should().BeEmpty("Events should be cleared after publishing");
        tenant.DomainEvents.Should().BeEmpty("Events should be cleared after publishing");
        logger.LogInformation("Tenant request creation test completed successfully");
    }

    [Fact]
    public async Task TenantRequestSubmission_Should_PublishTenantRequestSubmittedEvent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DomainEventPublishingTests>>();

        // Create property, tenant, and request
        var address = new PropertyAddress("999", "Elm Street", "Test City", "22222");
        var superintendent = new PersonContactInfo("David", "Wilson", "david@example.com", "555-3333");
        var units = new List<string> { "401", "402" };

        var property = new Property("Elm Property", "EP001", address, "555-3333", superintendent, units,
            "noreply@elm.com");
        var tenantContact = new PersonContactInfo("Eve", "Miller", "eve@example.com", "555-4444");
        var tenant = property.RegisterTenant(tenantContact, "401");
        var tenantRequest = tenant.CreateRequest("Broken Window", "Living room window won't open", "High");

        _context.Properties.Add(property);
        await _context.SaveChangesAsync(); // This will publish various events

        // Act
        tenantRequest.Submit();

        // Verify events were added during submission
        tenantRequest.DomainEvents.Should().NotBeEmpty("Tenant request submission should generate domain events");

        await _context.SaveChangesAsync(); // This should publish TenantRequestSubmittedEvent

        // Assert
        tenantRequest.Status.Should().Be(Domain.Enums.TenantRequestStatus.Submitted);
        tenantRequest.DomainEvents.Should().BeEmpty("Events should be cleared after publishing");
        logger.LogInformation("Tenant request submission test completed successfully");
    }

    [Fact]
    public async Task WorkerRegistration_Should_PublishWorkerRegisteredEvent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DomainEventPublishingTests>>();

        var workerContact = new PersonContactInfo("Frank", "Garcia", "frank@example.com", "555-5555");

        // Act
        var worker = new Worker(workerContact);
        worker.SetSpecialization(Domain.Enums.WorkerSpecialization.Plumbing);

        // Verify events were added during worker creation/setup
        worker.DomainEvents.Should().NotBeEmpty("Worker registration should generate domain events");

        _context.Workers.Add(worker);
        await _context.SaveChangesAsync(); // This should publish WorkerRegisteredEvent

        // Assert
        worker.IsActive.Should().BeTrue();
        worker.Specialization.Should().Be(Domain.Enums.WorkerSpecialization.Plumbing);
        worker.DomainEvents.Should().BeEmpty("Events should be cleared after publishing");
        logger.LogInformation("Worker registration test completed successfully");
    }

    [Fact]
    public async Task MultipleEvents_Should_AllBePublished()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DomainEventPublishingTests>>();

        // Create a scenario that generates multiple domain events
        var address = new PropertyAddress("777", "Maple Street", "Test City", "33333");
        var superintendent = new PersonContactInfo("Grace", "Lee", "grace@example.com", "555-6666");
        var units = new List<string> { "501", "502", "503" };

        var property = new Property("Maple Property", "MP001", address, "555-6666", superintendent, units,
            "noreply@maple.com");

        var tenant1Contact = new PersonContactInfo("Henry", "Taylor", "henry@example.com", "555-7777");
        var tenant1 = property.RegisterTenant(tenant1Contact, "501");

        var tenant2Contact = new PersonContactInfo("Iris", "Anderson", "iris@example.com", "555-8888");
        var tenant2 = property.RegisterTenant(tenant2Contact, "502");

        var request1 = tenant1.CreateRequest("Heating Issue", "Heater not working", "High");
        var request2 = tenant2.CreateRequest("Plumbing Problem", "Sink clogged", "Normal");

        var workerContact = new PersonContactInfo("Jack", "Thomas", "jack@example.com", "555-9999");
        var worker = new Worker(workerContact);

        // Act
        _context.Properties.Add(property);
        _context.Workers.Add(worker);
        await _context.SaveChangesAsync(); // This should publish multiple events

        request1.Submit();
        request2.Submit();
        await _context.SaveChangesAsync(); // This should publish TenantRequestSubmittedEvents

        // Assert
        property.DomainEvents.Should().BeEmpty();
        tenant1.DomainEvents.Should().BeEmpty();
        tenant2.DomainEvents.Should().BeEmpty();
        request1.DomainEvents.Should().BeEmpty();
        request2.DomainEvents.Should().BeEmpty();
        worker.DomainEvents.Should().BeEmpty();

        request1.Status.Should().Be(Domain.Enums.TenantRequestStatus.Submitted);
        request2.Status.Should().Be(Domain.Enums.TenantRequestStatus.Submitted);

        logger.LogInformation(
            "Multiple events test completed successfully - all domain events were published and cleared");
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}

/// <summary>
/// Test implementation of ICurrentUserService for testing scenarios
/// </summary>
internal class TestCurrentUserService : ICurrentUserService
{
    public string? UserId => "test-user-123";
    public string? UserName => "Test User";
    public bool IsAuthenticated => true;
    public string? UserRole => "TestUser";

    public Dictionary<string, string> GetUserClaims()
    {
        return new Dictionary<string, string>
        {
            ["sub"] = UserId ?? "",
            ["name"] = UserName ?? "",
            ["role"] = UserRole ?? ""
        };
    }

    public string GetAuditUserIdentifier()
    {
        return $"{UserName} ({UserId})";
    }
}

/// <summary>
/// Test implementation of IAuditService for testing scenarios
/// </summary>
internal class TestAuditService : IAuditService
{
    public void ApplyAuditInformation(IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries,
        string? currentUser = null)
    {
        var auditUser = currentUser ?? "test-user";
        var auditTime = DateTime.UtcNow;

        foreach (var entry in entries)
            if (entry.Entity is Domain.Common.IAuditableEntity auditableEntity)
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditableEntity.CreatedAt = auditTime;
                        auditableEntity.CreatedBy = auditUser;
                        break;
                    case EntityState.Modified:
                        auditableEntity.UpdatedAt = auditTime;
                        auditableEntity.UpdatedBy = auditUser;
                        break;
                }
    }
}

/// <summary>
/// Test implementation of MockEmailService for testing scenarios
/// </summary>
internal class MockEmailService : IEmailService
{
    public Task SendEmailAsync(EmailInfo emailInfo, CancellationToken cancellationToken = default)
    {
        // Mock implementation - just log the email send
        Console.WriteLine($"Mock email sent to {emailInfo.RecipientEmail}: {emailInfo.Subject}");
        return Task.CompletedTask;
    }

    public Task SendBulkEmailAsync(IEnumerable<EmailInfo> emails, CancellationToken cancellationToken = default)
    {
        // Mock implementation
        foreach (var email in emails)
            Console.WriteLine($"Mock bulk email sent to {email.RecipientEmail}: {email.Subject}");
        return Task.CompletedTask;
    }
}