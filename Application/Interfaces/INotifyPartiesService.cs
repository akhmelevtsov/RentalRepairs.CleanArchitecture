using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Application.Interfaces;

/// <summary>
/// Service interface for notifying various parties about events in the rental repairs system
/// </summary>
public interface INotifyPartiesService
{
    // Tenant Request Event Methods
    Task NotifyTenantOfRequestCreationAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifyTenantOfRequestSubmissionAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifyTenantOfRequestScheduledAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifyTenantOfScheduledWorkAsync(TenantRequest tenantRequest, ServiceWorkScheduleInfo scheduleInfo, CancellationToken cancellationToken = default);
    Task NotifyTenantOfRequestCompletedAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifyTenantOfWorkCompletionAsync(TenantRequest tenantRequest, bool successful, string? notes, CancellationToken cancellationToken = default);
    Task NotifyTenantOfRequestClosureAsync(TenantRequest tenantRequest, string? closureNotes, CancellationToken cancellationToken = default);
    Task NotifyTenantOfRequestDeclinationAsync(TenantRequest tenantRequest, string reason, CancellationToken cancellationToken = default);

    Task NotifySuperintendentOfNewRequestAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfPendingRequestAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfUrgentRequestAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfRequestScheduledAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfScheduledWorkAsync(TenantRequest tenantRequest, ServiceWorkScheduleInfo scheduleInfo, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfRequestCompletedAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfWorkCompletionAsync(TenantRequest tenantRequest, bool successful, string? notes, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfWorkFailureAsync(TenantRequest tenantRequest, string? notes, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfRequestClosureAsync(TenantRequest tenantRequest, string? closureNotes, CancellationToken cancellationToken = default);

    Task NotifyWorkerOfWorkAssignmentAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);
    Task NotifyWorkerOfWorkAssignmentAsync(TenantRequest tenantRequest, ServiceWorkScheduleInfo scheduleInfo, CancellationToken cancellationToken = default);
    Task NotifyWorkerOfWorkCompletionAsync(TenantRequest tenantRequest, CancellationToken cancellationToken = default);

    // Worker Event Methods
    Task NotifyWorkerOfRegistrationAsync(Worker worker, CancellationToken cancellationToken = default);
    Task NotifyWorkerOfSpecializationChangeAsync(Worker worker, string? oldSpecialization, string newSpecialization, CancellationToken cancellationToken = default);
    Task NotifyWorkerOfStatusChangeAsync(Worker worker, bool isActive, CancellationToken cancellationToken = default);
    Task NotifyWorkerOfDeactivationAsync(Worker worker, string reason, CancellationToken cancellationToken = default);
    Task UpdateWorkerScheduleAsync(Worker worker, WorkAssignment assignment, CancellationToken cancellationToken = default);
    Task UpdateWorkerAvailabilityAsync(Worker worker, WorkAssignment assignment, CancellationToken cancellationToken = default);
    Task RecordWorkerPerformanceAsync(Worker worker, WorkAssignment assignment, bool successful, CancellationToken cancellationToken = default);
    Task HandleWorkerDeactivationReassignmentsAsync(Worker worker, CancellationToken cancellationToken = default);

    // Property Event Methods
    Task NotifySuperintendentOfPropertyRegistrationAsync(Property property, CancellationToken cancellationToken = default);
    Task NotifyTenantsOfSuperintendentChangeAsync(Property property, PersonContactInfo oldSuperintendent, PersonContactInfo newSuperintendent, CancellationToken cancellationToken = default);
    Task NotifyNewSuperintendentOfAssignmentAsync(Property property, PersonContactInfo newSuperintendent, CancellationToken cancellationToken = default);
    Task ArchiveOldSuperintendentAccessAsync(Property property, PersonContactInfo oldSuperintendent, CancellationToken cancellationToken = default);
    Task TransferSuperintendentResponsibilitiesAsync(Property property, PersonContactInfo oldSuperintendent, PersonContactInfo newSuperintendent, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfUnitAddedAsync(Property property, string unitNumber, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfUnitRemovedAsync(Property property, string unitNumber, CancellationToken cancellationToken = default);
    Task InitializePropertyResourcesAsync(Property property, CancellationToken cancellationToken = default);

    // Tenant Event Methods
    Task NotifyTenantOfRegistrationAsync(Tenant tenant, Property property, CancellationToken cancellationToken = default);
    Task NotifyTenantOfContactInfoChangeAsync(Tenant tenant, PersonContactInfo oldContactInfo, PersonContactInfo newContactInfo, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfNewTenantAsync(Tenant tenant, Property property, CancellationToken cancellationToken = default);
    Task NotifySuperintendentOfTenantContactChangeAsync(Tenant tenant, PersonContactInfo oldContactInfo, PersonContactInfo newContactInfo, CancellationToken cancellationToken = default);
    Task UpdateActiveRequestsWithNewContactInfoAsync(Tenant tenant, PersonContactInfo newContactInfo, CancellationToken cancellationToken = default);

    // Administrative Event Methods
    Task NotifyAdministratorsOfNewPropertyAsync(Property property, CancellationToken cancellationToken = default);
    Task NotifyAdministratorsOfNewWorkerAsync(Worker worker, CancellationToken cancellationToken = default);
    Task NotifyAdministratorsOfWorkerSpecializationChangeAsync(Worker worker, string? oldSpecialization, string newSpecialization, CancellationToken cancellationToken = default);
    
    // Capacity and Resource Management Methods
    Task UpdatePropertyOccupancyAsync(Property property, CancellationToken cancellationToken = default);
    Task UpdatePropertyCapacityAsync(Property property, CancellationToken cancellationToken = default);
    Task UpdateWorkforceCapacityAsync(Worker worker, bool isActive, CancellationToken cancellationToken = default);
}