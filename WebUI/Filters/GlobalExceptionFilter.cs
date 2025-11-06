using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RentalRepairs.WebUI.Filters;

/// <summary>
/// Global exception filter for Razor Pages.
/// Catches unhandled exceptions, logs them with context, and provides user-friendly error handling.
/// Implements IPageFilter for Razor Pages-specific exception handling.
/// </summary>
public class GlobalExceptionFilter : IPageFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionFilter(
        ILogger<GlobalExceptionFilter> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Called after a handler method has been selected but before model binding occurs.
    /// No action needed for exception handling.
    /// </summary>
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
        // No action needed before handler selection
    }

    /// <summary>
    /// Called before the handler method is invoked.
    /// No action needed for exception handling.
    /// </summary>
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        // No action needed before handler execution
    }

    /// <summary>
    /// Called after the handler method has been executed.
    /// This is where we catch and handle any unhandled exceptions.
    /// </summary>
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
        if (context.Exception != null)
        {
            // Log the exception with rich contextual information
            _logger.LogError(context.Exception,
                "Unhandled exception in page {PageName}. " +
                "User: {UserId}, IP: {IpAddress}, Method: {HttpMethod}, Path: {Path}, Query: {QueryString}",
                context.ActionDescriptor.DisplayName,
                context.HttpContext.User.Identity?.Name ?? "Anonymous",
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                context.HttpContext.Request.QueryString);

            // Mark exception as handled to prevent ASP.NET Core default error handling
            context.ExceptionHandled = true;

            // Environment-specific behavior
            if (_environment.IsDevelopment())
            {
                // In development, let the developer exception page handle it for full stack traces
                // This allows developers to see detailed error information
                context.ExceptionHandled = false;

                _logger.LogWarning(
                    "Exception in development mode - allowing developer exception page to display details");
            }
            else
            {
                // In production, redirect to user-friendly error page
                // Hide sensitive implementation details from users
                context.Result = new RedirectToPageResult("/Error",
                    new
                    {
                        message =
                            "An unexpected error occurred. Please try again or contact support if the problem persists."
                    });

                _logger.LogInformation(
                    "Exception handled - user redirected to error page (Production mode)");
            }
        }
    }
}