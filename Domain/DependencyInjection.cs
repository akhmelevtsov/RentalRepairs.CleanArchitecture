using Microsoft.Extensions.DependencyInjection;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // ✅ Pure Domain Services (no repository dependencies) - actively used
        services.AddScoped<PropertyPolicyService>();
        services.AddScoped<UserRoleDomainService>();
        services.AddScoped<SpecializationDeterminationService>(); // NEW: Specialization logic

        // ✅ Domain Services with proper abstractions (actively used services)
        services.AddScoped<TenantRequestStatusPolicy>();
        services.AddScoped<TenantRequestUrgencyPolicy>();
        services.AddScoped<RequestAuthorizationPolicy>();
        services.AddScoped<ITenantRequestSubmissionPolicy, TenantRequestSubmissionPolicy>();
        services.AddScoped<UnitSchedulingService>();
        services.AddScoped<AuthorizationDomainService>();

        return services;
    }
}
