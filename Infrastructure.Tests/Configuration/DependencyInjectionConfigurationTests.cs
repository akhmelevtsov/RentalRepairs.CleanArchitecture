using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using RentalRepairs.Infrastructure.Configuration;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Infrastructure.Authentication.Services;
using Xunit;
using Xunit.Abstractions;

namespace RentalRepairs.Infrastructure.Tests.Configuration;

/// <summary>
/// Tests for Issue #14 fix validation - Dependency Injection Over-Configuration resolution
/// Validates consolidated configuration and simplified service registration
/// Updated to work with new secure authentication system
/// </summary>
public class DependencyInjectionConfigurationTests
{
    private readonly ITestOutputHelper _output;

    public DependencyInjectionConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ConsolidatedConfiguration_ShouldBindCorrectly_FromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:Database:ConnectionString"] = "test-connection",
                ["Infrastructure:Database:EnableRetry"] = "true",
                ["Infrastructure:Database:CommandTimeoutSeconds"] = "45",
                ["Infrastructure:Email:Provider"] = "Smtp",
                ["Infrastructure:Email:DefaultSender"] = "test@example.com",
                ["Infrastructure:Authentication:TokenExpirationHours"] = "12",
                ["Infrastructure:Performance:EnableCaching"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetSection(InfrastructureOptions.SectionName))
            .ValidateDataAnnotations();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<InfrastructureOptions>>().Value;

        // Assert
        options.Database.ConnectionString.Should().Be("test-connection");
        options.Database.EnableRetry.Should().BeTrue();
        options.Database.CommandTimeoutSeconds.Should().Be(45);
        options.Email.Provider.Should().Be("Smtp");
        options.Email.DefaultSender.Should().Be("test@example.com");
        options.Authentication.TokenExpirationHours.Should().Be(12);
        options.Performance.EnableCaching.Should().BeFalse();

        _output.WriteLine("Consolidated configuration binding works correctly");
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterServices_WithCorrectLifetimes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "test-connection",
                ["Infrastructure:Email:Provider"] = "Mock",
                ["DemoAuthentication:EnableDemoMode"] = "true",
                ["DemoAuthentication:DefaultPassword"] = "Test123!"
            })
            .Build();

        var services = new ServiceCollection();
        var mockEnvironment = new MockWebHostEnvironment { EnvironmentName = "Development" };

        // Act
        services.AddInfrastructure(configuration, mockEnvironment);

        // Assert - Check service lifetimes for core services
        var auditServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAuditService));
        var currentUserServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICurrentUserService));

        // Check authentication services with explicit namespace
        var passwordServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPasswordService));
        var demoUserServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IDemoUserService));

        // Verify correct service lifetimes
        auditServiceDescriptor.Should().NotBeNull();
        auditServiceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped,
            "Audit service should be scoped for per-request auditing");

        currentUserServiceDescriptor.Should().NotBeNull();
        currentUserServiceDescriptor!.Lifetime.Should()
            .Be(ServiceLifetime.Scoped, "Current user service should be scoped");

        // Verify authentication services are registered
        passwordServiceDescriptor.Should().NotBeNull();
        passwordServiceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped, "Password service should be scoped");

        demoUserServiceDescriptor.Should().NotBeNull();
        demoUserServiceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped, "Demo user service should be scoped");

        _output.WriteLine("Service lifetimes are correctly configured including authentication services");
    }

    [Fact]
    public void InfrastructureOptions_ShouldHaveValidDefaults()
    {
        // Arrange & Act
        var options = new InfrastructureOptions();

        // Assert
        options.Database.Should().NotBeNull();
        options.Database.EnableRetry.Should().BeTrue();
        options.Database.CommandTimeoutSeconds.Should().Be(30);
        options.Database.MaxRetryCount.Should().Be(3);

        options.Email.Should().NotBeNull();
        options.Email.Provider.Should().Be("Mock");
        options.Email.DefaultSender.Should().Be("noreply@rentalrepairs.com");
        options.Email.EnableEmailNotifications.Should().BeTrue();

        options.Authentication.Should().NotBeNull();
        options.Authentication.TokenExpirationHours.Should().Be(8);
        options.Authentication.MaxLoginAttempts.Should().Be(5);

        options.Performance.Should().NotBeNull();
        options.Performance.EnableCaching.Should().BeTrue();
        options.Performance.CacheProvider.Should().Be("Memory");

        _output.WriteLine("Infrastructure options have sensible defaults");
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterEnvironmentSpecificServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "test-connection",
                ["DemoAuthentication:EnableDemoMode"] = "true"
            })
            .Build();

        var services = new ServiceCollection();

        // Test Development Environment
        var devEnvironment = new MockWebHostEnvironment { EnvironmentName = "Development" };
        services.AddInfrastructure(configuration, devEnvironment);

        // Act & Assert - Check that core services are registered
        var contextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IApplicationDbContext));
        var passwordServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPasswordService));

        contextDescriptor.Should().NotBeNull("Database context should be registered");
        passwordServiceDescriptor.Should().NotBeNull("Password service should be registered");

        _output.WriteLine("Environment-specific services are registered correctly");
    }

    /// <summary>
    /// Mock web host environment for testing that implements IWebHostEnvironment
    /// </summary>
    private class MockWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "TestApp";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Development";
    }
}