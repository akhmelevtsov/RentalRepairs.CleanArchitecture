namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for simplified worker assignment rules
/// Implements clear and simple business logic for unit-level scheduling
/// </summary>
public class UnitSchedulingService
{
    /// <summary>
    /// Validates if a worker can be assigned to a unit on a specific date
    /// Implements simplified rules: specialization match, unit exclusivity, max 2 per unit
    /// </summary>
    public UnitSchedulingValidationResult ValidateWorkerAssignment(
        Guid requestId,
        string propertyCode,
        string unitNumber,
        DateTime scheduledDate,
        string workerEmail,
        string workerSpecialization,
        string requiredSpecialization,
        bool isEmergency,
        IEnumerable<ExistingAssignment> existingAssignments)
    {
        var result = new UnitSchedulingValidationResult();
        DateTime dateOnly = scheduledDate.Date;

        // Filter active assignments for the date
        var activeAssignments = existingAssignments
            .Where(a => a.ScheduledDate.Date == dateOnly &&
                        (a.Status == "Scheduled" || a.Status == "InProgress"))
            .ToList();

        // Rule 1: Check specialization match (already implemented elsewhere, but validate here too)
        if (!DoesSpecializationMatch(workerSpecialization, requiredSpecialization))
        {
            result.IsValid = false;
            result.ErrorMessage =
                $"Worker specialized in {workerSpecialization} cannot handle {requiredSpecialization} work";
            result.ConflictType = SchedulingConflictType.SpecializationMismatch;
            return result;
        }

        // Rule 3: Check if OTHER DIFFERENT workers are assigned to this unit on same date
        // Updated: Same worker can be assigned multiple times (Rule 4 handles the limit)
        var otherWorkersInUnit = activeAssignments
            .Where(a => a.PropertyCode == propertyCode &&
                        a.UnitNumber == unitNumber &&
                        a.WorkerEmail != workerEmail) // Only block DIFFERENT workers
            .ToList();

        if (otherWorkersInUnit.Any())
        {
            if (!isEmergency)
            {
                result.IsValid = false;
                result.ErrorMessage =
                    $"Unit {unitNumber} already has a different worker ({otherWorkersInUnit.First().WorkerEmail}) assigned on {dateOnly:yyyy-MM-dd}. Same worker can be assigned multiple times to the same unit.";
                result.ConflictType = SchedulingConflictType.UnitConflict;
                result.ConflictingAssignments = otherWorkersInUnit;
                return result;
            }
            else
            {
                // Emergency: Separate normal and emergency requests
                var normalRequestsToRevoke = otherWorkersInUnit.Where(a => !a.IsEmergency).ToList();
                var emergencyConflicts = otherWorkersInUnit.Where(a => a.IsEmergency).ToList();

                if (normalRequestsToRevoke.Any())
                {
                    result.AssignmentsToCancelForEmergency = normalRequestsToRevoke;
                }

                if (emergencyConflicts.Any())
                {
                    result.EmergencyConflicts = emergencyConflicts;
                    result.HasEmergencyConflicts = true;
                }
            }
        }

        // Rule 4: Check max 2 requests per worker per unit per day
        var sameWorkerSameUnit = activeAssignments
            .Where(a => a.PropertyCode == propertyCode &&
                        a.UnitNumber == unitNumber &&
                        a.WorkerEmail == workerEmail &&
                        a.TenantRequestId != requestId) // Don't count this request if it's a reassignment
            .ToList();

        if (sameWorkerSameUnit.Count >= 2)
        {
            if (!isEmergency)
            {
                result.IsValid = false;
                result.ErrorMessage =
                    $"Worker {workerEmail} already has maximum 2 assignments in Unit {unitNumber} on {dateOnly:yyyy-MM-dd}";
                result.ConflictType = SchedulingConflictType.WorkerUnitLimit;
                result.ConflictingAssignments = sameWorkerSameUnit;
                return result;
            }
            else
            {
                // Emergency: Revoke normal requests from same worker in same unit
                var normalToRevoke = sameWorkerSameUnit.Where(a => !a.IsEmergency).ToList();
                if (normalToRevoke.Any())
                {
                    result.AssignmentsToCancelForEmergency = result.AssignmentsToCancelForEmergency
                        .Concat(normalToRevoke)
                        .Distinct()
                        .ToList();
                }

                // If still over limit after revoking normal requests, check emergency conflicts
                var remainingEmergencies = sameWorkerSameUnit.Where(a => a.IsEmergency).ToList();
                if (remainingEmergencies.Count >= 2)
                {
                    result.EmergencyConflicts = result.EmergencyConflicts
                        .Concat(remainingEmergencies)
                        .Distinct()
                        .ToList();
                    result.HasEmergencyConflicts = true;
                }
            }
        }

        result.IsValid = true;
        return result;
    }

