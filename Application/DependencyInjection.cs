using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalRepairs.Application.Common.Behaviors;
using RentalRepairs.Application.Common.Configuration;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Services;
using RentalRepairs.Application.Services.Notifications;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MediatR - CQRS implementation
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // MediatR Pipeline Behaviors (order matters: Exception → Validation → Performance)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        // Application Configuration
        services.Configure<WorkerServiceSettings>(
            configuration.GetSection("WorkerService"));
        // ✅ Removed SpecializationSettings - logic moved to SpecializationDeterminationService (Domain)

        // Application Orchestration Services
        services.AddScoped<IWorkerService, WorkerService>();

        // Notification Services - Specialized (SRP compliant)
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<ITenantNotificationService, TenantNotificationService>();
        services.AddScoped<ISuperintendentNotificationService, SuperintendentNotificationService>();
        services.AddScoped<IWorkerNotificationService, WorkerNotificationService>();

        // Legacy Notification Service - Facade for backward compatibility
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotifyPartiesService, NotifyPartiesService>();

        // FluentValidation - Input validation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}