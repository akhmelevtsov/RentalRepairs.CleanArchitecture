using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Events.Workers;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Entities;

/// <summary>
/// Worker entity representing a maintenance worker in the rental repairs system.
/// Contains business logic for work assignments, specializations, and availability.
/// </summary>
public class Worker : BaseEntity, IAggregateRoot
{
    #region Private Fields

    private readonly List<WorkAssignment> _assignments = new();

    #endregion

    #region Constructors

    protected Worker() 
    { 
        // For EF Core
        ContactInfo = null!;
    }

    public Worker(PersonContactInfo contactInfo) : base()
    {
        ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
        IsActive = true;
        AddDomainEvent(new WorkerRegisteredEvent(this));
    }

    #endregion

    #region Properties

    public PersonContactInfo ContactInfo { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public string? Specialization { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<WorkAssignment> Assignments => _assignments.AsReadOnly();

    #endregion

    #region Public Business Methods


    public void SetSpecialization(string specialization)
    {
        if (string.IsNullOrWhiteSpace(specialization))
        {
            throw new ArgumentException("Specialization cannot be empty", nameof(specialization));
        }

        string? oldSpecialization = Specialization;
        Specialization = specialization;

        if (oldSpecialization != specialization)
        {
            AddDomainEvent(new WorkerSpecializationChangedEvent(this, oldSpecialization, specialization));
        }
    }

    public void AddNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return;
        }

        Notes = string.IsNullOrWhiteSpace(Notes) 
            ? notes 
            : $"{Notes}\n{DateTime.UtcNow:yyyy-MM-dd}: {notes}";
    }

    public void Deactivate(string reason = "")
    {
        IsActive = false;
        AddNotes($"Worker deactivated. Reason: {reason}");
        AddDomainEvent(new WorkerDeactivatedEvent(this, reason));
    }

    public void Activate()
    {
        IsActive = true;
        AddNotes("Worker reactivated");
        AddDomainEvent(new WorkerActivatedEvent(this));
    }

    /// <summary>
    /// Validates if this worker can be assigned to a tenant request for scheduling.
    /// </summary>
    /// <returns>True if the worker can be assigned to requests.</returns>
    public bool CanBeAssignedToRequest()
    {
        return IsActive;
    }



