using System.Text.RegularExpressions;
using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Entities;

public class Property : BaseEntity, IAggregateRoot
{
    private readonly List<Tenant> _tenants = [];

    protected Property()
    {
        // For EF Core
        Name = string.Empty;
        Code = string.Empty;
        Address = null!;
        PhoneNumber = string.Empty;
        Superintendent = null!;
        Units = [];
        NoReplyEmailAddress = string.Empty;
    }

    public Property(
        string name,
        string code,
        PropertyAddress address,
        string phoneNumber,
        PersonContactInfo superintendent,
        List<string> units,
        string noReplyEmailAddress)
    {
        // Move validation logic from domain service to entity
        ValidatePropertyCreation(name, code, address, phoneNumber, superintendent, units, noReplyEmailAddress);

        Name = name;
        Code = code;
        Address = address;
        PhoneNumber = phoneNumber;
        Superintendent = superintendent;
        Units = units ?? [];
        NoReplyEmailAddress = noReplyEmailAddress;

        AddDomainEvent(new PropertyRegisteredEvent(this));
    }

    public string Name { get; }
    public string Code { get; }
    public PropertyAddress Address { get; private set; }
    public string PhoneNumber { get; private set; }
    public PersonContactInfo Superintendent { get; private set; }
    public List<string> Units { get; }
    public string NoReplyEmailAddress { get; private set; }

    public IReadOnlyCollection<Tenant> Tenants => _tenants.AsReadOnly();

    public Tenant RegisterTenant(PersonContactInfo contactInfo, string unitNumber)
    {
        if (contactInfo == null)
        {
            throw new ArgumentNullException(nameof(contactInfo));
        }

        if (string.IsNullOrWhiteSpace(unitNumber))
        {
            throw new ArgumentException("Unit number cannot be empty", nameof(unitNumber));
        }

        // Rich business logic in the aggregate
        ValidateUnitForTenantRegistration(unitNumber);

        var tenant = new Tenant(Id, Code, contactInfo, unitNumber);
        _tenants.Add(tenant);

        // Create the event
        var tenantRegisteredEvent = new TenantRegisteredEvent(tenant, this);

        // Add event to both property and tenant
        AddDomainEvent(tenantRegisteredEvent);
        tenant.AddDomainEvent(tenantRegisteredEvent);

        return tenant;
    }

    public void UpdateSuperintendent(PersonContactInfo superintendent)
    {
        if (superintendent == null)
        {
            throw new ArgumentNullException(nameof(superintendent));
        }

        PersonContactInfo oldSuperintendent = Superintendent;
        Superintendent = superintendent;

        AddDomainEvent(new SuperintendentChangedEvent(this, oldSuperintendent, superintendent));
    }

    public void AddUnit(string unitNumber)
    {
        if (string.IsNullOrWhiteSpace(unitNumber))
        {
            throw new ArgumentException("Unit number cannot be empty", nameof(unitNumber));
        }

        if (!IsValidUnitNumber(unitNumber))
        {
            throw new PropertyDomainException($"Unit number '{unitNumber}' has invalid format");
        }

        if (Units.Contains(unitNumber))
        {
            throw new PropertyDomainException($"Unit {unitNumber} already exists in property {Code}");
        }

        Units.Add(unitNumber);
        AddDomainEvent(new UnitAddedEvent(this, unitNumber));
    }

    public void RemoveUnit(string unitNumber)
    {
        if (string.IsNullOrWhiteSpace(unitNumber))
        {
            throw new ArgumentException("Unit number cannot be empty", nameof(unitNumber));
        }

        if (!Units.Contains(unitNumber))
        {
            throw new PropertyDomainException($"Unit {unitNumber} does not exist in property {Code}");
        }

        if (_tenants.Any(t => t.UnitNumber == unitNumber))
        {
            throw new PropertyDomainException($"Cannot remove unit {unitNumber} as it is currently occupied");
        }

        Units.Remove(unitNumber);
        AddDomainEvent(new UnitRemovedEvent(this, unitNumber));
    }

    public bool IsUnitAvailable(string unitNumber)
    {
        return Units.Contains(unitNumber) && _tenants.All(t => t.UnitNumber != unitNumber);
    }

    public IEnumerable<string> GetAvailableUnits()
    {
        var occupiedUnits = _tenants.Select(t => t.UnitNumber).ToHashSet();
        return Units.Where(unit => !occupiedUnits.Contains(unit));
    }

    public int GetOccupiedUnitsCount()
    {
        return _tenants.Count;
    }

    public double GetOccupancyRate()
    {
        return Units.Count > 0 ? (double)_tenants.Count / Units.Count : 0;
    }

    /// <summary>
    ///     Business rule: Determines if property requires attention.
    ///     Encapsulates the business logic for attention threshold.
    /// </summary>
    public bool RequiresAttention()
    {
        const double attentionThreshold = 0.8; // Business rule constant
        return GetOccupancyRate() < attentionThreshold;
    }

    /// <summary>
    ///     Business logic: Calculates comprehensive property metrics.
    ///     Encapsulates all property-related calculations in the aggregate.
    /// </summary>
    public PropertyMetrics CalculateMetrics()
    {
        return new PropertyMetrics
        {
            TotalUnits = Units.Count,
            OccupiedUnits = GetOccupiedUnitsCount(),
            VacantUnits = GetAvailableUnits().Count(),
            OccupancyRate = GetOccupancyRate(),
            RequiresAttention = RequiresAttention()
        };
    }

