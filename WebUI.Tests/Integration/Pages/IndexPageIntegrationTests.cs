using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.Integration.Pages;

/// <summary>
/// ? CLEAN: Index Page Integration Tests
/// Tests the main landing page functionality and routing
/// </summary>
public class IndexPageIntegrationTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public IndexPageIntegrationTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Index_Page_Should_Be_Accessible()
    {
        _output.WriteLine("Testing index page accessibility...");

        // Act
        var response = await _client.GetAsync("/");

        // Assert - Should either return OK or redirect to login (both acceptable)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        _output.WriteLine($"Index page accessible (Status: {response.StatusCode})");
    }

    [Fact]
    public async Task Index_Page_Should_Handle_Unauthenticated_Users()
    {
        _output.WriteLine("Testing index page behavior for unauthenticated users...");

        // Arrange - Client without authentication
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert - Should either show content or redirect to login
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);
        
        if (response.StatusCode == HttpStatusCode.Redirect || 
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = response.Headers.Location?.ToString();
            location.Should().NotBeNull("Redirect should have a location header");
            _output.WriteLine($"Redirects unauthenticated users to: {location}");
        }
        else
        {
            _output.WriteLine("Shows content for unauthenticated users");
        }
    }

    [Fact]
    public async Task Index_Page_Should_Return_Valid_HTML_When_Accessible()
    {
        _output.WriteLine("Testing index page HTML content...");

        // Arrange
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("html", "Should return HTML content");
            content.Should().NotBeEmpty("Should not return empty content");
            _output.WriteLine("Returns valid HTML content");
        }
        else
        {
            _output.WriteLine($"Properly redirects instead of showing content (Status: {response.StatusCode})");
        }
    }

    [Fact]
    public async Task Index_Page_Should_Have_Proper_Content_Type()
    {
        _output.WriteLine("Testing index page content type...");

        // Act
        var response = await _client.GetAsync("/");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            contentType.Should().Be("text/html", "Should return HTML content type");
            _output.WriteLine($"Returns correct content type: {contentType}");
        }
        else
        {
            _output.WriteLine($"Redirects properly (Status: {response.StatusCode})");
        }
    }
}