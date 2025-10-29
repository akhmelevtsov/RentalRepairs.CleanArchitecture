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
        
        // ? Register DTO to ReadModel mappings for application services
        RegisterDtoToReadModelMappings();
        
        return services;
    }

    private static void RegisterDomainToApplicationMappings()
    {
        // ? Direct projection mappings - no circular references
        TypeAdapterConfig<Domain.Entities.TenantRequest, Application.ReadModels.TenantRequestListItemReadModel>
            .NewConfig()
            .Map(dest => dest.CreatedDate, src => src.CreatedAt)
            .Map(dest => dest.StatusDisplayName, src => MapStatusToDisplayName(src.Status))
            // ? Use denormalized fields from domain - no navigation properties
            .Map(dest => dest.TenantFullName, src => src.TenantFullName)
            .Map(dest => dest.PropertyName, src => src.PropertyName)
            .Map(dest => dest.SuperintendentFullName, src => src.SuperintendentFullName);

        TypeAdapterConfig<Domain.Entities.Worker, Application.DTOs.WorkerDto>
            .NewConfig()
            .Map(dest => dest.RegistrationDate, src => src.CreatedAt);

        TypeAdapterConfig<Domain.ValueObjects.PersonContactInfo, PersonContactInfoDto>
            .NewConfig()
            .Map(dest => dest.FullName, src => src.GetFullName());

        TypeAdapterConfig<Domain.Entities.Property, Application.DTOs.PropertyDto>
            .NewConfig()
            .Map(dest => dest.CreatedDate, src => src.CreatedAt);

        TypeAdapterConfig<Domain.Entities.Tenant, Application.DTOs.TenantDto>
            .NewConfig()
            .Map(dest => dest.RegistrationDate, src => src.CreatedAt);
    }

    private static void RegisterDtoToReadModelMappings()
    {
        // ? TenantRequestDto to TenantRequestDetailsReadModel mapping for application services
        TypeAdapterConfig<TenantRequestDto, Application.ReadModels.TenantRequestDetailsReadModel>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString()) // Convert Guid to string for display
            .Map(dest => dest.Code, src => src.Code ?? $"REQ-{src.Id}") // Generate code if missing
            .Map(dest => dest.Status, src => ParseStatusFromString(src.Status)) // Parse status string to enum
            .Map(dest => dest.CreatedDate, src => src.CreatedDate)
            .Map(dest => dest.StatusDisplayName, src => src.StatusDisplayName ?? MapStatusDisplayName(src.Status));
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

    private static string MapStatusDisplayName(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "draft" => "Draft",
            "submitted" => "Submitted", 
            "scheduled" => "Scheduled",
            "done" => "Completed",
            "failed" => "Failed",
            "declined" => "Declined",
            "closed" => "Closed",
            _ => status ?? "Unknown"
        };
    }

    private static Domain.Enums.TenantRequestStatus ParseStatusFromString(string status)
    {
        return Enum.TryParse<Domain.Enums.TenantRequestStatus>(status, true, out var result) 
            ? result 
            : Domain.Enums.TenantRequestStatus.Draft;
    }
}