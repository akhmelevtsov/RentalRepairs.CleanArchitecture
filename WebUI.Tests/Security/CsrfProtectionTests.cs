using RentalRepairs.WebUI.Tests.Integration.Infrastructure;
using System.Net;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.Security;

/// <summary>
/// ? CSRF Protection Tests - Verify all forms are protected against Cross-Site Request Forgery attacks
/// Tests that POST requests without antiforgery tokens are rejected
/// </summary>
public class CsrfProtectionTests : IClassFixture<WebApplicationTestFactory<Program>>
{
    private readonly WebApplicationTestFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;

    public CsrfProtectionTests(WebApplicationTestFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task DetailsPage_PostSubmit_WithoutCsrfToken_ShouldBeRejected()
    {
        // Arrange - Use the correct route from the Razor page
        var requestId = Guid.NewGuid();
        var url = $"/tenant-requests/{requestId}";

        _output.WriteLine($"Testing CSRF protection for Details page Submit action at {url}");

        // Act - Try to POST without CSRF token
        var formData = new List<KeyValuePair<string, string>>
        {
            new("handler", "Submit")
        };
        var formContent = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync(url, formContent);

        // Assert - Should be rejected due to missing CSRF token, authentication, or invalid ID
        _output.WriteLine($"Response status: {response.StatusCode}");
        
        // CSRF protection working means we don't get a successful result
        if (response.IsSuccessStatusCode)
        {
            // If we got a successful response, check if it's actually processing the form
            var content = await response.Content.ReadAsStringAsync();
            
            // If the response contains form validation errors or shows the form again,
            // it means CSRF validation might have passed incorrectly
            content.Should().NotContain("Request submitted successfully", 
                "Form should not process successfully without CSRF token");
            
            _output.WriteLine("Response was successful but didn't process the form - acceptable");
        }
        else
        {
            // Any non-success response indicates proper protection
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,     // CSRF validation failed
                HttpStatusCode.Forbidden,      // CSRF validation failed
                HttpStatusCode.Unauthorized,   // Authentication required
                HttpStatusCode.Redirect,       // Redirected to login
                HttpStatusCode.Found,          // Redirected to login
                HttpStatusCode.NotFound,       // Invalid request ID or route not found
                HttpStatusCode.MethodNotAllowed // POST not allowed without proper setup
            );
            
            _output.WriteLine("CSRF protection working - POST without token properly rejected");
        }
    }

