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
/// Tenant Requests List Page - Shows tenant's request history
/// </summary>
[Authorize(Roles = UserRoles.Tenant)]
public class ListModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<ListModel> _logger;

    public ListModel(IMediator mediator, ILogger<ListModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public List<TenantRequestSummaryViewModel> Requests { get; set; } = new();
    public string? PropertyName { get; set; }
    public string? UnitNumber { get; set; }
    public string? TenantName { get; set; }
    public string? StatusFilter { get; set; }
    public string? UrgencyFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRequests { get; set; } = 0;
    public bool HasRequests => Requests.Any();

    // Status and urgency filter options
    public readonly List<string> StatusOptions = new() 
    { 
        "All", "Draft", "Submitted", "Scheduled", "InProgress", "Completed", "Closed" 
    };
    
    public readonly List<string> UrgencyOptions = new() 
    { 
        "All", "Low", "Normal", "High", "Critical" 
    };

    public async Task OnGetAsync(int pageNumber = 1, string? status = null, string? urgency = null)
    {
        try
        {
            CurrentPage = Math.Max(1, pageNumber);
            StatusFilter = status ?? "All";
            UrgencyFilter = urgency ?? "All";

            // Extract tenant information from claims
            var tenantEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            PropertyName = User.FindFirst("property_name")?.Value ?? "Unknown Property";
            UnitNumber = User.FindFirst("unit_number")?.Value ?? User.FindFirst(CustomClaims.UnitNumber)?.Value ?? "Unknown Unit";
            TenantName = User.Identity?.Name ?? "Tenant";

            if (string.IsNullOrEmpty(tenantEmail))
            {
                _logger.LogWarning("No tenant email found in claims for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                TempData["Error"] = "Unable to identify tenant. Please log in again.";
                return;
            }

            await LoadTenantRequestsAsync(tenantEmail);
            
            _logger.LogInformation("Loaded {RequestCount} requests for tenant {TenantEmail} (Page {Page})", 
                Requests.Count, tenantEmail, CurrentPage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenant requests for page {Page}", CurrentPage);
            TempData["Error"] = "Unable to load your requests. Please try again.";
        }
    }

    private async Task LoadTenantRequestsAsync(string tenantEmail)
    {
        const int pageSize = 10;

        var query = new GetTenantRequestsQuery
        {
            PageNumber = CurrentPage,
            PageSize = pageSize
        };

        var allRequests = await _mediator.Send(query);
        
        // Filter requests for this tenant
        var tenantRequests = allRequests.Where(r => 
            !string.IsNullOrEmpty(r.TenantEmail) &&
            r.TenantEmail.Equals(tenantEmail, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Apply filters
        if (StatusFilter != "All")
        {
            tenantRequests = tenantRequests.Where(r => 
                r.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (UrgencyFilter != "All")
        {
            tenantRequests = tenantRequests.Where(r => 
                r.UrgencyLevel.Equals(UrgencyFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Calculate pagination
        TotalRequests = tenantRequests.Count;
        TotalPages = (int)Math.Ceiling((double)TotalRequests / pageSize);
        
        // Apply pagination
        var pagedRequests = tenantRequests
            .OrderByDescending(r => r.CreatedDate)
            .Skip((CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Map to view models
        Requests = pagedRequests.Adapt<List<TenantRequestSummaryViewModel>>();
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

    public string GetFormattedDate(DateTime date)
    {
        return date.ToString("MMM dd, yyyy");
    }
}