namespace RentalRepairs.Domain.Enums;

/// <summary>
/// Worker specialization types for maintenance work.
/// Defines the types of work a worker can perform.
/// </summary>
public enum WorkerSpecialization
{
    /// <summary>
    /// General maintenance - can handle any type of work
    /// </summary>
    GeneralMaintenance = 0,

    /// <summary>
    /// Plumbing work (leaks, pipes, drains, toilets)
    /// </summary>
    Plumbing = 1,

    /// <summary>
    /// Electrical work (outlets, wiring, lights, circuits)
    /// </summary>
    Electrical = 2,

    /// <summary>
    /// HVAC work (heating, cooling, ventilation)
    /// </summary>
    HVAC = 3,

    /// <summary>
    /// Carpentry work (wood, cabinets, doors, frames)
    /// </summary>
    Carpentry = 4,

    /// <summary>
    /// Painting work (walls, ceilings, trim)
    /// </summary>
    Painting = 5,

    /// <summary>
    /// Locksmith work (locks, keys, security)
    /// </summary>
    Locksmith = 6,

    /// <summary>
    /// Appliance repair (refrigerators, washers, dryers, ovens)
    /// </summary>
    ApplianceRepair = 7
}

/// <summary>
/// Extension methods for WorkerSpecialization enum.
/// Provides display names and business logic related to worker specializations.
/// </summary>
public static class WorkerSpecializationExtensions
{
    /// <summary>
    /// Gets the display name for the worker specialization.
    /// </summary>
    public static string GetDisplayName(this WorkerSpecialization specialization)
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
            _ => specialization.ToString()
        };
    }

    /// <summary>
    /// Gets a short description of the specialization.
    /// </summary>
    public static string GetDescription(this WorkerSpecialization specialization)
    {
        return specialization switch
        {
            WorkerSpecialization.GeneralMaintenance => "Can handle any type of maintenance work",
            WorkerSpecialization.Plumbing => "Leaks, pipes, drains, toilets",
            WorkerSpecialization.Electrical => "Outlets, wiring, lights, circuits",
            WorkerSpecialization.HVAC => "Heating, cooling, ventilation",
            WorkerSpecialization.Carpentry => "Wood, cabinets, doors, frames",
            WorkerSpecialization.Painting => "Walls, ceilings, trim",
            WorkerSpecialization.Locksmith => "Locks, keys, security",
            WorkerSpecialization.ApplianceRepair => "Refrigerators, washers, dryers, ovens",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets all available specializations as display names.
    /// </summary>
    public static IReadOnlyList<string> GetAllDisplayNames()
    {
        return Enum.GetValues<WorkerSpecialization>()
            .Select(s => s.GetDisplayName())
            .ToList()
            .AsReadOnly();
    }
}
