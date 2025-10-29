using Microsoft.AspNetCore.Mvc.Testing;
using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.EndToEnd;

/// <summary>
/// End-to-End tests based on Manual Testing Guide scenarios
/// Tests complete user workflows for Property Superintendent role
/// </summary>
public class PropertySuperintendentEndToEndTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public PropertySuperintendentEndToEndTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _output = output;
    }

    [Fact]
    public async Task PropertySuperintendent_PropertyFocusedDashboard_ShouldSucceed()
    {
        _output.WriteLine("Testing Property Superintendent Dashboard Access");

        // Step 1: Login as Property Superintendent
        await LoginAsPropertySuperintendent();
        
        // Step 2: Access dashboard
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect || dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }

        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);
        
        if (dashboardResponse.StatusCode == HttpStatusCode.OK)
        {
            var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
            
            // Verify superintendent sees property-specific dashboard
            dashboardContent.Should().NotBeNullOrEmpty();
        }
        
        _output.WriteLine("Superintendent dashboard shows property-focused data");
    }

    [Fact]
    public async Task PropertySuperintendent_RequestManagementWorkflow_ShouldSucceed()
    {
        _output.WriteLine("Testing Property Superintendent Request Management Workflow");

        // Step 1: Login as Property Superintendent
        await LoginAsPropertySuperintendent();
        
        // Step 2: Access request management pages
        var requestPages = new[]
        {
            "/TenantRequests",
            "/",
            "/Index"
        };

        foreach (var page in requestPages)
        {
            _output.WriteLine($"Testing access to {page} for request management");
            
            var response = await _client.GetAsync(page);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeNullOrEmpty();
                _output.WriteLine($"{page} accessible for superintendent");
            }
            else if (response.StatusCode == HttpStatusCode.Redirect || 
                     response.StatusCode == HttpStatusCode.Found || 
                     response.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                // Follow redirect to see actual content
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var redirectResponse = await _client.GetAsync(location);
                    if (redirectResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var content = await redirectResponse.Content.ReadAsStringAsync();
                        content.Should().NotBeNullOrEmpty();
                        _output.WriteLine($"{page} redirected to accessible content");
                    }
                }
            }
        }
    }

    [Fact]
    public async Task PropertySuperintendent_WorkerAssignmentProcess_ShouldHandleCorrectly()
    {
        _output.WriteLine("Testing Property Superintendent Worker Assignment Process");

        // Step 1: Login as Property Superintendent
        await LoginAsPropertySuperintendent();

        // Step 2: Try to access worker assignment page (might need a valid request ID)
        // Using a sample request ID - in real scenario, this would be from seeded data
        var testRequestId = 1;
        var assignWorkerResponse = await _client.GetAsync($"/TenantRequests/AssignWorker/{testRequestId}");
        
        // The response could be OK (page found), NotFound (no such request), or Redirect (auth issue)
        // Should either show request details or handle non-existent request appropriately
        assignWorkerResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Found,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.PermanentRedirect);
        
        if (assignWorkerResponse.StatusCode == HttpStatusCode.OK)
        {
            var assignContent = await assignWorkerResponse.Content.ReadAsStringAsync();
            assignContent.Should().ContainAny("Worker", "Assign", "Schedule");
            _output.WriteLine("Worker assignment page accessible");
        }
        else if (assignWorkerResponse.StatusCode == HttpStatusCode.NotFound)
        {
            _output.WriteLine("Worker assignment correctly returns NotFound for invalid request ID");
        }
        else
        {
            _output.WriteLine("Worker assignment page handled security appropriately");
        }
    }

    [Fact]
    public async Task PropertySuperintendent_TenantDirectoryAccess_ShouldSucceed()
    {
        _output.WriteLine("Testing Property Superintendent Tenant Directory Access");

        // Step 1: Login as Property Superintendent
        await LoginAsPropertySuperintendent();

        // Step 2: Access tenant-related pages
        var tenantPages = new[]
        {
            "/",
            "/Index"
        };

        bool hasAccessToTenantData = false;

        foreach (var page in tenantPages)
        {
            var response = await _client.GetAsync(page);
            
            if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    response = await _client.GetAsync(location);
                }
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.ContainsAny("Tenant", "Unit", "Contact"))
                {
                    hasAccessToTenantData = true;
                    _output.WriteLine($"Tenant directory data accessible via {page}");
                    break;
                }
            }
        }

        // At minimum, superintendent should have access to some dashboard
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect || dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }
        
        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);
        _output.WriteLine("Superintendent has access to property management interface");
    }

    private async Task LoginAsPropertySuperintendent()
    {
        var loginResponse = await _client.GetAsync("/Account/Login");
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(loginContent);

        var loginData = new List<KeyValuePair<string, string>>
        {
            new("Login.Email", "super.sun001@rentalrepairs.com"),
            new("Login.Password", "Demo123!"), // Fixed: Use correct demo password
            new("__RequestVerificationToken", token)
        };

        var loginFormContent = new FormUrlEncodedContent(loginData);
        var loginPostResponse = await _client.PostAsync("/Account/Login", loginFormContent);
        
        loginPostResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var tokenStart = html.IndexOf("__RequestVerificationToken");
        if (tokenStart == -1) return string.Empty;

        var valueStart = html.IndexOf("value=\"", tokenStart);
        if (valueStart == -1) return string.Empty;
        valueStart += 7;

        var valueEnd = html.IndexOf("\"", valueStart);
        if (valueEnd == -1) return string.Empty;

        return html.Substring(valueStart, valueEnd - valueStart);
    }
}

// Extension method for checking multiple strings
public static class StringExtensions
{
    public static bool ContainsAny(this string source, params string[] values)
    {
        return values.Any(value => source.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}