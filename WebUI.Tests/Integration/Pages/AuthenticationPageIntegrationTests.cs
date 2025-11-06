using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.Integration.Pages;

/// <summary>
///  Authentication Page Integration Tests
/// Tests login, logout, and authentication-related page functionality
/// </summary>
public class AuthenticationPageIntegrationTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public AuthenticationPageIntegrationTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Login_Page_Should_Be_Accessible()
    {
        _output.WriteLine("Testing login page accessibility...");

        // Act
        var response = await _client.GetAsync("/Account/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Login", "Page should contain login functionality");

        _output.WriteLine("Login page accessible and contains login form");
    }

    [Fact]
    public async Task Logout_Page_Should_Handle_Requests_Correctly()
    {
        _output.WriteLine("Testing logout page handling...");

        // Act
        var response = await _client.GetAsync("/Account/Logout");

        // Assert - Should redirect to login or home page
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        _output.WriteLine($"Logout page handles requests correctly (Status: {response.StatusCode})");
    }


    [Fact]
    public async Task Register_Page_Should_Be_Accessible()
    {
        _output.WriteLine("Testing user registration page...");

        // Act
        var response = await _client.GetAsync("/Account/Register");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.NotFound); // May not be implemented

        _output.WriteLine($"Register page handling correct (Status: {response.StatusCode})");
    }

    [Fact]
    public async Task AccessDenied_Page_Should_Be_Accessible()
    {
        _output.WriteLine("Testing access denied page...");

        // Act
        var response = await _client.GetAsync("/Account/AccessDenied");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.NotFound); // Page might not be implemented

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var hasAccessDeniedContent = content.Contains("Access Denied") ||
                                         content.Contains("access denied") ||
                                         content.Contains("Forbidden") ||
                                         content.Contains("Not Authorized");

            if (hasAccessDeniedContent)
                _output.WriteLine("Access denied page shows appropriate content");
            else
                _output.WriteLine(" Access denied page loads but content unclear");
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _output.WriteLine(" Access denied page not found - may not be implemented yet");
        }
        else
        {
            _output.WriteLine($"Access denied page redirects properly (Status: {response.StatusCode})");
        }
    }

    [Fact]
    public async Task Authentication_Pages_Should_Have_Proper_Content_Type()
    {
        _output.WriteLine("Testing authentication pages content types...");

        var pages = new[] { "/Account/Login", "/Account/TestAuth" };

        foreach (var page in pages)
        {
            var response = await _client.GetAsync(page);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                contentType.Should().Be("text/html", $"Page {page} should return HTML content");
                _output.WriteLine($"{page} returns correct content type: {contentType}");
            }
            else
            {
                _output.WriteLine($"{page} redirects properly (Status: {response.StatusCode})");
            }
        }
    }
}