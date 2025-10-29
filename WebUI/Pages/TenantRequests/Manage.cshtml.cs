using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Common.Constants;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequests;
using RentalRepairs.WebUI.Models;
using System.Security.Claims;
using Mapster;

namespace RentalRepairs.WebUI.Pages.TenantRequests;

/// <summary>
/// Superintendent Request Management Page - Manage requests for properties
/// </summary>
[Authorize(Roles = UserRoles.PropertySuperintendent)]
public class ManageModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<ManageModel> _logger;

    public ManageModel(IMediator mediator, ILogger<ManageModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public List<TenantRequestSummaryViewModel> Requests { get; set; } = new();
    public List<TenantRequestSummaryViewModel> PendingRequests { get; set; } = new();
    public List<TenantRequestSummaryViewModel> EmergencyRequests { get; set; } = new();
    public List<TenantRequestSummaryViewModel> OverdueRequests { get; set; } = new();
    
    public string SuperintendentName { get; set; } = string.Empty;
    public string SuperintendentEmail { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    
    public int TotalRequests { get; set; }
    public int TotalPending { get; set; }
    public int TotalEmergency { get; set; }
    public int TotalOverdue { get; set; }
    
    public string? StatusFilter { get; set; }
    public string? UrgencyFilter { get; set; }
    public string? PropertyFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;

    // Filter options
    public readonly List<string> StatusOptions = new() 
    { 
        "All", "Draft", "Submitted", "Scheduled", "InProgress", "Completed", "Closed" 
    };
    
    public readonly List<string> UrgencyOptions = new() 
    { 
        "All", "Low", "Normal", "High", "Critical" 
    };

    public async Task OnGetAsync(string? status = null, string? urgency = null, string? property = null, int pageNumber = 1)
    {
        try
        {
            CurrentPage = Math.Max(1, pageNumber);
            StatusFilter = status ?? "All";
            UrgencyFilter = urgency ?? "All";
            PropertyFilter = property ?? "All";

            // Extract superintendent information from claims
            SuperintendentEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "";
            SuperintendentName = User.Identity?.Name ?? "Superintendent";
            PropertyName = User.FindFirst("property_name")?.Value;

            if (string.IsNullOrEmpty(SuperintendentEmail))
            {
                _logger.LogWarning("No superintendent email found in claims for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                TempData["Error"] = "Unable to identify superintendent. Please log in again.";
                return;
            }

            await LoadRequestsForManagementAsync();
            
            _logger.LogInformation("Loaded {RequestCount} requests for superintendent {SuperintendentEmail} management", 
                Requests.Count, SuperintendentEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading requests for superintendent management");
            TempData["Error"] = "Unable to load requests for management. Please try again.";
        }
    }

    private async Task LoadRequestsForManagementAsync()
    {
        const int pageSize = 20;

        var query = new GetTenantRequestsQuery
        {
            PageNumber = 1,
            PageSize = 100 // Load more for superintendent management
        };

        var allRequests = await _mediator.Send(query);
        
        // Convert to view models first
        var allRequestViewModels = allRequests.Adapt<List<TenantRequestSummaryViewModel>>();
        
        // For now, show all requests - in a real implementation, filter by superintendent's properties
        var superintendentRequests = allRequestViewModels.ToList();

        // Apply filters
        if (StatusFilter != "All")
        {
            superintendentRequests = superintendentRequests.Where(r => 
                r.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (UrgencyFilter != "All")
        {
            superintendentRequests = superintendentRequests.Where(r => 
                r.UrgencyLevel.Equals(UrgencyFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Categorize requests
        var now = DateTime.Now;
        
        PendingRequests = superintendentRequests.Where(r => 
            r.Status == "Draft" || r.Status == "Submitted").ToList();
            
        // ? FIX: Emergency requests should exclude completed/closed requests
        EmergencyRequests = superintendentRequests.Where(r => 
            (r.UrgencyLevel == "High" || r.UrgencyLevel == "Critical" || r.IsEmergency) &&
            r.Status != "Completed" && 
            r.Status != "Closed" &&
            r.Status != "Done").ToList();
            
        // ? FIX: Also add "Done" status to overdue filter for consistency
        OverdueRequests = superintendentRequests.Where(r => 
            r.ScheduledDate.HasValue && 
            r.ScheduledDate < now &&
            r.Status != "Completed" && 
            r.Status != "Closed" &&
            r.Status != "Done").ToList();

        // Set counts
        TotalPending = PendingRequests.Count;
        TotalEmergency = EmergencyRequests.Count;
        TotalOverdue = OverdueRequests.Count;
        TotalRequests = superintendentRequests.Count;

        // Apply pagination
        TotalPages = (int)Math.Ceiling((double)TotalRequests / pageSize);
        
        var pagedRequests = superintendentRequests
            .OrderByDescending(r => r.CreatedDate)
            .Skip((CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Already view models
        Requests = pagedRequests;
    }

    public string GetStatusBadgeClass(string status)
    {
        return status?.ToLower() switch
        {
            "draft" => "badge bg-secondary",
            "submitted" => "badge bg-primary",
            "scheduled" => "badge bg-info",
            "inprogress" => "badge bg-warning text-dark",
            "completed" => "badge bg-success",
            "closed" => "badge bg-dark",
            _ => "badge bg-light text-dark"
        };
    }

    public string GetUrgencyBadgeClass(string urgency)
    {
        return urgency?.ToLower() switch
        {
            "low" => "badge bg-light text-dark",
            "normal" => "badge bg-primary",
            "high" => "badge bg-warning text-dark",
            "critical" => "badge bg-danger",
            _ => "badge bg-light text-dark"
        };
    }

    public string GetRowClass(TenantRequestSummaryViewModel request)
    {
        // Check if request is completed first
        if (request.Status == "Completed" || request.Status == "Closed" || request.Status == "Done")
        {
            return ""; // No special highlighting for completed requests
        }
        
        if (request.ScheduledDate.HasValue && request.ScheduledDate < DateTime.Now)
        {
            return "table-danger"; // Overdue
        }
        
        if (request.IsEmergency || request.UrgencyLevel == "Critical" || request.UrgencyLevel == "High")
        {
            return "table-warning"; // Emergency
        }
        
        if (request.Status == "Submitted")
        {
            return "table-info"; // Pending assignment
        }
        
        return "";
    }

    public string GetFormattedDate(DateTime date)
    {
        return date.ToString("MMM dd, yyyy");
    }

    public bool CanAssign(TenantRequestSummaryViewModel request)
    {
        return request.Status == "Draft" || request.Status == "Submitted";
    }

    public bool IsOverdue(TenantRequestSummaryViewModel request)
    {
        return request.ScheduledDate.HasValue && 
               request.ScheduledDate < DateTime.Now && 
               request.Status != "Completed" && 
               request.Status != "Closed" &&
               request.Status != "Done";
    }
}