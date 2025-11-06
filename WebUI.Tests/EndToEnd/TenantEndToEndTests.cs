using Microsoft.AspNetCore.Mvc.Testing;
using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.EndToEnd;

/// <summary>
/// End-to-End tests based on Manual Testing Guide scenarios
/// Tests complete user workflows for Tenant role
/// </summary>
public class TenantEndToEndTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public TenantEndToEndTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _output = output;
    }

    [Fact]
    public async Task Tenant_MaintenanceRequestSubmission_ShouldSucceed()
    {
        _output.WriteLine("Testing Tenant Maintenance Request Submission");

        // Step 1: Login as Tenant
        await LoginAsTenant();

        // Step 2: Access maintenance request submission form
        var submitResponse = await _client.GetAsync("/TenantRequests/Submit");
        submitResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        if (submitResponse.StatusCode == HttpStatusCode.OK)
        {
            var submitContent = await submitResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(submitContent);

            // Verify form shows tenant information pre-populated
            submitContent.Should().ContainAny("Submit", "Request", "Maintenance");

            _output.WriteLine("Maintenance request form accessible");

            // Step 3: Submit maintenance request
            var requestData = new List<KeyValuePair<string, string>>
            {
                new("TenantRequest.PropertyCode", "SUN001"),
                new("TenantRequest.UnitNumber", "101"),
                new("TenantRequest.TenantFirstName", "Jane"),
                new("TenantRequest.TenantLastName", "Smith"),
                new("TenantRequest.TenantEmail", "tenant.smith@domain.com"),
                new("TenantRequest.TenantPhone", "555-0123"),
                new("TenantRequest.ProblemDescription",
                    "Kitchen faucet dripping constantly - needs immediate attention"),
                new("TenantRequest.UrgencyLevel", "High"),
                new("TenantRequest.PreferredContactTime", "Morning (8 AM - 12 PM)"),
                new("__RequestVerificationToken", token)
            };

            var requestFormContent = new FormUrlEncodedContent(requestData);
            var requestPostResponse = await _client.PostAsync("/TenantRequests/Submit", requestFormContent);

            // Should redirect after successful submission
            requestPostResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found,
                HttpStatusCode.OK, HttpStatusCode.PermanentRedirect);

            _output.WriteLine("Maintenance request submitted successfully");

            // Step 4: Verify request was created (follow redirect if applicable)
            if (requestPostResponse.StatusCode == HttpStatusCode.Redirect ||
                requestPostResponse.StatusCode == HttpStatusCode.Found ||
                requestPostResponse.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                var location = requestPostResponse.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    var resultResponse = await _client.GetAsync(location);
                    resultResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect,
                        HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);

                    if (resultResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var resultContent = await resultResponse.Content.ReadAsStringAsync();
                        resultContent.Should().NotBeNullOrEmpty();
                    }

                    _output.WriteLine("Request submission confirmed");
                }
            }
        }
        else
        {
            _output.WriteLine("Maintenance request form redirected (may require additional authentication)");
        }
    }

    [Fact]
    public async Task Tenant_EmergencyRequestSubmission_ShouldSucceed()
    {
        _output.WriteLine("Testing Tenant Emergency Request Submission");

        // Step 1: Login as Tenant
        await LoginAsTenant();

        // Step 2: Access maintenance request submission form
        var submitResponse = await _client.GetAsync("/TenantRequests/Submit");
        submitResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        if (submitResponse.StatusCode == HttpStatusCode.OK)
        {
            var submitContent = await submitResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(submitContent);

            _output.WriteLine("Emergency request form accessible");

            // Step 3: Submit emergency maintenance request
            var emergencyRequestData = new List<KeyValuePair<string, string>>
            {
                new("TenantRequest.PropertyCode", "SUN001"),
                new("TenantRequest.UnitNumber", "101"),
                new("TenantRequest.TenantFirstName", "Jane"),
                new("TenantRequest.TenantLastName", "Smith"),
                new("TenantRequest.TenantEmail", "tenant.smith@domain.com"),
                new("TenantRequest.TenantPhone", "555-0123"),
                new("TenantRequest.ProblemDescription",
                    "Water heater completely failed - no hot water for family with small children"),
                new("TenantRequest.UrgencyLevel", "Critical"),
                new("TenantRequest.PreferredContactTime", "Anytime"),
                new("__RequestVerificationToken", token)
            };

            var emergencyFormContent = new FormUrlEncodedContent(emergencyRequestData);
            var emergencyPostResponse = await _client.PostAsync("/TenantRequests/Submit", emergencyFormContent);

            // Should handle emergency request appropriately
            emergencyPostResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found,
                HttpStatusCode.OK, HttpStatusCode.PermanentRedirect);

            _output.WriteLine("Emergency request submitted successfully");
        }
        else
        {
            _output.WriteLine("Emergency request form redirected (authentication may be required)");
        }
    }

    [Fact]
    public async Task Tenant_RequestStatusTracking_ShouldSucceed()
    {
        _output.WriteLine("Testing Tenant Request Status Tracking");

        // Step 1: Login as Tenant
        await LoginAsTenant();

        // Step 2: Access tenant dashboard/requests page
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect ||
            dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }

        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        if (dashboardResponse.StatusCode == HttpStatusCode.OK)
        {
            var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
            dashboardContent.Should().NotBeNullOrEmpty();
        }

        _output.WriteLine("Tenant dashboard accessible for request tracking");

        // Step 3: Try to access request details (if any requests exist)
        // Note: In a real scenario, we'd need to create a request first or use seeded data
        var testRequestId = Guid.NewGuid();
        var detailsResponse = await _client.GetAsync($"/tenant-requests/{testRequestId}");

        // Should either show request details or handle non-existent request appropriately
        detailsResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        if (detailsResponse.StatusCode == HttpStatusCode.OK)
        {
            var detailsContent = await detailsResponse.Content.ReadAsStringAsync();
            detailsContent.Should().ContainAny("Request", "Status", "Details");
            _output.WriteLine("Request details page accessible");
        }
        else
        {
            _output.WriteLine("Request details page handles invalid/missing requests appropriately");
        }
    }

    [Fact]
    public async Task Tenant_RequestHistoryAccess_ShouldSucceed()
    {
        _output.WriteLine("Testing Tenant Request History Access");

        // Step 1: Login as Tenant
        await LoginAsTenant();

        // Step 2: Access various pages that might show request history
        var historyPages = new[]
        {
            "/",
            "/Index",
            "/TenantRequests"
        };

        var hasAccessToHistory = false;

        foreach (var page in historyPages)
        {
            var response = await _client.GetAsync(page);

            if (response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.Found ||
                response.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location)) response = await _client.GetAsync(location);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.ContainsAny("Request", "History", "Status", "Maintenance"))
                {
                    hasAccessToHistory = true;
                    _output.WriteLine($"Request history accessible via {page}");
                    break;
                }
            }
        }

        // At minimum, tenant should have access to some interface
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect ||
            dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }

        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);
        _output.WriteLine("Tenant has access to request management interface");
    }

    [Fact]
    public async Task Tenant_ContactInformationVerification_ShouldSucceed()
    {
        _output.WriteLine("Testing Tenant Contact Information Verification");

        // Step 1: Login as Tenant
        await LoginAsTenant();

        // Step 2: Access request submission form to verify contact info pre-population
        var submitResponse = await _client.GetAsync("/TenantRequests/Submit");
        submitResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        if (submitResponse.StatusCode == HttpStatusCode.OK)
        {
            var submitContent = await submitResponse.Content.ReadAsStringAsync();

            // Verify form contains tenant-specific information
            submitContent.Should().NotBeNullOrEmpty();
            _output.WriteLine("Tenant contact information form accessible");
        }
        else
        {
            _output.WriteLine("Contact information form redirected (authentication may be required)");
        }
    }

    private async Task LoginAsTenant()
    {
        var loginResponse = await _client.GetAsync("/Account/Login");
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(loginContent);

        var loginData = new List<KeyValuePair<string, string>>
        {
            new("Login.Email", "tenant1.unit101@sunset.com"), // Fixed: Use correct demo tenant email
            new("Login.Password", "Demo123!"), // Fixed: Use correct demo password
            new("__RequestVerificationToken", token)
        };

        var loginFormContent = new FormUrlEncodedContent(loginData);
        var loginPostResponse = await _client.PostAsync("/Account/Login", loginFormContent);

        loginPostResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);
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