    /// <summary>
    ///     Business logic: Calculates property performance score based on multiple factors.
    ///     Encapsulates complex business rules for property evaluation.
    /// </summary>
    public double CalculatePerformanceScore()
    {
        double occupancyScore = GetOccupancyRate() * 100;
        double maintenanceScore = CalculateMaintenanceScore();
        double tenantSatisfactionScore = CalculateTenantSatisfactionScore();

        // Weighted average: 50% occupancy, 30% maintenance, 20% satisfaction
        return occupancyScore * 0.5 + maintenanceScore * 0.3 + tenantSatisfactionScore * 0.2;
    }

    /// <summary>
    ///     Business logic: Evaluates if property can accommodate additional tenants.
    /// </summary>
    public bool CanAccommodateAdditionalTenants()
    {
        return GetAvailableUnits().Any();
    }

    /// <summary>
    ///     Business logic: Gets revenue potential based on occupancy.
    ///     Simplified calculation for demonstration.
    /// </summary>
    public double CalculateRevenuePotential(double averageRentPerUnit = 1000)
    {
        return Units.Count * averageRentPerUnit * GetOccupancyRate();
    }

    public Dictionary<string, object> GetStatistics()
    {
        IEnumerable<string> availableUnits = GetAvailableUnits();

        return new Dictionary<string, object>
        {
            ["PropertyName"] = Name,
            ["PropertyCode"] = Code,
            ["TotalUnits"] = Units.Count,
            ["OccupiedUnits"] = _tenants.Count,
            ["AvailableUnits"] = availableUnits.Count(),
            ["OccupancyRate"] = GetOccupancyRate(),
            ["SuperintendentName"] = Superintendent.GetFullName(),
            ["SuperintendentEmail"] = Superintendent.EmailAddress,
            ["RequiresAttention"] = RequiresAttention(),
            ["PerformanceScore"] = CalculatePerformanceScore()
        };
    }

    private static void ValidatePropertyCreation(
        string name,
        string code,
        PropertyAddress address,
        string phoneNumber,
        PersonContactInfo superintendent,
        List<string> units,
        string noReplyEmailAddress)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Property name cannot be empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Property code cannot be empty", nameof(code));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (superintendent == null)
        {
            throw new ArgumentNullException(nameof(superintendent));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentNullException(nameof(phoneNumber));
        }

        if (string.IsNullOrWhiteSpace(noReplyEmailAddress))
        {
            throw new ArgumentNullException(nameof(noReplyEmailAddress));
        }

        ValidateUnits(units);
    }

    private static void ValidateUnits(List<string> units)
    {
        if (units == null || !units.Any())
        {
            throw new PropertyDomainException("Property must have at least one unit");
        }

        if (units.Distinct().Count() != units.Count)
        {
            throw new PropertyDomainException("Property cannot have duplicate unit numbers");
        }

        // Validate unit number format
        foreach (string unit in units)
        {
            if (!IsValidUnitNumber(unit))
            {
                throw new PropertyDomainException($"Unit number '{unit}' has invalid format");
            }
        }
    }

    private void ValidateUnitForTenantRegistration(string unitNumber)
    {
        if (!Units.Contains(unitNumber))
        {
            throw new PropertyDomainException($"Unit {unitNumber} does not exist in property {Code}");
        }

        if (_tenants.Any(t => t.UnitNumber == unitNumber))
        {
            throw new PropertyDomainException($"Unit {unitNumber} is already occupied in property {Code}");
        }
    }

    private static bool IsValidUnitNumber(string unitNumber)
    {
        // Unit number should be alphanumeric, possibly with dash or space
        return !string.IsNullOrWhiteSpace(unitNumber) &&
               unitNumber.Length <= 10 &&
               Regex.IsMatch(unitNumber, @"^[A-Za-z0-9\-\s]+$");
    }

    #region Private Business Logic Methods

    /// <summary>
    ///     Business logic: Calculates maintenance score based on property characteristics.
    ///     Simplified calculation - in production would consider request history, age, etc.
    /// </summary>
    private double CalculateMaintenanceScore()
    {
        // Simplified scoring based on unit count (larger properties may have more maintenance challenges)
        double baseScore = 85.0;

        switch (Units.Count)
        {
            case > 50:
                baseScore -= 10; // Large properties have more maintenance complexity
                break;
            case < 10:
                baseScore += 5; // Small properties easier to maintain
                break;
        }

        return Math.Max(0, Math.Min(100, baseScore));
    }

    /// <summary>
    ///     Business logic: Estimates tenant satisfaction score based on property metrics.
    ///     Simplified calculation - in production would use actual tenant feedback.
    /// </summary>
    private double CalculateTenantSatisfactionScore()
    {
        const double baseScore = 80.0;

        // Higher occupancy often indicates tenant satisfaction
        double occupancyBonus = GetOccupancyRate() * 20; // Up to 20 point bonus

        return Math.Max(0, Math.Min(100, baseScore + occupancyBonus));
    }

 

    #endregion
}
