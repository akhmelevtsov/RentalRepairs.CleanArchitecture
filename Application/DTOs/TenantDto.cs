namespace RentalRepairs.Application.DTOs;

public class TenantDto
{
    public Guid Id { get; set; }
    public string UnitNumber { get; set; } = default!;
    public PersonContactInfoDto ContactInfo { get; set; } = default!;

    // Property information (flattened)
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = default!;
    public string PropertyName { get; set; } = default!;

    public DateTime RegistrationDate { get; set; }

    // ? Replace circular reference with summary statistics
    public int TotalRequests { get; set; }
    public int ActiveRequestsCount { get; set; }
    public int CompletedRequestsCount { get; set; }
    public DateTime? LastRequestDate { get; set; }
    public DateTime? NextScheduledServiceDate { get; set; }

    // ? UI-optimized properties
    public bool HasActiveRequests => ActiveRequestsCount > 0;
    public bool HasScheduledService => NextScheduledServiceDate.HasValue;
}

public class WorkerDto
{
    public Guid Id { get; set; }
    public PersonContactInfoDto ContactInfo { get; set; } = default!;
    public string? Specialization { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime RegistrationDate { get; set; }

    // ? Add summary statistics instead of complex relationships
    public int ActiveAssignmentsCount { get; set; }
    public int CompletedAssignmentsCount { get; set; }
    public DateTime? NextScheduledWork { get; set; }

    // ? UI-optimized properties
    public bool IsAvailable => IsActive && ActiveAssignmentsCount < 5; // Business logic
    public string DisplayName => ContactInfo?.FullName ?? "Unknown Worker";
}