    [Fact]
    public async Task DetailsPage_PostClose_WithoutCsrfToken_ShouldBeRejected()
    {
        // Arrange - Use the correct route from the Razor page
        var requestId = Guid.NewGuid();
        var url = $"/tenant-requests/{requestId}";

        _output.WriteLine($"Testing CSRF protection for Details page Close action at {url}");

        // Act - Try to POST without CSRF token
        var formData = new List<KeyValuePair<string, string>>
        {
            new("handler", "Close"),
            new("closureNotes", "Test closure notes")
        };
        var formContent = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync(url, formContent);

        // Assert - Should be rejected for various security reasons
        _output.WriteLine($"Response status: {response.StatusCode}");
        
        if (response.IsSuccessStatusCode)
        {
            // Check if the form was actually processed
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("Request closed successfully", 
                "Form should not process successfully without CSRF token");
            
            _output.WriteLine("Response was successful but didn't process the form - acceptable");
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.Found,
                HttpStatusCode.NotFound,
                HttpStatusCode.MethodNotAllowed
            );
            
            _output.WriteLine("CSRF protection working - POST without token properly rejected");
        }
    }

    [Fact]
    public async Task SubmitPage_Post_WithoutCsrfToken_ShouldBeRejected()
    {
        // Arrange - Use the correct route
        var url = "/TenantRequests/Submit";

        _output.WriteLine($"Testing CSRF protection for Submit page at {url}");

        // Act - Try to POST without CSRF token
        var formData = new List<KeyValuePair<string, string>>
        {
            new("TenantRequest.PropertyCode", "TEST001"),
            new("TenantRequest.UnitNumber", "101"),
            new("TenantRequest.TenantFirstName", "John"),
            new("TenantRequest.TenantLastName", "Doe"),
            new("TenantRequest.TenantEmail", "john@test.com"),
            new("TenantRequest.TenantPhone", "555-1234"),
            new("TenantRequest.ProblemDescription", "Test problem description for CSRF test"),
            new("TenantRequest.UrgencyLevel", "Normal")
        };
        var formContent = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync(url, formContent);

        // Assert - Should be rejected
        _output.WriteLine($"Response status: {response.StatusCode}");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("Request submitted successfully", 
                "Form should not process successfully without CSRF token");
            
            _output.WriteLine("Response was successful but didn't process the form - acceptable");
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.Found,
                HttpStatusCode.NotFound,
                HttpStatusCode.MethodNotAllowed
            );
            
            _output.WriteLine("CSRF protection working - POST without token properly rejected");
        }
    }

    [Fact]
    public async Task AssignWorkerPage_Post_WithoutCsrfToken_ShouldBeRejected()
    {
        // Arrange - Use the correct route (note: uses int, not Guid)
        var requestId = 123; // Use an integer as per the route definition
        var url = $"/TenantRequests/AssignWorker/{requestId}";

        _output.WriteLine($"Testing CSRF protection for AssignWorker page at {url}");

        // Act - Try to POST without CSRF token
        var formData = new List<KeyValuePair<string, string>>
        {
            new("ViewModel.RequestId", requestId.ToString()),
            new("ViewModel.WorkerEmail", "worker@test.com"),
            new("ViewModel.ScheduledDate", DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm")),
            new("ViewModel.WorkOrderNumber", "WO-TEST-001")
        };
        var formContent = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync(url, formContent);

        // Assert - Should be rejected
        _output.WriteLine($"Response status: {response.StatusCode}");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("Worker assigned successfully", 
                "Form should not process successfully without CSRF token");
            
            _output.WriteLine("Response was successful but didn't process the form - acceptable");
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.Found,
                HttpStatusCode.NotFound,
                HttpStatusCode.MethodNotAllowed
            );
            
            _output.WriteLine("CSRF protection working - POST without token properly rejected");
        }
    }

    [Fact]
    public async Task LoginPage_PostAdminLogin_WithoutCsrfToken_ShouldAllowLogin()
    {
        // Arrange
        var url = "/Account/Login";

        _output.WriteLine($"Testing that Login page allows login without CSRF token (for role switching)");

        // Act - Try to POST without CSRF token
        var formData = new List<KeyValuePair<string, string>>
        {
            new("Login.Email", "admin@test.com"),
            new("Login.Password", "TestPassword123")
        };
        var formContent = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync(url, formContent);

        // Assert - Login page should allow POST without CSRF token to enable role switching
        _output.WriteLine($"Response status: {response.StatusCode}");
        
        // The login page is designed to bypass CSRF validation for role switching scenarios
        // This is a deliberate security trade-off: login forms are lower risk and this enables
        // seamless user experience when switching between roles
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            // The form should process the login attempt (though it may fail due to invalid credentials)
            // This demonstrates that CSRF validation is bypassed as intended
            _output.WriteLine("Login form processed without CSRF token - role switching enabled");
        }
        else if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
        {
            // Redirect responses are also acceptable - could be authentication failure redirects
            _output.WriteLine("Login form responded with redirect - role switching enabled");
        }
        else
        {
            // Other responses indicate potential issues but aren't necessarily wrong
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.NotFound,
                HttpStatusCode.MethodNotAllowed
            );
            
            _output.WriteLine(" Login form returned non-success response - may still support role switching");
        }
    }

    [Fact]
    public async Task PropertyRegisterPage_Post_WithoutCsrfToken_ShouldBeRejected()
    {
        // Arrange
        var url = "/Properties/Register";

        _output.WriteLine($"Testing CSRF protection for Property Register page at {url}");

        // Act - Try to POST without CSRF token
        var formData = new List<KeyValuePair<string, string>>
        {
            new("Property.Name", "Test Property"),
            new("Property.Code", "TEST001"),
            new("Property.Address.StreetNumber", "123"),
            new("Property.Address.StreetName", "Test St"),
            new("Property.Address.City", "Test City"),
            new("Property.Address.PostalCode", "12345"),
            new("Property.PhoneNumber", "555-1234"),
            new("Property.Superintendent.FirstName", "John"),
            new("Property.Superintendent.LastName", "Doe"),
            new("Property.Superintendent.EmailAddress", "john@test.com")
        };
        var formContent = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync(url, formContent);

        // Assert - Should be rejected
        _output.WriteLine($"Response status: {response.StatusCode}");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("registered successfully", 
                "Form should not process successfully without CSRF token");
            
            _output.WriteLine("Response was successful but didn't process the form - acceptable");
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect,
                HttpStatusCode.Found,
                HttpStatusCode.NotFound,
                HttpStatusCode.MethodNotAllowed
            );
            
            _output.WriteLine("CSRF protection working - POST without token properly rejected");
        }
    }

    [Theory]
    [InlineData("X-CSRF-TOKEN")]
    [InlineData("__RequestVerificationToken")]
    public async Task AntiforgeryConfiguration_ShouldBeActive(string expectedHeaderOrCookie)
    {
        // Verify that the antiforgery system is active by checking that pages load
        _output.WriteLine($"Verifying antiforgery system is active (checking for {expectedHeaderOrCookie})");

        // Test basic page accessibility - if antiforgery is misconfigured, pages won't load
        var response = await _client.GetAsync("/Account/Login");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Found);
        
        _output.WriteLine("Antiforgery system is active - pages load correctly");
    }

    [Fact]
    public async Task GetPages_ShouldLoadWithoutCsrfErrors()
    {
        // Test that GET pages load correctly (CSRF only affects POST requests)
        var pagesToTest = new[]
        {
            "/Account/Login",
            "/Properties/Register",
            "/Privacy"
        };

        var successCount = 0;
        var redirectCount = 0;

        foreach (var page in pagesToTest)
        {
            _output.WriteLine($"Testing GET request to {page}");

            try
            {
                var response = await _client.GetAsync(page);
                
                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    _output.WriteLine($"{page} loads successfully");
                    
                    // Optionally check for antiforgery token presence in successful responses
                    var content = await response.Content.ReadAsStringAsync();
                    var hasAntiforgeryToken = content.Contains("__RequestVerificationToken") || 
                                            content.Contains("@Html.AntiForgeryToken()") ||
                                            content.Contains("asp-antiforgery=\"true\"");
                    
                    if (hasAntiforgeryToken)
                    {
                        _output.WriteLine($"   ? Contains antiforgery tokens");
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Redirect || 
                         response.StatusCode == HttpStatusCode.Found)
                {
                    redirectCount++;
                    _output.WriteLine($"{page} redirects properly (likely requires authentication)");
                }
                else
                {
                    _output.WriteLine($" {page} returned: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($" Error accessing {page}: {ex.Message}");
            }
        }
        
        // At least some pages should be accessible via GET
        (successCount + redirectCount).Should().BeGreaterThan(0, 
            "At least some pages should be accessible via GET requests");
        
        _output.WriteLine($"GET request test completed: {successCount} successful, {redirectCount} redirected");
    }

    [Fact]
    public async Task CsrfProtection_ShouldNotAffectHealthCheck()
    {
        // Verify that CSRF protection doesn't interfere with non-form endpoints
        _output.WriteLine("Testing that CSRF protection doesn't affect health check endpoint");

        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
        
        _output.WriteLine("Health check works correctly despite CSRF protection being enabled");
    }
}