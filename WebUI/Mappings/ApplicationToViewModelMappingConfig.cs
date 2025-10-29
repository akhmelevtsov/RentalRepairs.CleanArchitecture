using Mapster;
using RentalRepairs.Application.DTOs;
using RentalRepairs.WebUI.Models;

namespace RentalRepairs.WebUI.Mappings;

/// <summary>
/// Mapster configuration for mapping between Application DTOs and WebUI ViewModels
/// </summary>
public static class ApplicationToViewModelMappingConfig
{
    public static void RegisterMappings()
    {
        // Property mappings
        TypeAdapterConfig<PropertyDto, PropertyDetailsViewModel>
            .NewConfig()
            .Map(dest => dest.FullAddress, src => src.Address.FullAddress)
            .Map(dest => dest.SuperintendentName, src => src.Superintendent.FullName);

        // Consolidated PropertyDto -> PropertySummaryViewModel mapping (removed duplicate)
        TypeAdapterConfig<PropertyDto, PropertySummaryViewModel>
            .NewConfig()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Code, src => src.Code)
            .Map(dest => dest.City, src => src.Address.City)
            .Map(dest => dest.State, src => src.Address.PostalCode) // Assuming PostalCode is used as State
            .Map(dest => dest.SuperintendentName, src => src.Superintendent.FullName);

        TypeAdapterConfig<RegisterPropertyViewModel, PropertyDto>
            .NewConfig();

        // Tenant Request mappings - Complete with all necessary field mappings
        TypeAdapterConfig<TenantRequestDto, TenantRequestSummaryViewModel>
            .NewConfig()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.ProblemDescription, src => src.Description)
            .Map(dest => dest.TenantName, src => src.TenantFullName)
            .Map(dest => dest.UnitNumber, src => src.TenantUnit) // Fix: Unit number mapping
            .Map(dest => dest.PropertyName, src => src.PropertyName)
            .Map(dest => dest.PropertyCode, src => src.PropertyCode)
            .Map(dest => dest.TenantEmail, src => src.TenantEmail)
            .Map(dest => dest.UrgencyLevel, src => src.UrgencyLevel)
            .Map(dest => dest.IsEmergency, src => src.IsEmergency)
            .Map(dest => dest.SubmittedDate, src => src.SubmittedDate ?? src.CreatedDate)
            .Map(dest => dest.CreatedDate, src => src.CreatedDate)
            .Map(dest => dest.ScheduledDate, src => src.ScheduledDate)
            .Map(dest => dest.CompletedDate, src => src.CompletedDate)
            .Map(dest => dest.AssignedWorkerEmail, src => src.AssignedWorkerEmail)
            .Map(dest => dest.WorkOrderNumber, src => src.WorkOrderNumber);

        TypeAdapterConfig<TenantRequestDto, TenantRequestDetailsViewModel>
            .NewConfig()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.ProblemDescription, src => src.Description)
            .Map(dest => dest.TenantName, src => src.TenantFullName)
            .Map(dest => dest.UnitNumber, src => src.TenantUnit) // Fix: Unit number mapping
            .Map(dest => dest.PropertyName, src => src.PropertyName)
            .Map(dest => dest.PropertyCode, src => src.PropertyCode)
            .Map(dest => dest.TenantEmail, src => src.TenantEmail)
            .Map(dest => dest.IsEmergency, src => src.IsEmergency)
            .Map(dest => dest.SubmittedDate, src => src.SubmittedDate ?? src.CreatedDate)
            .Map(dest => dest.ScheduledDate, src => src.ScheduledDate)
            .Map(dest => dest.CompletedDate, src => src.CompletedDate)
            .Map(dest => dest.WorkerEmail, src => src.AssignedWorkerEmail)
            .Map(dest => dest.CompletionNotes, src => src.CompletionNotes);

        TypeAdapterConfig<SubmitTenantRequestViewModel, TenantRequestDto>
            .NewConfig();
    }
}