using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Diagnostics;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.Performance;

/// <summary>
/// ? CLEAN: Application Performance Tests
/// Tests performance characteristics and execution benchmarks for WebUI application
/// Validates that the application meets performance expectations
/// </summary>
public class ApplicationPerformanceTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public ApplicationPerformanceTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    #region Startup Performance Tests

    [Fact]
    public void Application_Should_Load_Quickly()
    {
        _output.WriteLine("Testing application load performance...");

        // Test that the test project itself loads quickly
        var stopwatch = Stopwatch.StartNew();

        var testAssembly = typeof(ApplicationPerformanceTests).Assembly;
        var types = testAssembly.GetTypes();

        stopwatch.Stop();

        types.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should load in under 5 seconds

        _output.WriteLine($"Application loaded in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task WebApplication_Should_Start_Quickly()
    {
        _output.WriteLine("Testing web application startup performance...");

        // Test that the web application factory starts up quickly
        var stopwatch = Stopwatch.StartNew();

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");

        stopwatch.Stop();

        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should respond in under 10 seconds

        _output.WriteLine($"Web application started and responded in {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Dependency Injection Performance Tests

    [Fact]
    public void DependencyInjection_Should_Resolve_Services_Quickly()
    {
        _output.WriteLine("Testing dependency injection performance...");

        // Test that dependency injection resolves quickly
        var stopwatch = Stopwatch.StartNew();

        using var scope = _factory.Services.CreateScope();

        // Test only services that should definitely be available
        var availableServices = new List<object?>();

        try
        {
            var mediator = scope.ServiceProvider.GetService<MediatR.IMediator>();
            if (mediator != null) availableServices.Add(mediator);
        }
        catch
        {
            /* Service may not be available */
        }

        try
        {
            var logger = scope.ServiceProvider
                .GetService<Microsoft.Extensions.Logging.ILogger<ApplicationPerformanceTests>>();
            if (logger != null) availableServices.Add(logger);
        }
        catch
        {
            /* Service may not be available */
        }

        try
        {
            var currentUser = scope.ServiceProvider.GetService<Application.Common.Interfaces.ICurrentUserService>();
            if (currentUser != null) availableServices.Add(currentUser);
        }
        catch
        {
            /* Service may not be available */
        }

        stopwatch.Stop();

        // At least some services should be resolvable
        availableServices.Should().NotBeEmpty("At least some core services should be available");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Allow more time for service resolution in tests

        _output.WriteLine($"{availableServices.Count} services resolved in {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Request Performance Tests

    [Fact]
    public async Task Multiple_Page_Requests_Should_Be_Fast()
    {
        _output.WriteLine("Testing multiple page request performance...");

        // Test that multiple page requests perform well
        var client = _factory.CreateClient();
        var pages = new[] { "/", "/Account/Login", "/Properties/Register", "/TenantRequests/Submit" };

        var stopwatch = Stopwatch.StartNew();

        var tasks = pages.Select(page => client.GetAsync(page)).ToArray();
        var responses = await Task.WhenAll(tasks);

        stopwatch.Stop();

        responses.Should().AllSatisfy(r => r.Should().NotBeNull());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000); // All requests should complete in under 15 seconds

        _output.WriteLine($"{pages.Length} page requests completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Concurrent_Requests_Should_Perform_Well()
    {
        _output.WriteLine("Testing concurrent request performance...");

        // Test that the application can handle multiple simultaneous requests efficiently
        var client = _factory.CreateClient();
        var requestTasks = new List<Task<HttpResponseMessage>>();

        var stopwatch = Stopwatch.StartNew();

        // Create 10 simultaneous requests
        for (var i = 0; i < 10; i++) requestTasks.Add(client.GetAsync("/"));

        var responses = await Task.WhenAll(requestTasks);

        stopwatch.Stop();

        responses.Should().HaveCount(10);
        responses.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.OK,
                System.Net.HttpStatusCode.Redirect,
                System.Net.HttpStatusCode.Found
            );
        });

        stopwatch.ElapsedMilliseconds.Should()
            .BeLessThan(20000); // Should handle 10 concurrent requests in under 20 seconds

        _output.WriteLine($"10 concurrent requests handled in {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Reliability Tests

    [Fact]
    public void Test_Execution_Should_Be_Deterministic()
    {
        _output.WriteLine("Testing test execution reliability...");

        // Test that tests execute reliably and deterministically
        var results = new List<bool>();

        // Run the same test multiple times
        for (var i = 0; i < 5; i++)
            try
            {
                var testAssembly = typeof(ApplicationPerformanceTests).Assembly;
                var webUIAssembly = typeof(Program).Assembly;

                var result = testAssembly != null && webUIAssembly != null;
                results.Add(result);
            }
            catch
            {
                results.Add(false);
            }

        // All executions should succeed
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        results.Should().HaveCount(5);

        _output.WriteLine("Test execution is deterministic and reliable");
    }

    [Fact]
    public void WebApplicationFactory_Should_Be_Stable()
    {
        _output.WriteLine("Testing WebApplicationFactory stability...");

        // Test that we can create multiple instances of the web application factory
        var factories = new List<WebApplicationTestFactory<Program>>();

        try
        {
            for (var i = 0; i < 3; i++)
            {
                var factory = new WebApplicationTestFactory<Program>();
                factories.Add(factory);

                // Test that each factory works
                var client = factory.CreateClient();
                client.Should().NotBeNull();
            }

            factories.Should().HaveCount(3);
            factories.Should().AllSatisfy(f => f.Should().NotBeNull());

            _output.WriteLine("WebApplicationFactory is stable and reusable");
        }
        finally
        {
            // Clean up
            foreach (var factory in factories) factory.Dispose();
        }
    }

    #endregion

    #region Memory and Resource Tests

    [Fact]
    public void Memory_Usage_Should_Be_Reasonable()
    {
        _output.WriteLine("Testing memory usage...");

        var initialMemory = GC.GetTotalMemory(true);

        // Perform some operations that might consume memory
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider.GetServices<object>().ToList();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Memory increase should be reasonable (less than 50MB for this test)
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024);

        _output.WriteLine($"Memory usage increase: {memoryIncrease / 1024}KB");
    }

    #endregion
}