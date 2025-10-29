using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.Integration.Pages;

/// <summary>
/// ? CLEAN: Property Page Integration Tests
/// Tests property registration and property-related page functionality
/// </summary>
public class PropertyPageIntegrationTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public PropertyPageIntegrationTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Property_Register_Page_Should_Be_Accessible()
    {
        _output.WriteLine("Testing property registration page...");

        // Act
        var response = await _client.GetAsync("/Properties/Register");

        // Assert - May redirect to login if authentication is required
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("html", "Should return HTML content");
            _output.WriteLine("Property registration page returns content");
        }
        else
        {
            _output.WriteLine($"Property registration page redirects (Status: {response.StatusCode})");
        }
    }

    [Fact]
    public async Task Property_Pages_Should_Handle_Authentication_Correctly()
    {
        _output.WriteLine("Testing property page authentication handling...");

        // Arrange - Client without auto-redirect
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Properties/Register");

        // Assert
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
            location.Should().NotBeNull("Redirect should have a location");
            _output.WriteLine($"Redirects to authentication: {location}");
        }
        else
        {
            _output.WriteLine("Property page accessible without authentication");
        }
    }
}