using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Events.TenantRequests;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Domain.Entities;

/// <summary>
/// Rich domain entity with encapsulated business validation.
/// Single source of truth for all tenant request business rules.
/// </summary>
public class TenantRequest : BaseEntity
{
    // Private setters to enforce invariants
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TenantRequestStatus Status { get; private set; } = TenantRequestStatus.Draft;
    public string UrgencyLevel { get; private set; } = "Normal";
    public bool IsEmergency { get; private set; } = false;
    
    // Relationships
    public Guid TenantId { get; private set; }
    public Guid PropertyId { get; private set; }
    
    // Denormalized fields for performance (read-only)
    public string TenantFullName { get; private set; } = string.Empty;
    public string TenantEmail { get; private set; } = string.Empty;
    public string TenantUnit { get; private set; } = string.Empty;
    public string PropertyName { get; private set; } = string.Empty;
    public string PropertyPhone { get; private set; } = string.Empty;
    public string SuperintendentFullName { get; private set; } = string.Empty;
    public string SuperintendentEmail { get; private set; } = string.Empty;
    
    // Service work information
    public DateTime? ScheduledDate { get; private set; }
    public string? AssignedWorkerEmail { get; private set; }
    public string? AssignedWorkerName { get; private set; }
    public string? WorkOrderNumber { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public string? CompletionNotes { get; private set; }
    public string? ClosureNotes { get; private set; }
    public bool? WorkCompletedSuccessfully { get; private set; }
    
    // Tenant preferences
    public string? PreferredContactTime { get; private set; } = null;

    // Business validation constants - single source of truth
    private static readonly string[] _validUrgencyLevels = { "Low", "Normal", "High", "Critical", "Emergency" };
    private const int _maxTitleLength = 200;
    private const int _maxDescriptionLength = 1000;
    private const int _maxCodeLength = 50;

    // Status transition rules - moved from BusinessRulesEngine
    private static readonly Dictionary<TenantRequestStatus, List<TenantRequestStatus>> _allowedStatusTransitions = new()
    {
        [TenantRequestStatus.Draft] = [TenantRequestStatus.Submitted],
        [TenantRequestStatus.Submitted] = [TenantRequestStatus.Scheduled, TenantRequestStatus.Declined],
        [TenantRequestStatus.Scheduled] = [TenantRequestStatus.Done, TenantRequestStatus.Failed],
        [TenantRequestStatus.Failed] = [TenantRequestStatus.Scheduled],
        [TenantRequestStatus.Done] = [TenantRequestStatus.Closed],
        [TenantRequestStatus.Declined] = [TenantRequestStatus.Closed]
    };

    // Private constructor for EF Core
    private TenantRequest() { }


    public static TenantRequest CreateNew(
        string code,
        string title,
        string description,
        string urgencyLevel,
        Guid tenantId,
        Guid propertyId,
        string tenantFullName,
        string tenantEmail,
        string tenantUnit,
        string propertyName,
        string propertyPhone,
        string superintendentFullName,
        string superintendentEmail,
        string? preferredContactTime = null)
    {
        var request = new TenantRequest();
        request.InitializeNewRequest(code, title, description, urgencyLevel, tenantId, propertyId,
            tenantFullName, tenantEmail, tenantUnit, propertyName, propertyPhone, 
            superintendentFullName, superintendentEmail, preferredContactTime);
        
        request.AddDomainEvent(new TenantRequestCreatedEvent(request));
        return request;
    }

    /// <summary>
    /// Validates if a status transition is allowed.
    /// </summary>
    public bool IsStatusTransitionValid(TenantRequestStatus newStatus, TenantRequestStatusPolicy? statusPolicy = null)
    {
        if (statusPolicy != null)
        {
            return statusPolicy.IsValidStatusTransition(Status, newStatus);
        }

        // Fallback to entity's internal rules if no policy provided
        if (!_allowedStatusTransitions.TryGetValue(Status, out List<TenantRequestStatus>? value))
        {
            return false;
        }

        return value.Contains(newStatus);
    }

    /// <summary>
    /// Business operation with validation - domain encapsulation.
    /// </summary>
    public void SubmitForReview()
    {
        ValidateCanBeSubmitted();
        
        Status = TenantRequestStatus.Submitted;
        AddDomainEvent(new TenantRequestSubmittedEvent(this));
    }

    /// <summary>
    /// Backward compatibility method - maps to new method name.
    /// </summary>
    public void Submit() => SubmitForReview();

    /// <summary>
    /// Business operation with complex validation.
    /// </summary>
    public void ScheduleWork(DateTime scheduledDate, string workerEmail, string workOrderNumber, string? workerName = null)
    {
        ValidateCanBeScheduled(scheduledDate, workerEmail, workOrderNumber);
        
        ScheduledDate = scheduledDate;
        AssignedWorkerEmail = workerEmail;
        AssignedWorkerName = workerName;
        WorkOrderNumber = workOrderNumber;
        Status = TenantRequestStatus.Scheduled;
        
        AddDomainEvent(new TenantRequestScheduledEvent(this, new ServiceWorkScheduleInfo(scheduledDate, workerEmail, workOrderNumber, 1)));
    }

    /// <summary>
    /// Backward compatibility method - maps to new method name.
    /// </summary>
    public void Schedule(DateTime scheduledDate, string workerEmail, string workOrderNumber, string? workerName = null) => 
        ScheduleWork(scheduledDate, workerEmail, workOrderNumber, workerName);

    /// <summary>
    /// Business operation with validation.
    /// </summary>
    public void ReportWorkCompleted(bool successful, string? completionNotes = null)
    {
        ValidateCanBeCompleted();
        
        Status = successful ? TenantRequestStatus.Done : TenantRequestStatus.Failed;
        CompletedDate = DateTime.UtcNow;
        WorkCompletedSuccessfully = successful;
        CompletionNotes = completionNotes;
        
        AddDomainEvent(new TenantRequestCompletedEvent(this, completionNotes ?? ""));
    }

    /// <summary>
    /// Business operation with validation.
    /// </summary>
    public void DeclineRequest(string reason)
    {
        ValidateCanBeDeclined();
        
        Status = TenantRequestStatus.Declined;
        AddDomainEvent(new TenantRequestDeclinedEvent(this, reason));
    }

    /// <summary>
    /// Marks scheduled work as failed due to emergency override
    /// Follows existing domain pattern for work interruption scenarios
    /// </summary>
    /// <param name="reason">Reason for the emergency override cancellation</param>
    public void FailDueToEmergencyOverride(string reason)
    {
        ValidateCanBeFailed();
        
        Status = TenantRequestStatus.Failed;
        CompletionNotes = $"Work cancelled due to emergency override: {reason}";

        // Clear worker assignment since work was interrupted
        string? previousWorker = AssignedWorkerEmail;
        string? previousWorkOrder = WorkOrderNumber;
        DateTime? previousScheduledDate = ScheduledDate;
        
        AssignedWorkerEmail = null;
        WorkOrderNumber = null;
        ScheduledDate = null;
        AssignedWorkerName = null;
        
        // Store audit information in closure notes
        ClosureNotes = $"Emergency override cancelled assignment: {previousWorker} ({previousWorkOrder}) on {previousScheduledDate:yyyy-MM-dd}";
        
        AddDomainEvent(new TenantRequestCompletedEvent(this, CompletionNotes));
    }

    /// <summary>
    /// Closes the request with provided notes
    /// </summary>
    /// <param name="closureNotes">Notes about why the request was closed</param>
    public void Close(string closureNotes)
    {
        ValidateCanBeClosed();
        
        Status = TenantRequestStatus.Closed;
        ClosureNotes = closureNotes;
        
        AddDomainEvent(new TenantRequestClosedEvent(this, closureNotes));
    }

    /// <summary>
    /// Backward compatibility method - maps to new method name.
    /// </summary>
    public void Decline(string reason) => DeclineRequest(reason);

    /// <summary>
    /// Enhanced method for updating tenant information with business validation.
    /// </summary>
    public void UpdateTenantInformation(string tenantFullName, string tenantEmail, string tenantUnit, string propertyName)
    {
        // Business rule: Cannot update tenant info for completed or closed requests
        if (Status is TenantRequestStatus.Done or TenantRequestStatus.Failed or TenantRequestStatus.Closed)
        {
            throw new TenantRequestDomainException($"Cannot update tenant information for request in {Status} status");
        }

        TenantFullName = ValidateRequiredString(tenantFullName, nameof(tenantFullName));
        TenantEmail = ValidateEmail(tenantEmail);
        TenantUnit = ValidateRequiredString(tenantUnit, nameof(tenantUnit));
        PropertyName = ValidateRequiredString(propertyName, nameof(propertyName));

        AddDomainEvent(new TenantRequestTenantInfoUpdatedEvent(this));
    }

 
 





    #region Private Initialization Methods



    /// <summary>
    /// FIXED: Initialize from aggregate IDs - reduces parameter coupling.
    /// </summary>
    private void InitializeFromAggregateIds(
        string code,
        string title,
        string description,
        string urgencyLevel,
        Guid tenantId,
        Guid propertyId,
        string tenantFullName,
        string tenantEmail,
        string tenantUnit,
        string propertyName,
        string propertyPhone,
        string superintendentFullName,
        string superintendentEmail)
    {
        Code = ValidateCode(code);
        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        UrgencyLevel = ValidateUrgencyLevel(urgencyLevel);
        IsEmergency = DetermineIfEmergency(urgencyLevel);
        
        ValidateTenantId(tenantId);
        ValidatePropertyId(propertyId);
        
        TenantId = tenantId;
        PropertyId = propertyId;
        TenantFullName = ValidateRequiredString(tenantFullName, nameof(tenantFullName));
        TenantEmail = ValidateEmail(tenantEmail);
        TenantUnit = ValidateRequiredString(tenantUnit, nameof(tenantUnit));
        PropertyName = ValidateRequiredString(propertyName, nameof(propertyName));
        PropertyPhone = ValidateRequiredString(propertyPhone, nameof(propertyPhone));
        SuperintendentFullName = ValidateRequiredString(superintendentFullName, nameof(superintendentFullName));
        SuperintendentEmail = ValidateEmail(superintendentEmail);
    }

    private void InitializeNewRequest(
        string code,
        string title,
        string description,
        string urgencyLevel,
        Guid tenantId,
        Guid propertyId,
        string tenantFullName,
        string tenantEmail,
        string tenantUnit,
        string propertyName,
        string propertyPhone,
        string superintendentFullName,
        string superintendentEmail,
        string? preferredContactTime = null)
    {
        Code = ValidateCode(code);
        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        UrgencyLevel = ValidateUrgencyLevel(urgencyLevel);
        IsEmergency = DetermineIfEmergency(urgencyLevel);
        
        ValidateTenantId(tenantId);
        ValidatePropertyId(propertyId);
        
        TenantId = tenantId;
        PropertyId = propertyId;
        TenantFullName = ValidateRequiredString(tenantFullName, nameof(tenantFullName));
        TenantEmail = ValidateEmail(tenantEmail);
        TenantUnit = ValidateRequiredString(tenantUnit, nameof(tenantUnit));
        PropertyName = ValidateRequiredString(propertyName, nameof(propertyName));
        PropertyPhone = ValidateRequiredString(propertyPhone, nameof(propertyPhone));
        SuperintendentFullName = ValidateRequiredString(superintendentFullName, nameof(superintendentFullName));
        SuperintendentEmail = ValidateEmail(superintendentEmail);
        PreferredContactTime = preferredContactTime?.Trim();
    }

    #endregion

    #region Private Validation Methods - Single Source of Truth

    /// <summary>
    /// Domain validation - single source of truth for code rules.
    /// </summary>
    private static string ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new TenantRequestDomainException("Request code cannot be empty");
        }
            
