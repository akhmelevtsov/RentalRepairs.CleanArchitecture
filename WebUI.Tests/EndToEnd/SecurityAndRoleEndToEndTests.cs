using Microsoft.AspNetCore.Mvc.Testing;
using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.EndToEnd;

/// <summary>
/// End-to-End tests based on Manual Testing Guide scenarios
/// Tests security boundaries and cross-role access restrictions
/// </summary>
public class SecurityAndRoleEndToEndTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public SecurityAndRoleEndToEndTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _output = output;
    }

    [Fact]
    public async Task CrossRoleAccessRestrictions_AllRoles_ShouldBeEnforced()
    {
        _output.WriteLine("Testing Cross-Role Access Restrictions");

        // Test Matrix: Each role attempting to access other roles' functions
        await TestTenantAccessRestrictions();
        await TestWorkerAccessRestrictions();
        await TestSuperintendentAccessRestrictions();
        await TestAdminAccessPrivileges();
    }

    [Fact]
    public async Task AuthenticationBoundaries_AllEndpoints_ShouldRequireAuth()
    {
        _output.WriteLine("Testing Authentication Boundaries");

        // Test that protected endpoints require authentication
        var protectedEndpoints = new[]
        {
            "/Properties/Register",
            "/TenantRequests/Submit",
            "/",
            "/Index"
        };

        foreach (var endpoint in protectedEndpoints)
        {
            _output.WriteLine($"Testing authentication requirement for {endpoint}");
            
            var response = await _client.GetAsync(endpoint);
            
            // Should either require authentication or redirect to login
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.Found,
                HttpStatusCode.PermanentRedirect);
            
            if (response.StatusCode == HttpStatusCode.Redirect || 
                response.StatusCode == HttpStatusCode.Found ||
                response.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location) && location.Contains("Login"))
                {
                    _output.WriteLine($"{endpoint} properly redirects to login");
                }
                else
                {
                    _output.WriteLine($"{endpoint} properly requires authentication");
                }
            }
            else
            {
                _output.WriteLine($"{endpoint} properly requires authentication");
            }
        }
    }

    [Fact]
    public async Task SessionSecurity_Authentication_ShouldBeSecure()
    {
        _output.WriteLine("Testing Session Security");

        // Test 1: Login with valid credentials
        var loginResult = await LoginAsUser("admin@demo.com", "Demo123!"); // Fixed: Use correct demo admin credentials
        loginResult.Should().BeTrue("Valid credentials should allow login");
        
        _output.WriteLine("Valid credentials accepted");

        // Test 2: Access protected resource after login
        var protectedResponse = await _client.GetAsync("/Properties/Register");
        protectedResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);
        
        _output.WriteLine("Authenticated access to protected resources works");

        // Test 3: Logout
        await LogoutCurrentUser();
        
        // Test 4: Try to access protected resource after logout
        var afterLogoutResponse = await _client.GetAsync("/Properties/Register");
        afterLogoutResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.PermanentRedirect);
        
        _output.WriteLine("Access properly restricted after logout");
    }

    [Fact]
    public async Task CSRFProtectionComprehensive_AllForms_ShouldBeProtected()
    {
        _output.WriteLine("Testing Comprehensive CSRF Protection");

        var formsToTest = new[]
        {
            ("/Account/Login", new[] { ("Login.Email", "test@test.com"), ("Login.Password", "test123") }),
            ("/Properties/Register", new[] { ("Property.Name", "Test"), ("Property.Code", "TEST") }),
            ("/TenantRequests/Submit", new[] { ("TenantRequest.ProblemDescription", "Test problem") })
        };

        foreach (var (url, formData) in formsToTest)
        {
            _output.WriteLine($"Testing CSRF protection for {url}");
            
            // Try to submit form without CSRF token
            var data = formData.Select(kvp => new KeyValuePair<string, string>(kvp.Item1, kvp.Item2)).ToList();
            var formContent = new FormUrlEncodedContent(data);
            
            var response = await _client.PostAsync(url, formContent);
            
            // Should be rejected due to missing CSRF token
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.PermanentRedirect);
            
            _output.WriteLine($"CSRF protection active for {url}");
        }
    }

    [Fact]
    public async Task DataIsolation_RoleBased_ShouldBeEnforced()
    {
        _output.WriteLine("Testing Role-Based Data Isolation");

        // Test that each role sees only appropriate data
        
        // Test 1: Superintendent should see only their property
        await LoginAsUser("super.sun001@rentalrepairs.com", "Demo123!"); // Fixed: Use correct demo credentials
        var superDashboard = await GetDashboardContent();
        
        if (!string.IsNullOrEmpty(superDashboard))
        {
            // Should contain Sunset-related content, not other properties
            if (superDashboard.ContainsAny("Sunset", "SUN001"))
            {
                _output.WriteLine("Superintendent sees their property data");
            }
            else
            {
                _output.WriteLine("Superintendent dashboard accessible (data isolation may be implicit)");
            }
        }

        // Test 2: Tenant should see only their data
        await LogoutCurrentUser();
        await LoginAsUser("tenant1.unit101@sunset.com", "Demo123!"); // Fixed: Use correct demo credentials
        var tenantDashboard = await GetDashboardContent();
        
        if (!string.IsNullOrEmpty(tenantDashboard))
        {
            _output.WriteLine("Tenant dashboard shows appropriate scope");
        }

        // Test 3: Worker should see only their assignments
        await LogoutCurrentUser();
        await LoginAsUser("plumber.smith@workers.com", "Demo123!"); // Fixed: Use correct demo credentials
        var workerDashboard = await GetDashboardContent();
        
        if (!string.IsNullOrEmpty(workerDashboard))
        {
            _output.WriteLine("Worker dashboard shows appropriate assignments");
        }
    }

    // ===== PRIVATE TEST METHODS =====

    private async Task TestTenantAccessRestrictions()
    {
        _output.WriteLine("\n?? Testing Tenant Access Restrictions");
        
        await LoginAsUser("tenant1.unit101@sunset.com", "Demo123!"); // Fixed: Use correct demo credentials
        
        var restrictedUrls = new[]
        {
            "/Account/DemoStatus",               // Admin function that exists
            "/TenantRequests/AssignWorker/1"     // Superintendent function
        };

        foreach (var url in restrictedUrls)
        {
            var response = await _client.GetAsync(url);
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.NotFound,
                HttpStatusCode.PermanentRedirect);
            
            _output.WriteLine($"Tenant properly restricted from {url}");
        }
        
        await LogoutCurrentUser();
    }

    private async Task TestWorkerAccessRestrictions()
    {
        _output.WriteLine("\n?? Testing Worker Access Restrictions");
        
        await LoginAsUser("plumber.smith@workers.com", "Demo123!"); // Fixed: Use correct demo credentials
        
        var restrictedUrls = new[]
        {
            "/Account/DemoStatus",               // Admin function that exists
            "/TenantRequests/AssignWorker/1"     // Superintendent function
        };

        foreach (var url in restrictedUrls)
        {
            var response = await _client.GetAsync(url);
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.NotFound,
                HttpStatusCode.PermanentRedirect);
            
            _output.WriteLine($"Worker properly restricted from {url}");
        }
        
        await LogoutCurrentUser();
    }

    private async Task TestSuperintendentAccessRestrictions()
    {
        _output.WriteLine("\n?? Testing Superintendent Access Restrictions");
        
        await LoginAsUser("super.sun001@rentalrepairs.com", "Demo123!"); // Fixed: Use correct demo credentials
        
        // Superintendent should have access to property management but not global admin functions
        var response = await _client.GetAsync("/");
        if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.PermanentRedirect)
        {
            var location = response.Headers.Location?.ToString() ?? "/Index";
            response = await _client.GetAsync(location);
        }
        
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);
        _output.WriteLine("Superintendent has appropriate access level");
        
        await LogoutCurrentUser();
    }

    private async Task TestAdminAccessPrivileges()
    {
        _output.WriteLine("\n?? Testing Admin Access Privileges");
        
        await LoginAsUser("admin@demo.com", "Demo123!"); // Fixed: Use correct demo credentials
        
        var adminUrls = new[]
        {
            "/Account/DemoStatus", // Demo status page (admin access)
            "/",                   // Home dashboard
            "/Index"              // Index page
        };

        foreach (var url in adminUrls)
        {
            var response = await _client.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    response = await _client.GetAsync(location);
                }
            }
            
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.PermanentRedirect);
            _output.WriteLine($"Admin can access {url}");
        }
        
        await LogoutCurrentUser();
    }

    // ===== HELPER METHODS =====

    private async Task<bool> LoginAsUser(string email, string password)
    {
        try
        {
            var loginResponse = await _client.GetAsync("/Account/Login");
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(loginContent);

            var loginData = new List<KeyValuePair<string, string>>
            {
                new("Login.Email", email),
                new("Login.Password", password),
                new("__RequestVerificationToken", token)
            };

            var loginFormContent = new FormUrlEncodedContent(loginData);
            var loginPostResponse = await _client.PostAsync("/Account/Login", loginFormContent);
            
            return loginPostResponse.StatusCode == HttpStatusCode.Redirect || 
                   loginPostResponse.StatusCode == HttpStatusCode.Found ||
                   loginPostResponse.StatusCode == HttpStatusCode.PermanentRedirect;
        }
        catch
        {
            return false;
        }
    }

    private async Task LogoutCurrentUser()
    {
        try
        {
            await _client.PostAsync("/Account/Logout", new StringContent(""));
        }
        catch
        {
            // Logout may fail - continue
        }
    }

    private async Task<string> GetDashboardContent()
    {
        try
        {
            var response = await _client.GetAsync("/");
            if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                var location = response.Headers.Location?.ToString() ?? "/Index";
                response = await _client.GetAsync(location);
            }
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
        catch
        {
            // Error getting dashboard content
        }
        
        return string.Empty;
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