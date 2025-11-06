using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for determining required worker specialization from work descriptions.
/// Encapsulates the business logic of mapping work descriptions to specializations.
/// </summary>
public class SpecializationDeterminationService
{
    // Keyword mappings for each specialization
    private static readonly Dictionary<WorkerSpecialization, string[]> _specializationKeywords = new()
    {
        [WorkerSpecialization.Plumbing] =
        [
            "plumb", "leak", "water", "drain", "pipe", "faucet", "toilet", "sink", "clog", "drip", "flush",
            "sewer"
        ],
        [WorkerSpecialization.Electrical] =
        [
            "electric", "power", "outlet", "wiring", "light", "switch", "breaker", "circuit", "lamp",
            "fixture", "voltage", "spark"
        ],
        [WorkerSpecialization.HVAC] =
        [
            "hvac", "furnace", "thermostat", "ventilation", "conditioner", "heating system",
            "cooling system", "heat pump", "air conditioning"
        ],
        [WorkerSpecialization.Locksmith] =
            ["lock", "key", "security", "deadbolt", "locked out", "lockout", "unlock", "rekey"],
        [WorkerSpecialization.Painting] = ["paint", "repaint", "brush", "roller", "color"],
        [WorkerSpecialization.Carpentry] = ["wood", "cabinet", "carpenter", "shelf", "wooden"],
        [WorkerSpecialization.ApplianceRepair] =
        [
            "appliance", "refrigerator", "washer", "dryer", "dishwasher", "oven", "stove", "microwave",
            "freezer"
        ]
    };

    /// <summary>
    /// Determines required specialization from work title and description.
    /// Uses keyword matching with priority ordering.
    /// </summary>
    /// <param name="title">Work request title</param>
    /// <param name="description">Work request description</param>
    /// <returns>Required worker specialization</returns>
    public WorkerSpecialization DetermineRequiredSpecialization(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            return WorkerSpecialization.GeneralMaintenance;
        }

        string text = $"{title} {description}".ToLowerInvariant();

        // Check specializations in priority order
        // More specific keywords checked first
        WorkerSpecialization[] priorityOrder =
        [
            WorkerSpecialization.ApplianceRepair, // Check first (appliances are specific)
            WorkerSpecialization.Locksmith, // Lock is more specific than door
            WorkerSpecialization.Plumbing, WorkerSpecialization.Electrical, WorkerSpecialization.HVAC,
            WorkerSpecialization.Painting, WorkerSpecialization.Carpentry
        ];

        foreach (WorkerSpecialization specialization in priorityOrder)
        {
            string[] keywords = _specializationKeywords[specialization];
            if (keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return specialization;
            }
        }

        return WorkerSpecialization.GeneralMaintenance;
    }

    /// <summary>
    /// Checks if a worker's specialization can handle the required work.
    /// General Maintenance workers can handle any work type.
    /// </summary>
    public bool CanHandleWork(WorkerSpecialization workerSpecialization, WorkerSpecialization requiredSpecialization)
    {
        // Exact match
        if (workerSpecialization == requiredSpecialization)
        {
            return true;
        }

        // General Maintenance can handle anything
        if (workerSpecialization == WorkerSpecialization.GeneralMaintenance)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses specialization from string (for backward compatibility and UI).
    /// Handles common variations and normalizes to enum.
    /// </summary>
    public WorkerSpecialization ParseSpecialization(string specializationText)
    {
        if (string.IsNullOrWhiteSpace(specializationText))
        {
            return WorkerSpecialization.GeneralMaintenance;
        }

        string normalized = specializationText.Trim().ToLowerInvariant();

        return normalized switch
        {
            "plumbing" or "plumber" => WorkerSpecialization.Plumbing,
            "electrical" or "electrician" => WorkerSpecialization.Electrical,
            "hvac" or "hvac technician" or "heating" or "cooling" => WorkerSpecialization.HVAC,
            "carpentry" or "carpenter" => WorkerSpecialization.Carpentry,
            "painting" or "painter" => WorkerSpecialization.Painting,
            "locksmith" => WorkerSpecialization.Locksmith,
            "appliance repair" or "appliance technician" => WorkerSpecialization.ApplianceRepair,
            "general maintenance" or "maintenance" or "general" => WorkerSpecialization.GeneralMaintenance,
            _ => Enum.TryParse<WorkerSpecialization>(specializationText, true, out WorkerSpecialization result)
                ? result
                : WorkerSpecialization.GeneralMaintenance
        };
    }

    /// <summary>
    /// Gets display name for specialization.
    /// </summary>
    public string GetDisplayName(WorkerSpecialization specialization)
    {
        return specialization switch
        {
            WorkerSpecialization.GeneralMaintenance => "General Maintenance",
            WorkerSpecialization.Plumbing => "Plumbing",
            WorkerSpecialization.Electrical => "Electrical",
            WorkerSpecialization.HVAC => "HVAC",
            WorkerSpecialization.Carpentry => "Carpentry",
            WorkerSpecialization.Painting => "Painting",
            WorkerSpecialization.Locksmith => "Locksmith",
            WorkerSpecialization.ApplianceRepair => "Appliance Repair",
            _ => "General Maintenance"
        };
    }

    /// <summary>
    /// Gets all available specializations with their display names.
    /// Useful for UI dropdowns and selection lists.
    /// </summary>
    public Dictionary<WorkerSpecialization, string> GetAllSpecializations()
    {
        return Enum.GetValues<WorkerSpecialization>()
            .ToDictionary(s => s, s => GetDisplayName(s));
    }
}
