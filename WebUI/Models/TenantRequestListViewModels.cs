using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.WebUI.Models;

/// <summary>
/// ? OPTIMIZED: List-specific ViewModel for TenantRequest listing pages.
/// BEFORE: Mixed list, details, and edit concerns in one model
/// AFTER: Specialized for listing with filtering and pagination
/// </summary>
public class TenantRequestListPageViewModel
{
    // Filtering options
    public TenantRequestFilterOptions Filters { get; set; } = new();
    
    // Results
    public List<TenantRequestListItemViewModel> Requests { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    
    // UI state
    public bool HasResults => Requests.Any();
    public string NoResultsMessage => GetNoResultsMessage();

    private string GetNoResultsMessage()
    {
        if (Filters.HasActiveFilters)
        {
            return "No requests found matching your filters. Try adjusting your search criteria.";
        }
        return "No maintenance requests found. Submit a new request to get started.";
    }
}

/// <summary>
/// ? OPTIMIZED: Individual list item model - minimal properties for list rendering
/// </summary>
public class TenantRequestListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    
    // UI-optimized properties
    public string StatusBadgeClass => GetStatusBadgeClass(Status);
    public string UrgencyBadgeClass => GetUrgencyBadgeClass(UrgencyLevel);
    public string FormattedCreatedDate => CreatedDate.ToString("MMM dd, yyyy");
    public string FormattedScheduledDate => ScheduledDate?.ToString("MMM dd, yyyy") ?? "Not scheduled";
    public bool IsEmergency => UrgencyLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
                              UrgencyLevel.Equals("Emergency", StringComparison.OrdinalIgnoreCase);
    public bool IsOverdue => ScheduledDate.HasValue && ScheduledDate < DateTime.UtcNow && Status == "Scheduled";
    
    private static string GetStatusBadgeClass(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "draft" => "badge bg-secondary",
            "submitted" => "badge bg-primary", 
            "scheduled" => "badge bg-info",
            "done" => "badge bg-success",
            "failed" => "badge bg-warning",
            "declined" => "badge bg-danger",
            "closed" => "badge bg-dark",
            _ => "badge bg-light text-dark"
        };
    }

    private static string GetUrgencyBadgeClass(string urgencyLevel)
    {
        return urgencyLevel.ToLowerInvariant() switch
        {
            "critical" or "emergency" => "badge bg-danger",
            "high" => "badge bg-warning",
            "normal" => "badge bg-info",
            "low" => "badge bg-secondary",
            _ => "badge bg-light text-dark"
        };
    }
}

/// <summary>
/// ? OPTIMIZED: Filter options model - specialized for search/filter UI
/// </summary>
public class TenantRequestFilterOptions
{
    [Display(Name = "Status")]
    public string? StatusFilter { get; set; }

    [Display(Name = "Property")]
    public string? PropertyFilter { get; set; }

    [Display(Name = "Emergency Only")]
    public bool EmergencyOnly { get; set; }

    [Display(Name = "Date From")]
    [DataType(DataType.Date)]
    public DateTime? DateFrom { get; set; }

    [Display(Name = "Date To")]
    [DataType(DataType.Date)]
    public DateTime? DateTo { get; set; }

    [Display(Name = "Search")]
    public string? SearchTerm { get; set; }

    // Available filter options
    public List<SelectOption> StatusOptions { get; set; } = new()
    {
        new("", "All Statuses"),
        new("Draft", "Draft"),
        new("Submitted", "Submitted"),
        new("Scheduled", "Scheduled"),
        new("Done", "Completed"),
        new("Closed", "Closed")
    };

    public List<SelectOption> PropertyOptions { get; set; } = new();

    // UI helpers
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(StatusFilter) ||
                                   !string.IsNullOrWhiteSpace(PropertyFilter) ||
                                   EmergencyOnly ||
                                   DateFrom.HasValue ||
                                   DateTo.HasValue ||
                                   !string.IsNullOrWhiteSpace(SearchTerm);

    public int ActiveFilterCount => 
        (string.IsNullOrWhiteSpace(StatusFilter) ? 0 : 1) +
        (string.IsNullOrWhiteSpace(PropertyFilter) ? 0 : 1) +
        (EmergencyOnly ? 1 : 0) +
        (DateFrom.HasValue ? 1 : 0) +
        (DateTo.HasValue ? 1 : 0) +
        (string.IsNullOrWhiteSpace(SearchTerm) ? 0 : 1);
}

/// <summary>
/// ? OPTIMIZED: Pagination model - reusable across list pages
/// </summary>
public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public string SortBy { get; set; } = "CreatedDate";
    public string SortDirection { get; set; } = "desc";

    // Calculated properties
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int StartItem => (CurrentPage - 1) * PageSize + 1;
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

    // Page number range for UI
    public List<int> PageNumbers
    {
        get
        {
            var pages = new List<int>();
            var start = Math.Max(1, CurrentPage - 2);
            var end = Math.Min(TotalPages, CurrentPage + 2);

            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }

            return pages;
        }
    }
}

/// <summary>
/// ? Reusable select option model
/// </summary>
public class SelectOption
{
    public string Value { get; set; }
    public string Text { get; set; }
    public bool Selected { get; set; }

    public SelectOption(string value, string text, bool selected = false)
    {
        Value = value;
        Text = text;
        Selected = selected;
    }
}