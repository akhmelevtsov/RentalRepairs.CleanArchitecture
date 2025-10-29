using System.ComponentModel.DataAnnotations;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.WebUI.Models;

/// <summary>
/// Enhanced presentation model for slot-based worker assignment
/// Contains only the data and validation rules needed for that specific use case.
/// </summary>
public class AssignWorkerPageViewModel
{
    [Required(ErrorMessage = "Please select a worker")]
    [Display(Name = "Worker")]
    public string WorkerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Scheduled date is required")]
    [Display(Name = "Scheduled Date")]
    public DateTime ScheduledDate { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Time Slot")]
    public string SelectedSlotKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Work order number is required")]
    [Display(Name = "Work Order Number")]
    [StringLength(50, ErrorMessage = "Work order number cannot exceed 50 characters")]
    public string WorkOrderNumber { get; set; } = string.Empty;

    [Display(Name = "Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    // Display-only properties populated from service
    public Guid RequestId { get; set; }
    public TenantRequestDisplayInfo Request { get; set; } = new();
    public List<WorkerSelectOption> AvailableWorkers { get; set; } = new();
    public List<TimeSlotOption> AvailableSlots { get; set; } = new();

    /// <summary>
    /// Updates available slots based on selected worker and date
    /// </summary>
    public void UpdateAvailableSlots(List<SchedulingSlot> slots, string? tenantPreferredTime = null)
    {
        AvailableSlots.Clear();
        
        foreach (var slot in slots)
        {
            var isPreferred = !string.IsNullOrEmpty(tenantPreferredTime) && 
                             slot.Type == SlotType.TenantPreferred;
            
            AvailableSlots.Add(new TimeSlotOption
            {
                Key = GenerateSlotKey(slot),
                Slot = slot,
                DisplayName = slot.GetDisplayName(),
                IsPreferred = isPreferred,
                IsAvailable = true
            });
        }

        // Sort slots: preferred first, then by time
        AvailableSlots = AvailableSlots
            .OrderByDescending(s => s.IsPreferred)
            .ThenBy(s => s.Slot.StartTime)
            .ToList();
    }

    private string GenerateWorkOrderNumber()
    {
        var today = DateTime.Today;
        var dateStr = today.ToString("yyyyMMdd");
        var randomNum = new Random().Next(100, 999);
        return $"WO-{dateStr}-{randomNum}";
    }

    private string GenerateSlotKey(SchedulingSlot slot)
    {
        return $"{slot.Date:yyyy-MM-dd}_{slot.StartTime:hhmm}_{slot.EndTime:hhmm}_{slot.Type}";
    }
}

/// <summary>
/// Enhanced display model for tenant request information with scheduling context
/// </summary>
public class TenantRequestDisplayInfo
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsEmergency { get; set; }
    public DateTime SubmittedDate { get; set; }
    public string? PreferredContactTime { get; set; }
    
    public string FormattedSubmittedDate => SubmittedDate.ToString("MMMM dd, yyyy 'at' h:mm tt");
    
    public string PreferredContactTimeDisplay => string.IsNullOrEmpty(PreferredContactTime) 
        ? "No preference specified" 
        : PreferredContactTime;
}

/// <summary>
/// Enhanced model for worker selection with availability context
/// </summary>
public class WorkerSelectOption
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int AvailableSlotCount { get; set; }
}

/// <summary>
/// Model for time slot selection options
/// </summary>
public class TimeSlotOption
{
    public string Key { get; set; } = string.Empty;
    public SchedulingSlot Slot { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public bool IsAvailable { get; set; } = true;
    
    public string CssClass => IsPreferred ? "slot-preferred" : IsAvailable ? "slot-available" : "slot-unavailable";
    public string Icon => IsPreferred ? "?" : IsAvailable ? "?" : "?";
}