        code = code.Trim();
        
        if (code.Length > _maxCodeLength)
        {
            throw new TenantRequestDomainException($"Request code cannot exceed {_maxCodeLength} characters");
        }
            
        return code;
    }

    /// <summary>
    /// Domain validation - single source of truth for title rules.
    /// </summary>
    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new TenantRequestDomainException("Request title cannot be empty");
        }
            
        title = title.Trim();
        
        if (title.Length > _maxTitleLength)
        {
            throw new TenantRequestDomainException($"Request title cannot exceed {_maxTitleLength} characters");
        }
            
        return title;
    }

    /// <summary>
    /// Domain validation - single source of truth for description rules.
    /// </summary>
    private static string ValidateDescription(string description)
    {
        description = description?.Trim() ?? string.Empty;
        
        if (description.Length > _maxDescriptionLength)
        {
            throw new TenantRequestDomainException($"Request description cannot exceed {_maxDescriptionLength} characters");
        }
            
        return description;
    }

    /// <summary>
    /// Domain validation - single source of truth for urgency level rules.
    /// </summary>
    private static string ValidateUrgencyLevel(string urgencyLevel)
    {
        if (string.IsNullOrWhiteSpace(urgencyLevel))
        {
            throw new TenantRequestDomainException("Urgency level cannot be empty");
        }
            
        if (!_validUrgencyLevels.Contains(urgencyLevel))
        {
            throw new TenantRequestDomainException($"Invalid urgency level '{urgencyLevel}'. Valid values: {string.Join(", ", _validUrgencyLevels)}");
        }
            
        return urgencyLevel;
    }

    /// <summary>
    /// Business logic - determines emergency status based on urgency.
    /// </summary>
    private static bool DetermineIfEmergency(string urgencyLevel)
    {
        return urgencyLevel is "Critical" or "Emergency";
    }

    /// <summary>
    /// Domain validation - business rule for submission.
    /// </summary>
    private void ValidateCanBeSubmitted()
    {
        if (Status != TenantRequestStatus.Draft)
        {
            throw new TenantRequestDomainException($"Request can only be submitted from Draft status. Current status: {Status}");
        }
            
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new TenantRequestDomainException("Request title is required for submission");
        }
            
        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new TenantRequestDomainException("Request description is required for submission");
        }
    }

    /// <summary>
    /// Domain validation - business rules for scheduling.
    /// Updated to allow rescheduling of Failed requests since failed work should be reschedulable.
    /// </summary>
    public void ValidateCanBeScheduled(DateTime scheduledDate, string workerEmail, string workOrderNumber)
    {
        // Business rule: Only submitted and failed requests can be scheduled
        // Failed requests should be reschedulable since the work didn't complete successfully
        if (Status != TenantRequestStatus.Submitted && Status != TenantRequestStatus.Failed)
        {
            throw new TenantRequestDomainException($"Request can only be scheduled from Submitted or Failed status. Current status: {Status}");
        }
            
        if (scheduledDate <= DateTime.UtcNow)
        {
            throw new TenantRequestDomainException("Scheduled date must be in the future");
        }
            
        if (string.IsNullOrWhiteSpace(workerEmail))
        {
            throw new TenantRequestDomainException("Worker email is required for scheduling");
        }
            
        if (string.IsNullOrWhiteSpace(workOrderNumber))
        {
            throw new TenantRequestDomainException("Work order number is required for scheduling");
        }
    }

    private void ValidateCanBeCompleted()
    {
        if (Status != TenantRequestStatus.Scheduled)
        {
            throw new TenantRequestDomainException($"Request can only be completed from Scheduled status. Current status: {Status}");
        }
    }

    private void ValidateCanBeDeclined()
    {
        if (Status != TenantRequestStatus.Submitted)
        {
            throw new TenantRequestDomainException($"Request can only be declined from Submitted status. Current status: {Status}");
        }
    }

    private void ValidateCanBeFailed()
    {
        if (Status != TenantRequestStatus.Scheduled)
        {
            throw new TenantRequestDomainException($"Request can only be failed from Scheduled status. Current status: {Status}");
        }
        
        if (string.IsNullOrWhiteSpace(AssignedWorkerEmail))
        {
            throw new TenantRequestDomainException("Cannot fail request - no worker assignment found");
        }
    }

    private void ValidateCanBeClosed()
    {
        if (Status is not (TenantRequestStatus.Done or TenantRequestStatus.Failed))
        {
            throw new TenantRequestDomainException($"Request can only be closed from Done or Failed status. Current status: {Status}");
        }
    }

    private static void ValidateTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new TenantRequestDomainException("Tenant ID must be a valid Guid");
        }
    }

    private static void ValidatePropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
        {
            throw new TenantRequestDomainException("Property ID must be a valid Guid");
        }
    }

    private static string ValidateRequiredString(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TenantRequestDomainException($"{fieldName} cannot be empty");
        }
        return value.Trim();
    }

    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new TenantRequestDomainException("Email address cannot be empty");
        }
            
        // Basic email validation
        if (!email.Contains("@"))
        {
            throw new TenantRequestDomainException("Email address must be valid");
        }
            
        return email.Trim().ToLowerInvariant();
    }

    #endregion

    /// <summary>
    /// Business query method - determines if request needs immediate attention.
    /// </summary>
    public bool RequiresImmediateAttention()
    {
        return IsEmergency || (Status == TenantRequestStatus.Submitted && CreatedAt <= DateTime.UtcNow.AddDays(-2));
    }

    /// <summary>
    /// Business query method - calculates service level based on urgency.
    /// </summary>
    public int GetExpectedResolutionHours()
    {
        return UrgencyLevel switch
        {
            "Emergency" => 2,
            "Critical" => 4,
            "High" => 24,
            "Normal" => 72,
            "Low" => 168,
            _ => 72
        };
    }

    /// <summary>
    /// Domain method to check if request is currently active.
    /// Used by business rules for pending request validation.
    /// </summary>
    public bool IsActive() => Status is TenantRequestStatus.Submitted or TenantRequestStatus.Scheduled;

    /// <summary>
    /// Sets the tenant's preferred contact time (public method for command handlers)
    /// </summary>
    public void SetPreferredContactTime(string? preferredContactTime)
    {
        PreferredContactTime = preferredContactTime?.Trim();
    }

    /// <summary>
    /// Business logic: Determines if request is overdue.
    /// Encapsulates the business rule for overdue calculation.
    /// </summary>
    public bool IsOverdue(TenantRequestStatusPolicy? statusPolicy = null)
    {
        TenantRequestStatusPolicy policy = statusPolicy ?? new TenantRequestStatusPolicy();
        
        if (policy.IsCompletedStatus(Status))
        {
            return false;
        }

        int expectedHours = GetExpectedResolutionHours();
        TimeSpan timeInProcess = DateTime.UtcNow - CreatedAt;
        return timeInProcess.TotalHours > expectedHours;
    }

    /// <summary>
    /// Business logic: Calculates resolution performance score.
    /// </summary>
    public double CalculateResolutionPerformanceScore()
    {
        if (!CompletedDate.HasValue)
        {
            return 0;
        }

        double actualHours = (CompletedDate.Value - CreatedAt).TotalHours;
        int expectedHours = GetExpectedResolutionHours();
        
        if (actualHours <= expectedHours)
        {
            return 100; // On time = perfect score
        }

        // Calculate penalty for being late
        double latePenalty = Math.Min(50, (actualHours - expectedHours) / expectedHours * 50);
        return Math.Max(0, 100 - latePenalty);
    }

    /// <summary>
    /// Business logic: Calculates urgency priority score.
    /// </summary>
    public int CalculateUrgencyPriority()
    {
        int priority = 0;
        
        if (IsEmergency)
        {
            priority += 100;
        }

        if (Status == TenantRequestStatus.Failed)
        {
            priority += 50;
        }

        if (IsOverdue())
        {
            priority += 25;
        }

        return priority;
    }

    /// <summary>
    /// Business logic: Determines if request contributes to first-call resolution rate.
    /// </summary>
    public bool IsFirstCallResolution()
    {
        return Status != TenantRequestStatus.Failed;
    }

    /// <summary>
    /// Business logic: Calculates age in days for age distribution analysis.
    /// Business rule for request age categorization.
    /// </summary>
    public int GetAgeInDays()
    {
        return (DateTime.UtcNow - CreatedAt).Days;
    }

    /// <summary>
    /// Business logic: Gets age category for dashboard analytics.
    /// </summary>
    public string GetAgeCategory()
    {
        int days = GetAgeInDays();
        
        return days switch
        {
            <= 1 => "0-1 days",
            <= 3 => "1-3 days", 
            <= 7 => "3-7 days",
            _ => "7+ days"
        };
    }

    /// <summary>
    /// Business logic: Determines category from request description.
    /// </summary>
    public string DetermineCategoryFromDescription()
    {
        string text = Description.ToLowerInvariant();
        
        if (text.Contains("plumb") || text.Contains("water") || text.Contains("leak"))
        {
            return "Plumbing";
        }

        if (text.Contains("electric") || text.Contains("power") || text.Contains("light"))
        {
            return "Electrical";
        }

        if (text.Contains("heat") || text.Contains("air") || text.Contains("hvac"))
        {
            return "HVAC";
        }

        if (text.Contains("lock") || text.Contains("door") || text.Contains("window"))
        {
            return "Security/Access";
        }

        if (text.Contains("paint") || text.Contains("wall") || text.Contains("ceiling"))
        {
            return "Cosmetic";
        }

        return "General Maintenance";
    }

    /// <summary>
    /// Business logic: Determines if request was resolved on time for satisfaction scoring.
    /// Used in customer satisfaction calculations.
    /// </summary>
    public bool WasResolvedOnTime()
    {
        if (!CompletedDate.HasValue)
        {
            return false;
        }

        TimeSpan resolutionTime = CompletedDate.Value - CreatedAt;
        int expectedHours = GetExpectedResolutionHours();
        
        return resolutionTime.TotalHours <= expectedHours;
    }

    /// <summary>
    /// Business logic: Determines if emergency request was handled well.
    /// Specific business rule for emergency request satisfaction scoring.
    /// </summary>
    public bool WasEmergencyHandledWell()
    {
        if (!IsEmergency || !CompletedDate.HasValue)
        {
            return false;
        }

        TimeSpan responseTime = CompletedDate.Value - CreatedAt;
        return responseTime.TotalHours <= 2; // Emergency should be handled within 2 hours
    }
}
