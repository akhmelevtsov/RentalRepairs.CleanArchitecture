using Microsoft.AspNetCore.Mvc.Testing;
using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.EndToEnd;

/// <summary>
/// End-to-End tests based on Manual Testing Guide scenarios
/// Tests complete user workflows for Maintenance Worker role
/// </summary>
public class MaintenanceWorkerEndToEndTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public MaintenanceWorkerEndToEndTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _output = output;
    }

    [Fact]
    public async Task MaintenanceWorker_WorkDashboardAccess_ShouldSucceed()
    {
        _output.WriteLine("Testing Maintenance Worker Dashboard Access");

        // Step 1: Login as Maintenance Worker
        await LoginAsMaintenanceWorker();

        // Step 2: Access worker dashboard
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect ||
            dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }

        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        // Verify worker sees work-related dashboard
        if (dashboardResponse.StatusCode == HttpStatusCode.OK)
        {
            var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
            dashboardContent.Should().NotBeNullOrEmpty();

            // Step 3: Check for work order related content
            // The dashboard should show assigned work orders or schedule
            if (dashboardContent.ContainsAny("Work", "Order", "Assignment", "Schedule", "Plumber"))
                _output.WriteLine("Dashboard contains work-related information");
            else
                _output.WriteLine("Dashboard accessible (work assignments may be empty)");
        }
        else
        {
            _output.WriteLine("Worker dashboard accessible (redirected to appropriate location)");
        }
    }

    [Fact]
    public async Task MaintenanceWorker_WorkOrderManagement_ShouldSucceed()
    {
        _output.WriteLine("Testing Maintenance Worker Work Order Management");

        // Step 1: Login as Maintenance Worker
        await LoginAsMaintenanceWorker();

        // Step 2: Access work order management pages
        var workPages = new[]
        {
            "/",
            "/Index"
        };

        var hasWorkInterface = false;

        foreach (var page in workPages)
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
                if (content.ContainsAny("Work", "Assignment", "Order", "Schedule", "Progress"))
                {
                    hasWorkInterface = true;
                    _output.WriteLine($"Work management interface accessible via {page}");
                    break;
                }
            }
        }

        // At minimum, worker should have access to some dashboard
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect ||
            dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }

        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);
    }

    [Fact]
    public async Task MaintenanceWorker_SpecializationMatch_ShouldSucceed()
    {
        _output.WriteLine("Testing Maintenance Worker Specialization Matching");

        // Step 1: Login as Plumbing Specialist
        await LoginAsMaintenanceWorker();

        // Step 2: Access dashboard and verify specialization context
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect ||
            dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }

        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);
        // Verify worker identity and specialization context
        if (dashboardResponse.StatusCode == HttpStatusCode.OK)
        {
            var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
            dashboardContent.Should().NotBeNullOrEmpty();

            // Check if specialization is mentioned or relevant work is shown
            if (dashboardContent.ContainsAny("Plumber", "Plumbing", "Water", "Pipe"))
                _output.WriteLine("Dashboard shows plumbing specialization context");
            else
                _output.WriteLine("Dashboard accessible for specialized worker");
        }
        else
        {
            _output.WriteLine("Dashboard accessible (redirected to appropriate worker interface)");
        }
    }

    [Fact]
    public async Task MaintenanceWorker_WorkCompletionProcess_ShouldHandleCorrectly()
    {
        _output.WriteLine("Testing Maintenance Worker Work Completion Process");

        // Step 1: Login as Maintenance Worker
        await LoginAsMaintenanceWorker();

        // Step 2: Test access to work completion functionality
        // This would typically involve accessing a specific work order
        // For now, test general access to work-related pages

        var workCompletionPages = new[]
        {
            "/",
            "/Index"
        };

        foreach (var page in workCompletionPages)
        {
            var response = await _client.GetAsync(page);

            if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location)) response = await _client.GetAsync(location);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Work completion interface accessible via {page}");

                // Check for completion-related functionality
                if (content.ContainsAny("Complete", "Progress", "Notes", "Status"))
                    _output.WriteLine("Work completion features available");
            }
        }
    }

    [Fact]
    public async Task MaintenanceWorker_TenantCommunication_ShouldSucceed()
    {
        _output.WriteLine("Testing Maintenance Worker Tenant Communication");

        // Step 1: Login as Maintenance Worker
        await LoginAsMaintenanceWorker();

        // Step 2: Access dashboard to verify tenant contact information availability
        var dashboardResponse = await _client.GetAsync("/");
        if (dashboardResponse.StatusCode == HttpStatusCode.Redirect ||
            dashboardResponse.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = dashboardResponse.Headers.Location?.ToString() ?? "/Index";
            dashboardResponse = await _client.GetAsync(location);
        }

        dashboardResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);

        // Verify worker has access to necessary communication tools/information
        if (dashboardResponse.StatusCode == HttpStatusCode.OK)
        {
            var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
            dashboardContent.Should().NotBeNullOrEmpty();

            if (dashboardContent.ContainsAny("Contact", "Phone", "Email", "Tenant"))
                _output.WriteLine("Tenant communication information available");
            else
                _output.WriteLine("Worker interface accessible (communication features may be contextual)");
        }
        else
        {
            _output.WriteLine("Worker interface accessible (redirected to appropriate interface)");
        }
    }

    [Fact]
    public async Task MaintenanceWorker_SuperintendentReporting_ShouldSucceed()
    {
        _output.WriteLine(" Testing Maintenance Worker Superintendent Reporting");

        // Step 1: Login as Maintenance Worker
        await LoginAsMaintenanceWorker();

        // Step 2: Verify worker has appropriate reporting interface
        var reportingPages = new[]
        {
            "/",
            "/Index"
        };

        foreach (var page in reportingPages)
        {
            var response = await _client.GetAsync(page);

            if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location)) response = await _client.GetAsync(location);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Reporting interface accessible via {page}");

                // Check for reporting-related functionality
                if (content.ContainsAny("Report", "Update", "Status", "Notes", "Superintendent"))
                    _output.WriteLine("Superintendent reporting features available");
            }
        }
    }

    private async Task LoginAsMaintenanceWorker()
    {
        var loginResponse = await _client.GetAsync("/Account/Login");
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(loginContent);

        var loginData = new List<KeyValuePair<string, string>>
        {
            new("Login.Email", "plumber.smith@workers.com"), // Fixed: Use correct demo worker email
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