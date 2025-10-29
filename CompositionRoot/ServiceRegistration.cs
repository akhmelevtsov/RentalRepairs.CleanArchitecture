using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RentalRepairs.Application;
using RentalRepairs.Infrastructure;
using RentalRepairs.Domain;

namespace RentalRepairs.CompositionRoot;

/// <summary>
/// Clean Architecture Composition Root
/// Provides clean service registration methods for different client types
/// while maintaining proper architectural boundaries
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Register services for Razor Pages client application
    /// </summary>
    public static IServiceCollection AddRazorPagesClient(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Register Domain Services first (required by Application layer)
        services.AddDomainServices(); // ? FIXED: Use the correct extension method name

        // Core Application Services
        services.AddApplicationServices(); // ? FIXED: Use the correct extension method name

        // Infrastructure Services (through clean abstraction)
        services.AddInfrastructure(configuration, environment);

        // Essential ASP.NET Core services
        services.AddHttpContextAccessor(); // Required for CurrentUserService

        // Razor Pages specific services
        services.AddRazorPages(options =>
        {
            // Configure authorization policies for pages
            options.Conventions.AuthorizeFolder("/");
            options.Conventions.AllowAnonymousToPage("/Account/Login");
            options.Conventions.AllowAnonymousToPage("/Account/Register");
            options.Conventions.AllowAnonymousToPage("/Shared/Error");
        });

        // Authentication for Razor Pages
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = environment.IsDevelopment() 
                    ? CookieSecurePolicy.SameAsRequest 
                    : CookieSecurePolicy.Always;
            });

        return services;
    }

    /// <summary>
    /// Register shared authorization policies used across all client types
    /// </summary>
    public static IServiceCollection AddSharedAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Role-based policies
            options.AddPolicy("RequireSystemAdmin", policy =>
                policy.RequireRole("SystemAdministrator"));
            
            options.AddPolicy("RequirePropertySuperintendent", policy =>
                policy.RequireRole("PropertySuperintendent", "SystemAdministrator"));
            
            options.AddPolicy("RequireMaintenanceWorker", policy =>
                policy.RequireRole("MaintenanceWorker", "PropertySuperintendent", "SystemAdministrator"));
            
            options.AddPolicy("RequireTenant", policy =>
                policy.RequireRole("Tenant", "PropertySuperintendent", "SystemAdministrator"));

            // Claim-based policies
            options.AddPolicy("RequirePropertyAccess", policy =>
                policy.RequireClaim("PropertyCode"));
            
            options.AddPolicy("RequireWorkerAssignment", policy =>
                policy.RequireClaim("WorkerSpecialization"));
        });

        return services;
    }

    /// <summary>
    /// Register production-ready services (HTTPS, compression, health checks, etc.)
    /// </summary>
    public static IServiceCollection AddProductionServices(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        // Health Checks
        services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application");

        return services;
    }
}