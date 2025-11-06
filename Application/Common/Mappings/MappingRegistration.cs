using Mapster;
using Microsoft.Extensions.DependencyInjection;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Common.Mappings;

/// <summary>
/// ? Proper mapping configuration without singleton anti-pattern
/// </summary>
public static class MappingRegistration
{
    /// <summary>
    /// ? Register all mapping configurations at startup - no singletons needed
    /// </summary>
    public static IServiceCollection AddMappingProfiles(this IServiceCollection services)
    {
        // ? Configure Mapster globally at startup - thread safe
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingRegistration).Assembly);

        // ? Register domain to application mappings only (no WebUI references in Application layer)
        RegisterDomainToApplicationMappings();

        return services;
    }

    private static void RegisterDomainToApplicationMappings()
    {
        // ? Direct projection mappings - no circular references
        TypeAdapterConfig<Domain.Entities.Worker, WorkerDto>
            .NewConfig()
            .Map(dest => dest.RegistrationDate, src => src.CreatedAt);

        TypeAdapterConfig<Domain.ValueObjects.PersonContactInfo, PersonContactInfoDto>
            .NewConfig()
            .Map(dest => dest.FullName, src => src.GetFullName());

        TypeAdapterConfig<Domain.Entities.Property, PropertyDto>
            .NewConfig()
            .Map(dest => dest.CreatedDate, src => src.CreatedAt);

        TypeAdapterConfig<Domain.Entities.Tenant, TenantDto>
            .NewConfig()
            .Map(dest => dest.RegistrationDate, src => src.CreatedAt);
    }

    private static string MapStatusToDisplayName(Domain.Enums.TenantRequestStatus status)
    {
        return status switch
        {
            Domain.Enums.TenantRequestStatus.Draft => "Draft",
            Domain.Enums.TenantRequestStatus.Submitted => "Submitted",
            Domain.Enums.TenantRequestStatus.Scheduled => "Scheduled",
            Domain.Enums.TenantRequestStatus.Done => "Completed",
            Domain.Enums.TenantRequestStatus.Failed => "Failed",
            Domain.Enums.TenantRequestStatus.Declined => "Declined",
            Domain.Enums.TenantRequestStatus.Closed => "Closed",
            _ => status.ToString()
        };
    }
}