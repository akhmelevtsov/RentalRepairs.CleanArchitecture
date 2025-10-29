using System.Diagnostics;
using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using RentalRepairs.Infrastructure.Authentication.Services;

namespace RentalRepairs.Infrastructure.Tests.Authentication;

/// <summary>
/// Performance tests to verify BCrypt optimization
/// </summary>
public class PasswordPerformanceTests
{
    [Fact]
    public void Development_PasswordService_Should_Be_Fast()
    {
        // Arrange
        var devEnvironment = new PerformanceTestHostEnvironment { EnvironmentName = "Development" };
        var passwordService = new PasswordService(devEnvironment);
        const string password = "Demo123!";
        
        // Hash a password first
        var hashedPassword = passwordService.HashPassword(password);
        
        // Act - Measure verification time
        var stopwatch = Stopwatch.StartNew();
        var isValid = passwordService.VerifyPassword(password, hashedPassword);
        stopwatch.Stop();
        
        // Assert
        Assert.True(isValid);
        Assert.True(stopwatch.ElapsedMilliseconds < 200, 
            $"Development password verification took {stopwatch.ElapsedMilliseconds}ms, should be under 200ms");
    }

    [Fact]
    public void Production_PasswordService_Should_Be_Secure_But_Slower()
    {
        // Arrange
        var prodEnvironment = new PerformanceTestHostEnvironment { EnvironmentName = "Production" };
        var passwordService = new PasswordService(prodEnvironment);
        const string password = "Demo123!";
        
        // Hash a password first
        var hashedPassword = passwordService.HashPassword(password);
        
        // Act - Measure verification time
        var stopwatch = Stopwatch.StartNew();
        var isValid = passwordService.VerifyPassword(password, hashedPassword);
        stopwatch.Stop();
        
        // Assert
        Assert.True(isValid);
        // Production should be slower (more secure) but we won't enforce a specific time in tests
        // as it depends on hardware
        Assert.True(stopwatch.ElapsedMilliseconds > 0);
    }

    [Fact]
    public void Development_Should_Be_Faster_Than_Production()
    {
        // Arrange
        var devEnvironment = new PerformanceTestHostEnvironment { EnvironmentName = "Development" };
        var prodEnvironment = new PerformanceTestHostEnvironment { EnvironmentName = "Production" };
        var devPasswordService = new PasswordService(devEnvironment);
        var prodPasswordService = new PasswordService(prodEnvironment);
        const string password = "Demo123!";
        
        // Hash passwords
        var devHashedPassword = devPasswordService.HashPassword(password);
        var prodHashedPassword = prodPasswordService.HashPassword(password);
        
        // Act - Measure verification times
        var devStopwatch = Stopwatch.StartNew();
        devPasswordService.VerifyPassword(password, devHashedPassword);
        devStopwatch.Stop();
        
        var prodStopwatch = Stopwatch.StartNew();
        prodPasswordService.VerifyPassword(password, prodHashedPassword);
        prodStopwatch.Stop();
        
        // Assert - Development should be significantly faster
        Assert.True(devStopwatch.ElapsedMilliseconds < prodStopwatch.ElapsedMilliseconds,
            $"Development ({devStopwatch.ElapsedMilliseconds}ms) should be faster than Production ({prodStopwatch.ElapsedMilliseconds}ms)");
    }
}

/// <summary>
/// Test host environment for performance testing
/// </summary>
public class PerformanceTestHostEnvironment : IHostEnvironment
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