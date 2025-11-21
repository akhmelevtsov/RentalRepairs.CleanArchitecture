using RentalRepairs.CompositionRoot;
using RentalRepairs.WebUI.Mappings;
using RentalRepairs.WebUI.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with environment-aware logging
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .Enrich.FromLogContext();

    // Detect if running locally (not on Azure App Service)
    var isRunningOnAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));

    if (!isRunningOnAzure)
    {
        // Local development - log to file for easy debugging
        configuration.WriteTo.File("logs/rentalrepairs-.txt", rollingInterval: RollingInterval.Day);
    }
    // Note: On Azure, console logs are automatically captured by Azure App Service diagnostics
});

// Enable Azure App Services logging integration when running on Azure
var websiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
if (!string.IsNullOrEmpty(websiteName))
{
    builder.Logging.AddAzureWebAppDiagnostics();
    Console.WriteLine($"=== AZURE LOGGING ENABLED FOR: {websiteName} ===");
    Console.WriteLine($"=== HOME PATH: {Environment.GetEnvironmentVariable("HOME")} ===");
}
else
{
    Console.WriteLine("=== RUNNING LOCALLY - File logging enabled ===");
}

// ✅ COMPOSITION ROOT: Clean Architecture compliant service registration
// This replaces direct Infrastructure calls with proper abstraction
builder.Services.AddRazorPagesClient(builder.Configuration, builder.Environment);

// ✅ SHARED: Authorization policies for all client types
builder.Services.AddSharedAuthorization();

// ✅ SHARED: Production services (HTTPS, health checks, etc.)
builder.Services.AddProductionServices(builder.Environment);

// ✅ Configure Mapster mappings properly
ApplicationToViewModelMappingConfig.RegisterMappings();

// ✅ Add WebUI-specific services
// Register GlobalExceptionFilter for centralized error handling
builder.Services.AddScoped<RentalRepairs.WebUI.Filters.GlobalExceptionFilter>();

// Configure Razor Pages to use GlobalExceptionFilter
builder.Services.Configure<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions>(options =>
{
    // Filter is registered separately and will be resolved from DI
});

builder.Services.AddMvc(options => { options.Filters.Add<RentalRepairs.WebUI.Filters.GlobalExceptionFilter>(); });

// Override Infrastructure's CurrentUserService with WebUI's HttpContext-aware version
// This is acceptable as it's presentation-layer specific implementation
builder.Services.AddScoped<RentalRepairs.Application.Common.Interfaces.ICurrentUserService, CurrentUserService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Shared/Error");
    app.UseHsts();
}

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    if (!app.Environment.IsDevelopment())
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "font-src 'self' https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https:;";

    await next.Invoke();
});

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map health checks
app.MapHealthChecks("/health");

// Map Razor Pages
app.MapRazorPages();

// ✅ COMPOSITION ROOT: Clean initialization through abstraction
// No direct Infrastructure calls - everything goes through Composition Root
using (var scope = app.Services.CreateScope())
{
    try
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Log environment information for diagnostics
        logger.LogWarning("=== APPLICATION STARTING ===");
        logger.LogWarning("Environment: {Environment}", app.Environment.EnvironmentName);
        logger.LogWarning("Website Name: {WebsiteName}", Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "LOCAL");
        logger.LogWarning("Home Path: {HomePath}", Environment.GetEnvironmentVariable("HOME") ?? "NOT SET");

        await ApplicationCompositionRoot.InitializeApplicationAsync(scope.ServiceProvider);

        logger.LogInformation("Application initialization completed successfully");
        logger.LogWarning("=== APPLICATION READY ===");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the application");

        if (!app.Environment.IsDevelopment()) throw;
    }
}

app.Run();

// Make the implicit Program class public for testing
public partial class Program
{
}