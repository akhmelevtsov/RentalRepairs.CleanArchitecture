using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RentalRepairs.Application.Common.Behaviors;
using RentalRepairs.Application.Services;
using RentalRepairs.Application.Interfaces;

namespace RentalRepairs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR Registration
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Behavior Registration
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        // ? FIXED: Application Orchestration Services (proper DDD architecture)
        services.AddScoped<ITenantRequestService, TenantRequestService>(); // Uses pure domain services
        services.AddScoped<IWorkerService, WorkerService>();
        //services.AddScoped<IWorkerAssignmentOrchestrationService, WorkerAssignmentOrchestrationService>(); // ? NEW: Worker assignment orchestration
        
        // ? Application Services (data transformation and coordination)
        services.AddScoped<UserRoleService>(); // ? Added role management service
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotifyPartiesService, NotifyPartiesService>();
        
        // ? REMOVED: Services that don't exist or are incorrectly referenced
        // services.AddScoped<ITenantRequestStatusService, StatusManagement.TenantRequestStatusService>();
        // services.AddScoped<ITenantRequestAuthorizationService, Authorization.TenantRequestAuthorizationService>();

        // Validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}