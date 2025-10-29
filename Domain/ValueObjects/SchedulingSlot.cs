using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Exceptions;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Value object representing a scheduling time slot for maintenance work
/// Encapsulates business rules for slot validation and availability
/// </summary>
public class SchedulingSlot : ValueObject
{
    public DateTime Date { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public SlotType Type { get; private set; }

    private SchedulingSlot() { } // EF Core

    public SchedulingSlot(DateTime date, TimeSpan startTime, TimeSpan endTime, SlotType type = SlotType.Standard)
    {
        ValidateSlot(date, startTime, endTime);
        
        Date = date.Date; // Ensure we only store the date part
        StartTime = startTime;
        EndTime = endTime;
        Type = type;
    }

    /// <summary>
    /// Creates a slot from tenant's preferred time preference
    /// </summary>
    public static SchedulingSlot FromTenantPreference(DateTime date, string? preferredContactTime)
    {
        if (string.IsNullOrWhiteSpace(preferredContactTime))
        {
            // Default to business hours if no preference
            return new SchedulingSlot(date, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), SlotType.TenantPreferred);
        }

        string preference = preferredContactTime.ToLowerInvariant().Trim();
        
        (TimeSpan startTime, TimeSpan endTime) = preference switch
        {
            // Handle exact matches from the UI
            "morning (8 am - 12 pm)" or "morning" => (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0)),
            "afternoon (12 pm - 5 pm)" or "afternoon" => (new TimeSpan(12, 0, 0), new TimeSpan(17, 0, 0)),
            "evening (5 pm - 8 pm)" or "evening" => (new TimeSpan(17, 0, 0), new TimeSpan(20, 0, 0)),
            
            // Handle variations from the UI
            "morning (8:00 am - 12:00 pm)" => (new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0)),
            "afternoon (12:00 pm - 5:00 pm)" => (new TimeSpan(12, 0, 0), new TimeSpan(17, 0, 0)),
            "evening (5:00 pm - 8:00 pm)" => (new TimeSpan(17, 0, 0), new TimeSpan(20, 0, 0)),
            
            // Handle other variations
            "anytime" or "any time" => (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)),
            
            // Default fallback
            _ => (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0))
        };

        return new SchedulingSlot(date, startTime, endTime, SlotType.TenantPreferred);
    }

    /// <summary>
    /// Creates standard business hour slots for a given date
    /// </summary>
    public static List<SchedulingSlot> CreateStandardSlots(DateTime date)
    {
        return new List<SchedulingSlot>
        {
            new(date, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), SlotType.Morning),
            new(date, new TimeSpan(12, 0, 0), new TimeSpan(17, 0, 0), SlotType.Afternoon),
            new(date, new TimeSpan(17, 0, 0), new TimeSpan(20, 0, 0), SlotType.Evening)
        };
    }

    /// <summary>
    /// Checks if this slot overlaps with another slot
    /// </summary>
    public bool OverlapsWith(SchedulingSlot other)
    {
        if (other == null || Date != other.Date)
        {
            return false;
        }

        return StartTime < other.EndTime && EndTime > other.StartTime;
    }

    /// <summary>
    /// Checks if this slot is within business hours
    /// </summary>
    public bool IsWithinBusinessHours()
    {
        var businessStart = new TimeSpan(7, 0, 0); // 7 AM
        var businessEnd = new TimeSpan(21, 0, 0);  // 9 PM

        return StartTime >= businessStart && EndTime <= businessEnd;
    }

    /// <summary>
    /// Gets the duration of this slot
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Checks if the slot is suitable for emergency work
    /// </summary>
    public bool IsSuitableForEmergency()
    {
        // Emergency slots can be scheduled outside normal hours if needed
        return Type == SlotType.Emergency || IsWithinBusinessHours();
    }

    /// <summary>
    /// Gets a user-friendly display name for the slot
    /// </summary>
    public string GetDisplayName()
    {
        return Type switch
        {
            SlotType.Morning => "Morning (8:00 AM - 12:00 PM)",
            SlotType.Afternoon => "Afternoon (12:00 PM - 5:00 PM)",
            SlotType.Evening => "Evening (5:00 PM - 8:00 PM)",
            SlotType.TenantPreferred => $"Tenant Preferred ({FormatTime(StartTime)} - {FormatTime(EndTime)})",
            SlotType.Emergency => $"Emergency Slot ({FormatTime(StartTime)} - {FormatTime(EndTime)})",
            _ => $"{FormatTime(StartTime)} - {FormatTime(EndTime)}"
        };
    }

    /// <summary>
    /// Formats a TimeSpan to a user-friendly time string with AM/PM
    /// </summary>
    private static string FormatTime(TimeSpan time)
    {
        DateTime dateTime = DateTime.Today.Add(time);
        return dateTime.ToString("h:mm tt");
    }

    /// <summary>
    /// Gets the actual scheduled DateTime for this slot (middle of the time range)
    /// </summary>
    public DateTime GetScheduledDateTime()
    {
        TimeSpan midpoint = StartTime.Add(TimeSpan.FromTicks((EndTime - StartTime).Ticks / 2));
        return Date.Add(midpoint);
    }

    public override string ToString()
    {
        return $"{Date:yyyy-MM-dd} {GetDisplayName()} ({Type})";
    }

    private static void ValidateSlot(DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        if (date.Date < DateTime.Today)
        {
            throw new TenantRequestDomainException("Cannot schedule slots in the past");
        }

        if (startTime >= endTime)
        {
            throw new TenantRequestDomainException("Start time must be before end time");
        }

        if (endTime - startTime < TimeSpan.FromMinutes(30))
        {
            throw new TenantRequestDomainException("Slot must be at least 30 minutes long");
        }

        if (endTime - startTime > TimeSpan.FromHours(8))
        {
            throw new TenantRequestDomainException("Slot cannot be longer than 8 hours");
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Date;
        yield return StartTime;
        yield return EndTime;
        yield return Type;
    }
}

public enum SlotType
{
    Standard,
    Morning,
    Afternoon, 
    Evening,
    TenantPreferred,
    Emergency,
    Flexible
}