    /// <summary>
    /// Checks if the worker is available for work on the specified date.
    /// Updated to prevent time-based conflicts and limit daily assignments.
    /// </summary>
    /// <param name="workDate">The date to check availability for.</param>
    /// <param name="duration">Optional duration (currently not used in logic).</param>
    /// <returns>True if the worker is available.</returns>
    public bool IsAvailableForWork(DateTime workDate, TimeSpan? duration = null)
    {
        if (!IsActive)
        {
            return false;
        }

        if (workDate < DateTime.UtcNow.Date)
        {
            return false;
        }

        // Check internal assignments for availability
        DateTime startOfDay = workDate.Date;
        DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var assignmentsOnDate = _assignments.Where(a => 
            a.ScheduledDate >= startOfDay && 
            a.ScheduledDate <= endOfDay &&
            !a.IsCompleted).ToList();

        // Business Rule: Limit to 2 assignments per day (reduced from 3)
        if (assignmentsOnDate.Count >= 2)
        {
            return false;
        }

        // Business Rule: Check for time-based conflicts if we have a specific time
        if (duration.HasValue)
        {
            DateTime proposedEnd = workDate.Add(duration.Value);
            
            foreach (WorkAssignment? assignment in assignmentsOnDate)
            {
                // Assume each assignment takes 4 hours
                DateTime assignmentEnd = assignment.ScheduledDate.Add(TimeSpan.FromHours(4));
                
                // Check for overlap
                if (workDate < assignmentEnd && proposedEnd > assignment.ScheduledDate)
                {
                    return false; // Time conflict detected
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Assigns work to this worker with enhanced conflict detection.
    /// </summary>
    /// <param name="workOrderNumber">The work order number.</param>
    /// <param name="scheduledDate">The scheduled date for the work.</param>
    /// <param name="notes">Optional notes for the assignment.</param>
    public void AssignToWork(string workOrderNumber, DateTime scheduledDate, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(workOrderNumber))
        {
            throw new ArgumentException("Work order number cannot be empty", nameof(workOrderNumber));
        }

        if (scheduledDate <= DateTime.UtcNow)
        {
            throw new ArgumentException("Scheduled date must be in the future", nameof(scheduledDate));
        }

        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot assign work to inactive worker");
        }

        // Enhanced availability check with time-based conflict detection
        DateTime startOfDay = scheduledDate.Date;
        DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var assignmentsOnDate = _assignments.Where(a => 
            a.ScheduledDate >= startOfDay && 
            a.ScheduledDate <= endOfDay &&
            !a.IsCompleted).ToList();

        // Business Rule: Limit to 2 assignments per day
        if (assignmentsOnDate.Count >= 2)
        {
            throw new InvalidOperationException($"Worker already has {assignmentsOnDate.Count} assignments on {scheduledDate:yyyy-MM-dd}. Maximum is 2 per day.");
        }

        // Business Rule: Check for time-based conflicts (assume 4-hour duration per assignment)
        DateTime proposedEnd = scheduledDate.Add(TimeSpan.FromHours(4));
        
        foreach (WorkAssignment? existingAssignment in assignmentsOnDate)
        {
            DateTime assignmentEnd = existingAssignment.ScheduledDate.Add(TimeSpan.FromHours(4));
            
            // Check for overlap
            if (scheduledDate < assignmentEnd && proposedEnd > existingAssignment.ScheduledDate)
            {
                throw new InvalidOperationException(
                    $"Worker has a time conflict on {scheduledDate:yyyy-MM-dd}. " +
                    $"Existing assignment at {existingAssignment.ScheduledDate:HH:mm} would overlap with proposed assignment at {scheduledDate:HH:mm}.");
            }
        }

        // Create and add the assignment
        var newAssignment = new WorkAssignment(workOrderNumber, scheduledDate, notes);
        _assignments.Add(newAssignment);

        AddDomainEvent(new WorkerAssignedEvent(this, newAssignment));
    }

    /// <summary>
    /// Completes work for the specified work order.
    /// </summary>
    /// <param name="workOrderNumber">The work order number to complete.</param>
    /// <param name="successful">Whether the work was completed successfully.</param>
    /// <param name="completionNotes">Optional completion notes.</param>
    public void CompleteWork(string workOrderNumber, bool successful, string? completionNotes = null)
    {
        WorkAssignment? assignment = _assignments.FirstOrDefault(a => a.WorkOrderNumber == workOrderNumber);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Work order '{workOrderNumber}' not found for this worker");
        }

        // Create a new completed assignment and replace the old one
        WorkAssignment completedAssignment = assignment.Complete(successful, completionNotes);
        
        _assignments.Remove(assignment);
        _assignments.Add(completedAssignment);

        AddDomainEvent(new WorkCompletedEvent(this, completedAssignment, successful, completionNotes));
    }

    /// <summary>
    /// Gets the count of upcoming work assignments.
    /// </summary>
    /// <param name="fromDate">The starting date to count from.</param>
    /// <param name="daysAhead">The number of days ahead to include.</param>
    /// <returns>The count of upcoming assignments.</returns>
    public int GetUpcomingWorkloadCount(DateTime fromDate, int daysAhead = 7)
    {
        DateTime endDate = fromDate.AddDays(daysAhead);
        
        return _assignments.Count(a => 
            a.ScheduledDate >= fromDate && 
            a.ScheduledDate <= endDate &&
            !a.IsCompleted);
    }

    /// <summary>
    /// Checks if the worker has the required specialized skills.
    /// </summary>
    /// <param name="requiredSpecialization">The required specialization.</param>
    /// <returns>True if the worker has the required skills.</returns>
    public bool HasSpecializedSkills(string requiredSpecialization)
    {
        if (string.IsNullOrWhiteSpace(requiredSpecialization))
        {
            return true; // No specific specialization required
        }

        if (string.IsNullOrWhiteSpace(Specialization))
        {
            return requiredSpecialization.Equals("General Maintenance", StringComparison.OrdinalIgnoreCase);
        }

        // Exact match
        if (Specialization.Equals(requiredSpecialization, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // General Maintenance can handle anything
        if (Specialization.Equals("General Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Handle common specialization variations
        string normalizedWorkerSpec = NormalizeSpecialization(Specialization);
        string normalizedRequiredSpec = NormalizeSpecialization(requiredSpecialization);

        return normalizedWorkerSpec.Equals(normalizedRequiredSpec, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines the required specialization based on the work title and description.
    /// </summary>
    /// <param name="title">The work title.</param>
    /// <param name="description">The work description.</param>
    /// <returns>The required specialization.</returns>
    public static string DetermineRequiredSpecialization(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            return "General Maintenance";
        }

        string text = $"{title} {description}".ToLowerInvariant();

        // Simple keyword-based specialization determination
        // IMPORTANT: Order matters! Check more specific keywords before general ones
        
        if (ContainsKeywords(text, "plumb", "leak", "water", "drain", "pipe", "faucet", "toilet"))
        {
            return "Plumbing";
        }

        if (ContainsKeywords(text, "electric", "power", "outlet", "wiring", "light", "switch", "breaker"))
        {
            return "Electrical";
        }

        if (ContainsKeywords(text, "heat", "hvac", "air", "furnace", "thermostat", "cooling", "ventilation"))
        {
            return "HVAC";
        }

        // Check Locksmith BEFORE Carpentry since "lock" is more specific than "door"
        if (ContainsKeywords(text, "lock", "key", "security", "deadbolt"))
        {
            return "Locksmith";
        }

        if (ContainsKeywords(text, "paint", "wall", "ceiling", "trim", "brush", "roller"))
        {
            return "Painting";
        }

        // Check Carpentry after Locksmith to avoid "door" conflicts with "lock"
        if (ContainsKeywords(text, "wood", "cabinet", "door", "frame", "carpenter", "build"))
        {
            return "Carpentry";
        }

        if (ContainsKeywords(text, "appliance", "refrigerator", "washer", "dryer", "dishwasher", "oven"))
        {
            return "Appliance Repair";
        }

        return "General Maintenance"; // Default
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Normalizes specialization names to handle common variations.
    /// </summary>
    /// <param name="specialization">The specialization to normalize.</param>
    /// <returns>The normalized specialization name.</returns>
    private static string NormalizeSpecialization(string specialization)
    {
        if (string.IsNullOrWhiteSpace(specialization))
        {
            return "General Maintenance";
        }

        string normalized = specialization.Trim();

        // Handle common variations
        return normalized.ToLowerInvariant() switch
        {
            "plumber" => "Plumbing",
            "plumbing" => "Plumbing",
            "electrician" => "Electrical", 
            "electrical" => "Electrical",
            "hvac" => "HVAC",
            "hvac technician" => "HVAC",
            "heating" => "HVAC",
            "cooling" => "HVAC",
            "painter" => "Painting",
            "painting" => "Painting",
            "carpenter" => "Carpentry",
            "carpentry" => "Carpentry",
            "locksmith" => "Locksmith",
            "appliance repair" => "Appliance Repair",
            "appliance technician" => "Appliance Repair",
            "general maintenance" => "General Maintenance",
            "maintenance" => "General Maintenance",
            _ => specialization // Return original if no mapping found
        };
    }

    private static bool ContainsKeywords(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }

    #endregion

    #region Worker Assignment Validation

    /// <summary>
    /// Domain method to validate if this worker can be assigned to a request.
    /// Follows Option 2: Rich Domain Entity Approach for business rule validation.
    /// </summary>
    /// <param name="scheduledDate">The proposed scheduled date for the work</param>
    /// <param name="workOrderNumber">The work order number</param>
    /// <param name="requiredSpecialization">Optional required specialization</param>
    public void ValidateCanBeAssignedToRequest(DateTime scheduledDate, string workOrderNumber, string? requiredSpecialization = null)
    {
        // Business rule: Worker must be active
        if (!IsActive)
        {
            throw new WorkerNotAvailableException(ContactInfo.EmailAddress, "Worker is inactive");
        }

        // Business rule: Scheduled date must be in the future
        if (scheduledDate <= DateTime.UtcNow)
        {
            throw new InvalidAssignmentParametersException(
                nameof(scheduledDate), 
                scheduledDate, 
                "Scheduled date must be in the future");
        }

        // Business rule: Work order number is required
        if (string.IsNullOrWhiteSpace(workOrderNumber))
        {
            throw new InvalidAssignmentParametersException(
                nameof(workOrderNumber), 
                workOrderNumber, 
                "Work order number is required");
        }

        // Business rule: Worker must be available on the scheduled date
        if (!IsAvailableForWork(scheduledDate))
        {
            throw new WorkerNotAvailableException(
                ContactInfo.EmailAddress, 
                $"Worker is not available on {scheduledDate:yyyy-MM-dd}");
        }

        // Business rule: Worker must have required specialization
        if (!string.IsNullOrWhiteSpace(requiredSpecialization) && 
            !HasSpecializedSkills(requiredSpecialization))
        {
            throw new WorkerNotAvailableException(
                ContactInfo.EmailAddress, 
                $"Worker does not have required specialization: {requiredSpecialization}");
        }
    }

 
    #endregion

    #region Slot-Based Availability

    /// <summary>
    /// Checks if the worker is available for a specific scheduling slot
    /// </summary>
    public bool IsAvailableForSlot(SchedulingSlot slot)
    {
        if (!IsActive)
        {
            return false;
        }

        if (slot.Date < DateTime.Today)
        {
            return false;
        }

        // Check internal assignments for slot conflicts
        IEnumerable<WorkAssignment> existingAssignments = _assignments.Where(a => 
            a.ScheduledDate.Date == slot.Date && 
            !a.IsCompleted);

        foreach (WorkAssignment? assignment in existingAssignments)
        {
            // Create a slot for the existing assignment (assume 4-hour window)
            TimeSpan assignmentStart = assignment.ScheduledDate.TimeOfDay;
            TimeSpan assignmentEnd = assignmentStart.Add(TimeSpan.FromHours(4));
            var assignmentSlot = new SchedulingSlot(slot.Date, assignmentStart, assignmentEnd);
            
            if (slot.OverlapsWith(assignmentSlot))
            {
                return false;
            }
        }

        return true;
    }


 
    /// <summary>
    /// Checks if the worker is certified for emergency response.
    /// Business rule for emergency request assignment.
    /// </summary>
    public bool IsEmergencyResponseCapable()
    {
        // Business rule: Only active workers with specific specializations can handle emergencies
        if (!IsActive)
        {
            return false;
        }

        // For now, assume all workers can handle emergencies
        // In a real system, this would check certifications
        return true;
    }

    #endregion

    #region Request Assignment Business Logic - Step 2: Push Logic to Aggregates

    /// <summary>
    /// Calculates this worker's score for a specific tenant request.
    /// Encapsulates worker-specific scoring logic within the aggregate.
    /// </summary>
    /// <param name="request">The tenant request to score against</param>
    /// <returns>Score where higher values indicate better suitability</returns>
    public int CalculateScoreForRequest(TenantRequest request)
    {
        int score = 0;

        // Base score for active workers
        if (!IsActive)
        {
            return 0; // Inactive workers get no score
        }

        score += 100; // Base score for active worker

        // Score based on specialization match (higher is better)
        string requiredSpecialization = DetermineRequiredSpecialization(request.Title, request.Description);
        if (HasSpecializedSkills(requiredSpecialization))
        {
            // Give exact match the highest score
            if (Specialization?.Equals(requiredSpecialization, StringComparison.OrdinalIgnoreCase) == true)
            {
                score += 200; // Exact specialization match gets highest score
            }
            else
            {
                score += 100; // General maintenance or other specialization
            }
        }

        // Score based on availability
        if (IsAvailableForWork(DateTime.Today.AddDays(1)))
        {
            score += 50;
        }

        // Score based on current workload (lower workload = better score)
        int upcomingWorkload = GetUpcomingWorkloadCount(DateTime.UtcNow);
        score += Math.Max(0, (10 - upcomingWorkload) * 10); // Less workload = higher score

        // Emergency request handling bonus
        if (request.IsEmergency && IsEmergencyResponseCapable())
        {
            score += 30;
        }

        return score;
    }

    /// <summary>
    /// Determines if this worker can be assigned to a specific tenant request.
    /// Encapsulates all worker-specific assignment business rules within the aggregate.
    /// </summary>
    /// <param name="request">The tenant request to check</param>
    /// <returns>True if the worker can be assigned to the request</returns>
    public bool CanBeAssignedToRequest(TenantRequest request)
    {
        if (request == null)
        {
            return false;
        }

        // Business rule 1: Worker must be active
        if (!IsActive)
        {
            return false;
        }

        // Business rule 2: Request must be in assignable status
        if (!IsRequestAssignable(request))
        {
            return false;
        }

        // Business rule 3: Worker must have required specialization
        string requiredSpecialization = DetermineRequiredSpecialization(request.Title, request.Description);
        if (!HasSpecializedSkills(requiredSpecialization))
        {
            return false;
        }

        // Business rule 4: Worker must be available (not overloaded)
        if (IsOverloaded())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates this worker's confidence level for completing a specific request.
    /// Encapsulates worker-specific confidence assessment within the aggregate.
    /// </summary>
    /// <param name="request">The tenant request to assess</param>
    /// <returns>Confidence level between 0.0 and 1.0</returns>
    public double CalculateRecommendationConfidence(TenantRequest request)
    {
        if (request == null || !IsActive)
        {
            return 0.0;
        }

        string requiredSpecialization = DetermineRequiredSpecialization(request.Title, request.Description);

        if (!HasSpecializedSkills(requiredSpecialization))
        {
            return 0.70; // Lower confidence without specialization match
        }

        // Higher confidence for exact specialization match
        if (Specialization?.Equals(requiredSpecialization, StringComparison.OrdinalIgnoreCase) == true)
        {
            return request.IsEmergency ? 0.95 : 0.90;
        }
        else
        {
            return request.IsEmergency ? 0.85 : 0.80;
        }

    }

    /// <summary>
    /// Generates reasoning for why this worker is recommended for a request.
    /// Encapsulates worker-specific reasoning logic within the aggregate.
    /// </summary>
    /// <param name="request">The tenant request to generate reasoning for</param>
    /// <returns>Human-readable reasoning string</returns>
    public string GenerateRecommendationReasoning(TenantRequest request)
    {
        if (!IsActive)
        {
            return "Worker is inactive";
        }

        var reasons = new List<string>();

        string requiredSpecialization = DetermineRequiredSpecialization(request.Title, request.Description);
        if (HasSpecializedSkills(requiredSpecialization))
        {
            reasons.Add(Specialization?.Equals(requiredSpecialization, StringComparison.OrdinalIgnoreCase) == true
                ? $"Has exact {requiredSpecialization} specialization"
                : $"Can handle {requiredSpecialization} work");
        }

        if (IsAvailableForWork(DateTime.Today.AddDays(1)))
        {
            reasons.Add("Available for immediate assignment");
        }

        if (request.IsEmergency && IsEmergencyResponseCapable())
        {
            reasons.Add("Qualified for emergency requests");
        }

        int workload = GetUpcomingWorkloadCount(DateTime.UtcNow);
        switch (workload)
        {
            case 0:
                reasons.Add("No current workload");
                break;
            case <= 2:
                reasons.Add("Light current workload");
                break;
        }

        return reasons.Any() ? string.Join("; ", reasons) : "General maintenance capability";
    }

    /// <summary>
    /// ✅ STEP 2: Business logic moved from WorkerAssignmentPolicyService to Worker aggregate.
    /// Estimates how long this worker would need to complete a specific request.
    /// Encapsulates worker-specific time estimation logic within the aggregate.
    /// </summary>
    /// <param name="request">The tenant request to estimate time for</param>
    /// <returns>Estimated completion time</returns>
    public TimeSpan EstimateCompletionTime(TenantRequest request)
    {
        if (!IsActive)
        {
            return TimeSpan.Zero;
        }

        // Base time estimation
        var baseTime = TimeSpan.FromHours(2);

        // Emergency requests get priority timing
        if (request.IsEmergency)
        {
            return baseTime;
        }

        // Specialization affects efficiency
        string requiredSpecialization = DetermineRequiredSpecialization(request.Title, request.Description);
        if (!HasSpecializedSkills(requiredSpecialization))
        {
            return TimeSpan.FromHours(3);
        }

        // Exact match = most efficient
        if (Specialization?.Equals(requiredSpecialization, StringComparison.OrdinalIgnoreCase) == true)
        {
            return baseTime;
        }

        return TimeSpan.FromHours(2.5); // General maintenance takes slightly longer

        // No specialization match = longer time
    }

    /// <summary>
    /// Validates if this worker can be assigned to a request with detailed error information.
    /// Encapsulates worker-specific validation logic within the aggregate.
    /// </summary>
    /// <param name="request">The tenant request to validate assignment for</param>
    /// <param name="scheduledDate">The proposed scheduled date</param>
    /// <returns>Validation result with success/failure and error details</returns>
    public AssignmentValidationResult ValidateAssignmentToRequest(TenantRequest request, DateTime scheduledDate)
    {
        if (!IsActive)
        {
            return AssignmentValidationResult.Failure($"Worker '{ContactInfo.GetFullName()}' is not active");
        }

        if (!IsRequestAssignable(request))
        {
            return AssignmentValidationResult.Failure($"Request is not in assignable status (current: {request.Status})");
        }

        if (scheduledDate <= DateTime.UtcNow)
        {
            return AssignmentValidationResult.Failure("Scheduled date must be in the future");
        }

        if (!IsAvailableForWork(scheduledDate))
        {
            return AssignmentValidationResult.Failure($"Worker is not available on {scheduledDate:yyyy-MM-dd}");
        }

        string requiredSpecialization = DetermineRequiredSpecialization(request.Title, request.Description);
        if (!HasSpecializedSkills(requiredSpecialization))
        {
            return AssignmentValidationResult.Failure($"Worker does not have required specialization: {requiredSpecialization}");
        }

        if (IsOverloaded())
        {
            return AssignmentValidationResult.Failure("Worker is currently overloaded");
        }

        return AssignmentValidationResult.Success();
    }

    #region Private Helper Methods for Request Assignment

    /// <summary>
    /// Determines if a request is in a status that allows worker assignment.
    /// </summary>
    /// <param name="request">The request to check</param>
    /// <returns>True if the request can have workers assigned</returns>
    private static bool IsRequestAssignable(TenantRequest request)
    {
        return request.Status is TenantRequestStatus.Submitted or TenantRequestStatus.Failed;
    }

    /// <summary>
    /// Determines if this worker is currently overloaded with assignments.
    /// </summary>
    /// <returns>True if the worker has too many active assignments</returns>
    private bool IsOverloaded()
    {
        // Business rule: Worker is overloaded if they have more than 5 active assignments
        int activeAssignments = _assignments.Count(a => !a.IsCompleted);
        return activeAssignments > 5;
    }

    #endregion

    #endregion
}
