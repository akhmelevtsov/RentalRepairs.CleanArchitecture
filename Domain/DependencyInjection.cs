using Microsoft.Extensions.DependencyInjection;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // ✅ Configuration for domain services (needed by TenantRequestSubmissionPolicy)
        services.AddSingleton<TenantRequestPolicyConfiguration>();

        // ✅ Pure Domain Services (no repository dependencies) - used by application services
        services.AddScoped<TenantRequestPolicyService>();
        services.AddScoped<PropertyPolicyService>();
        services.AddScoped<UserRoleDomainService>(); // ✅ Used by UserRoleService
        services.AddScoped<WorkerAssignmentPolicyService>(); // ✅ NEW: Pure worker assignment policy service
        
        // ✅ Domain Services with proper abstractions (actively used services)
        services.AddScoped<RequestTitleGenerator>();
        services.AddScoped<TenantRequestStatusPolicy>();
        services.AddScoped<TenantRequestUrgencyPolicy>();
        services.AddScoped<RequestAuthorizationPolicy>();
        services.AddScoped<RequestWorkflowManager>();
        services.AddScoped<RequestCategorizationService>();
        services.AddScoped<ITenantRequestSubmissionPolicy, TenantRequestSubmissionPolicy>(); // ✅ Interface with implementation
        services.AddScoped<UnitSchedulingService>(); // ✅ Used by ScheduleServiceWorkCommandHandler
        services.AddScoped<AuthorizationDomainService>();

        return services;
    }
}
