using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using RentalRepairs.Infrastructure.Authentication.Services;
using RentalRepairs.Infrastructure.Authentication.Models;
using Microsoft.Extensions.Options;

namespace RentalRepairs.Infrastructure.Tests.Authentication;

/// <summary>
/// Test to verify the secure authentication system is working properly
/// </summary>
public class SecureAuthenticationSystemTests
{
    private readonly IHostEnvironment _mockEnvironment;

    public SecureAuthenticationSystemTests()
    {
        // Create a simple test environment
        _mockEnvironment = new TestHostEnvironment();
    }

    [Fact]
    public async Task PasswordService_Should_Hash_And_Verify_Passwords_Correctly()
    {
        // Arrange
        var passwordService = new PasswordService(_mockEnvironment);
        const string originalPassword = "Demo123!";

        // Act
        var hashedPassword = passwordService.HashPassword(originalPassword);
        var isValid = passwordService.VerifyPassword(originalPassword, hashedPassword);
        var isInvalid = passwordService.VerifyPassword("WrongPassword", hashedPassword);

        // Assert
        Assert.NotEqual(originalPassword, hashedPassword);
        Assert.True(hashedPassword.StartsWith("$2a$"));
        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public async Task DemoUserService_Should_Initialize_Default_Users()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DemoAuthentication:EnableDemoMode", "true"),
                new KeyValuePair<string, string>("DemoAuthentication:DefaultPassword", "Demo123!"),
                new KeyValuePair<string, string>("DemoAuthentication:Security:MaxLoginAttempts", "5"),
                new KeyValuePair<string, string>("DemoAuthentication:Security:LockoutDurationMinutes", "15"),
            })
            .Build();

        var demoSettings = new DemoAuthenticationSettings();
        configuration.GetSection("DemoAuthentication").Bind(demoSettings);

        var options = Options.Create(demoSettings);
        var passwordService = new PasswordService(_mockEnvironment);
        var logger = new TestLogger<DemoUserService>();

        var demoUserService = new DemoUserService(options, passwordService, logger);

        // Act
        await demoUserService.InitializeDemoUsersAsync();
        var users = await demoUserService.GetDemoUsersForDisplayAsync();

        // Assert
        Assert.True(demoUserService.IsDemoModeEnabled());
        Assert.True(users.Count > 0);
        
        var adminUser = users.FirstOrDefault(u => u.Email == "admin@demo.com");
        Assert.NotNull(adminUser);
        Assert.Contains("SystemAdmin", adminUser.Roles);
        
        // Fix: Use correct email pattern that matches DemoUserService
        var tenantUser = users.FirstOrDefault(u => u.Email == "tenant1.unit101@sunset.com");
        Assert.NotNull(tenantUser);
        Assert.Contains("Tenant", tenantUser.Roles);
    }

    [Fact]
    public async Task DemoUserService_Should_Validate_User_Credentials()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DemoAuthentication:EnableDemoMode", "true"),
                new KeyValuePair<string, string>("DemoAuthentication:DefaultPassword", "Demo123!"),
            })
            .Build();

        var demoSettings = new DemoAuthenticationSettings();
        configuration.GetSection("DemoAuthentication").Bind(demoSettings);

        var options = Options.Create(demoSettings);
        var passwordService = new PasswordService(_mockEnvironment);
        var logger = new TestLogger<DemoUserService>();

        var demoUserService = new DemoUserService(options, passwordService, logger);
        await demoUserService.InitializeDemoUsersAsync();

        // Act
        var validResult = await demoUserService.ValidateUserAsync("admin@demo.com", "Demo123!");
        var invalidResult = await demoUserService.ValidateUserAsync("admin@demo.com", "WrongPassword");

        // Assert
        Assert.True(validResult.IsValid);
        Assert.NotNull(validResult.User);
        Assert.Equal("admin@demo.com", validResult.User.Email);

        Assert.False(invalidResult.IsValid);
        Assert.Null(invalidResult.User);
        Assert.Contains("Invalid email or password", invalidResult.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticationService_Should_Authenticate_Demo_Users()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DemoAuthentication:EnableDemoMode", "true"),
                new KeyValuePair<string, string>("DemoAuthentication:DefaultPassword", "Demo123!"),
            })
            .Build();

        var demoSettings = new DemoAuthenticationSettings();
        configuration.GetSection("DemoAuthentication").Bind(demoSettings);

        var options = Options.Create(demoSettings);
        var passwordService = new PasswordService(_mockEnvironment);
        var demoUserLogger = new TestLogger<DemoUserService>();
        var authLogger = new TestLogger<RentalRepairs.Infrastructure.Authentication.AuthenticationService>();

        var demoUserService = new DemoUserService(options, passwordService, demoUserLogger);
        await demoUserService.InitializeDemoUsersAsync();

        var authService = new RentalRepairs.Infrastructure.Authentication.AuthenticationService(
            authLogger,
            demoUserService,
            null, // propertyService - not needed for demo
            null, // workerService - not needed for demo  
            null  // httpContextAccessor - not needed for test
        );

        // Act - Test unified authentication
        var result = await authService.AuthenticateAsync("admin@demo.com", "Demo123!");

        // Assert - Updated for unified authentication system
        Assert.True(result.IsSuccess);
        Assert.Equal("admin@demo.com", result.Email);
        Assert.Contains("SystemAdmin", result.Roles);
        Assert.Equal("SystemAdmin", result.PrimaryRole);
        Assert.Equal("/", result.DashboardUrl); // SystemAdmin uses the unified Index dashboard
        
        // Verify that the result has the expected structure for unified authentication
        Assert.NotNull(result.Token);
        Assert.NotEqual(default(DateTime), result.ExpiresAt);
        Assert.NotNull(result.DisplayName);
    }

    [Fact]
    public async Task AuthenticationService_Should_Handle_Tenant_Authentication_With_Claims()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DemoAuthentication:EnableDemoMode", "true"),
                new KeyValuePair<string, string>("DemoAuthentication:DefaultPassword", "Demo123!"),
            })
            .Build();

        var demoSettings = new DemoAuthenticationSettings();
        configuration.GetSection("DemoAuthentication").Bind(demoSettings);

        var options = Options.Create(demoSettings);
        var passwordService = new PasswordService(_mockEnvironment);
        var demoUserLogger = new TestLogger<DemoUserService>();
        var authLogger = new TestLogger<RentalRepairs.Infrastructure.Authentication.AuthenticationService>();

        var demoUserService = new DemoUserService(options, passwordService, demoUserLogger);
        await demoUserService.InitializeDemoUsersAsync();

        var authService = new RentalRepairs.Infrastructure.Authentication.AuthenticationService(
            authLogger,
            demoUserService,
            null, null, null
        );

        // Act - Test tenant authentication with unified system - Fix: Use correct email pattern
        var result = await authService.AuthenticateAsync("tenant1.unit101@sunset.com", "Demo123!");

        // Assert - Updated for unified dashboard system - Tenant now uses unified dashboard
        Assert.True(result.IsSuccess);
        Assert.Equal("tenant1.unit101@sunset.com", result.Email);
        Assert.Contains("Tenant", result.Roles);
        Assert.Equal("Tenant", result.PrimaryRole);
        Assert.Equal("/", result.DashboardUrl); // Updated: Tenant uses unified dashboard now
        
        // Verify tenant-specific parameters are extracted
        Assert.Equal("SUN001", result.PropertyCode);
        Assert.Equal("101", result.UnitNumber);
        Assert.Equal("Sunset Apartments", result.PropertyName);
    }

    [Fact]
    public async Task AuthenticationService_Should_Handle_Worker_Authentication_With_Claims()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DemoAuthentication:EnableDemoMode", "true"),
                new KeyValuePair<string, string>("DemoAuthentication:DefaultPassword", "Demo123!"),
            })
            .Build();

        var demoSettings = new DemoAuthenticationSettings();
        configuration.GetSection("DemoAuthentication").Bind(demoSettings);

        var options = Options.Create(demoSettings);
        var passwordService = new PasswordService(_mockEnvironment);
        var demoUserLogger = new TestLogger<DemoUserService>();
        var authLogger = new TestLogger<RentalRepairs.Infrastructure.Authentication.AuthenticationService>();

        var demoUserService = new DemoUserService(options, passwordService, demoUserLogger);
        await demoUserService.InitializeDemoUsersAsync();

        var authService = new RentalRepairs.Infrastructure.Authentication.AuthenticationService(
            authLogger,
            demoUserService,
            null, null, null
        );

        // Act - Fix: Use correct email address that exists in DemoUserService
        var result = await authService.AuthenticateAsync("plumber.smith@workers.com", "Demo123!");

        // Assert - Updated for unified dashboard system - Worker now uses unified dashboard
        Assert.True(result.IsSuccess);
        Assert.Equal("plumber.smith@workers.com", result.Email);
        Assert.Contains("Worker", result.Roles);
        Assert.Equal("Worker", result.PrimaryRole);
        Assert.Equal("/", result.DashboardUrl); // Updated: Worker uses unified dashboard now
        
        // Verify worker-specific parameters are extracted - Fix: Use correct specialization
        Assert.Equal("Plumber", result.WorkerSpecialization);
    }

    [Fact]
    public async Task AuthenticationService_Should_Fail_With_Invalid_Credentials()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DemoAuthentication:EnableDemoMode", "true"),
                new KeyValuePair<string, string>("DemoAuthentication:DefaultPassword", "Demo123!"),
            })
            .Build();

        var demoSettings = new DemoAuthenticationSettings();
        configuration.GetSection("DemoAuthentication").Bind(demoSettings);

        var options = Options.Create(demoSettings);
        var passwordService = new PasswordService(_mockEnvironment);
        var demoUserLogger = new TestLogger<DemoUserService>();
        var authLogger = new TestLogger<RentalRepairs.Infrastructure.Authentication.AuthenticationService>();

        var demoUserService = new DemoUserService(options, passwordService, demoUserLogger);
        await demoUserService.InitializeDemoUsersAsync();

        var authService = new RentalRepairs.Infrastructure.Authentication.AuthenticationService(
            authLogger,
            demoUserService,
            null, null, null
        );

        // Act
        var result = await authService.AuthenticateAsync("invalid@demo.com", "WrongPassword");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid", result.ErrorMessage);
    }
}

/// <summary>
/// Simple test host environment implementation
/// </summary>
public class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "TestApp";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;

    public bool IsDevelopment() => EnvironmentName == "Development";
    public bool IsProduction() => EnvironmentName == "Production";
    public bool IsStaging() => EnvironmentName == "Staging";
    public bool IsEnvironment(string environmentName) => EnvironmentName == environmentName;
}

/// <summary>
/// Simple test logger for unit tests
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => null!;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Log to console for debugging if needed
        // Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}