    /// <summary>
    /// Processes emergency override by cancelling conflicting assignments
    /// Returns list of tenant request IDs that were returned to "Submitted" status
    /// </summary>
    public EmergencyOverrideResult ProcessEmergencyOverride(
        IEnumerable<ExistingAssignment> assignmentsToCancel)
    {
        var result = new EmergencyOverrideResult();

        foreach (ExistingAssignment assignment in assignmentsToCancel)
        {
            result.CancelledRequestIds.Add(assignment.TenantRequestId);
            result.CancelledAssignments.Add(new CancelledAssignmentInfo
            {
                TenantRequestId = assignment.TenantRequestId,
                WorkerEmail = assignment.WorkerEmail,
                WorkOrderNumber = assignment.WorkOrderNumber,
                OriginalScheduledDate = assignment.ScheduledDate,
                CancellationReason = "Cancelled due to emergency request override"
            });
        }

        return result;
    }

    /// <summary>
    /// Check if worker specialization matches required work type
    /// Updated to use the same normalization logic as the Worker entity
    /// </summary>
    private bool DoesSpecializationMatch(string workerSpecialization, string requiredSpecialization)
    {
        if (string.IsNullOrWhiteSpace(requiredSpecialization))
        {
            return true; // No specific requirement
        }

        if (string.IsNullOrWhiteSpace(workerSpecialization))
        {
            return requiredSpecialization.Equals("General Maintenance", StringComparison.OrdinalIgnoreCase);
        }

        // Normalize both specializations using the same logic as Worker entity
        string normalizedWorkerSpec = NormalizeSpecialization(workerSpecialization);
        string normalizedRequiredSpec = NormalizeSpecialization(requiredSpecialization);

        // Exact match after normalization
        if (normalizedWorkerSpec.Equals(normalizedRequiredSpec, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // General Maintenance can handle anything
        if (normalizedWorkerSpec.Equals("General Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Normalizes specialization names to handle common variations.
    /// This matches the same logic used in the Worker entity.
    /// </summary>
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
}

/// <summary>
/// Result of unit scheduling validation
/// </summary>
public class UnitSchedulingValidationResult
{
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
    public SchedulingConflictType ConflictType { get; set; } = SchedulingConflictType.None;
    public List<ExistingAssignment> ConflictingAssignments { get; set; } = new();
    public List<ExistingAssignment> AssignmentsToCancelForEmergency { get; set; } = new();
    public List<ExistingAssignment> EmergencyConflicts { get; set; } = new();
    public bool HasEmergencyConflicts { get; set; } = false;
}

/// <summary>
/// Result of emergency override processing
/// </summary>
public class EmergencyOverrideResult
{
    public List<Guid> CancelledRequestIds { get; set; } = new();
    public List<CancelledAssignmentInfo> CancelledAssignments { get; set; } = new();
}

/// <summary>
/// Information about a cancelled assignment
/// </summary>
public class CancelledAssignmentInfo
{
    public Guid TenantRequestId { get; set; }
    public string WorkerEmail { get; set; } = string.Empty;
    public string WorkOrderNumber { get; set; } = string.Empty;
    public DateTime OriginalScheduledDate { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
}

/// <summary>
/// Represents an existing assignment for validation purposes
/// </summary>
public class ExistingAssignment
{
    public Guid TenantRequestId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string WorkerEmail { get; set; } = string.Empty;
    public string WorkerSpecialization { get; set; } = string.Empty;
    public string WorkOrderNumber { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsEmergency { get; set; } = false;
}

/// <summary>
/// Types of scheduling conflicts
/// </summary>
public enum SchedulingConflictType
{
    None,
    SpecializationMismatch,
    UnitConflict,
    WorkerUnitLimit
}
