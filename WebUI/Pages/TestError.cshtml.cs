using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RentalRepairs.WebUI.Pages;

/// <summary>
/// Test page for verifying GlobalExceptionFilter functionality.
/// This page deliberately throws an exception to test error handling.
/// DELETE THIS FILE AFTER TESTING.
/// </summary>
public class TestErrorModel : PageModel
{
    private readonly ILogger<TestErrorModel> _logger;

    public TestErrorModel(ILogger<TestErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        _logger.LogInformation("TestError page accessed - about to throw test exception");

        // Deliberately throw an exception to test GlobalExceptionFilter
        throw new InvalidOperationException(
            "TEST EXCEPTION: This is a deliberate test of GlobalExceptionFilter. " +
            "If you see this in logs with context information and are redirected to /Error page, " +
            "the filter is working correctly.");
    }
}