using RentalRepairs.CompositionRoot;
using RentalRepairs.WebUI.Mappings;
using RentalRepairs.WebUI.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with basic configuration
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/rentalrepairs-.txt", rollingInterval: RollingInterval.Day)
        .Enrich.FromLogContext();
});

// ✅ COMPOSITION ROOT: Clean Architecture compliant service registration
// This replaces direct Infrastructure calls with proper abstraction
builder.Services.AddRazorPagesClient(builder.Configuration, builder.Environment);

// ✅ SHARED: Authorization policies for all client types
builder.Services.AddSharedAuthorization();

// ✅ SHARED: Production services (HTTPS, health checks, etc.)
builder.Services.AddProductionServices(builder.Environment);

// ✅ Configure Mapster mappings properly
ApplicationToViewModelMappingConfig.RegisterMappings();

// Add WebUI-specific services (presentation layer only)
builder.Services.AddScoped<IViewRenderService, ViewRenderService>();

// ✅ Override Infrastructure's CurrentUserService with WebUI's HttpContext-aware version
// This is acceptable as it's presentation-layer specific implementation
builder.Services.AddScoped<RentalRepairs.Application.Common.Interfaces.ICurrentUserService, RentalRepairs.WebUI.Services.CurrentUserService>();

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
    {
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "font-src 'self' https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https:;";
    }
    
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
        await ApplicationCompositionRoot.InitializeApplicationAsync(scope.ServiceProvider);
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Application initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the application");
        
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }