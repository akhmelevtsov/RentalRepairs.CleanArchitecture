using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Common.Constants;
using RentalRepairs.Application.Queries.Workers.GetWorkerRequests;
using RentalRepairs.WebUI.Models;
using System.Security.Claims;
using Mapster;

namespace RentalRepairs.WebUI.Pages.Worker;

/// <summary>
/// Worker Assignments Page - Shows worker's assigned work orders
/// </summary>
[Authorize(Roles = UserRoles.Worker)]
public class AssignmentsModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<AssignmentsModel> _logger;

    public AssignmentsModel(IMediator mediator, ILogger<AssignmentsModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public List<TenantRequestSummaryViewModel> AssignedRequests { get; set; } = new();
    public List<TenantRequestSummaryViewModel> UpcomingWork { get; set; } = new();
    public List<TenantRequestSummaryViewModel> OverdueWork { get; set; } = new();
    public List<TenantRequestSummaryViewModel> CompletedWork { get; set; } = new();

    public string WorkerName { get; set; } = string.Empty;
    public string WorkerEmail { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    public int TotalAssigned { get; set; }
    public int TotalUpcoming { get; set; }
    public int TotalOverdue { get; set; }
    public int TotalCompleted { get; set; }

    public string? StatusFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;

    // Status filter options for workers
    public readonly List<string> StatusOptions = new()
    {
        "All", "Scheduled", "InProgress", "Completed", "Overdue"
    };

    public async Task OnGetAsync(string? status = null, int pageNumber = 1)
    {
        try
        {
            CurrentPage = Math.Max(1, pageNumber);
            StatusFilter = status ?? "All";

            // Extract worker information from claims
            WorkerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "";
            WorkerName = User.Identity?.Name ?? "Worker";
            Specialization = User.FindFirst("worker_specialization")?.Value ?? "General";

            if (string.IsNullOrEmpty(WorkerEmail))
            {
                _logger.LogWarning("No worker email found in claims for user {UserId}",
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                TempData["Error"] = "Unable to identify worker. Please log in again.";
                return;
            }

            await LoadWorkerAssignmentsAsync();

            _logger.LogInformation(
                "Loaded assignments for worker {WorkerEmail}: {Assigned} assigned, {Upcoming} upcoming, {Overdue} overdue",
                WorkerEmail, TotalAssigned, TotalUpcoming, TotalOverdue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading worker assignments for {WorkerEmail}", WorkerEmail);
            TempData["Error"] = "Unable to load your assignments. Please try again.";
        }
    }

    private async Task LoadWorkerAssignmentsAsync()
    {
        var query = new GetWorkerRequestsQuery(WorkerEmail)
        {
            PageSize = 50, // Load more for categorization
            PageNumber = 1
        };

        var requestsResult = await _mediator.Send(query);
        var allRequests = requestsResult.Items.Adapt<List<TenantRequestSummaryViewModel>>();

        // Categorize requests
        var now = DateTime.Now;

        AssignedRequests = allRequests.Where(r =>
            r.Status == "Scheduled" || r.Status == "InProgress").ToList();

        UpcomingWork = allRequests.Where(r =>
            r.ScheduledDate.HasValue &&
            r.ScheduledDate > now &&
            (r.Status == "Scheduled" || r.Status == "InProgress")).ToList();

        OverdueWork = allRequests.Where(r =>
            r.ScheduledDate.HasValue &&
            r.ScheduledDate < now &&
            r.Status != "Completed" &&
            r.Status != "Closed").ToList();

        CompletedWork = allRequests.Where(r =>
            r.Status == "Completed" &&
            r.CompletedDate.HasValue &&
            r.CompletedDate.Value.Month == now.Month &&
            r.CompletedDate.Value.Year == now.Year).ToList();

        // Set counts
        TotalAssigned = AssignedRequests.Count;
        TotalUpcoming = UpcomingWork.Count;
        TotalOverdue = OverdueWork.Count;
        TotalCompleted = CompletedWork.Count;

        // Apply status filter for display
        AssignedRequests = ApplyStatusFilter(AssignedRequests);
    }

    private List<TenantRequestSummaryViewModel> ApplyStatusFilter(List<TenantRequestSummaryViewModel> requests)
    {
        if (StatusFilter == "All") return requests;

        return StatusFilter switch
        {
            "Scheduled" => requests.Where(r => r.Status == "Scheduled").ToList(),
            "InProgress" => requests.Where(r => r.Status == "InProgress").ToList(),
            "Completed" => CompletedWork,
            "Overdue" => OverdueWork,
            _ => requests
        };
    }

    public string GetStatusBadgeClass(string status)
    {
        return status?.ToLower() switch
        {
            "scheduled" => "badge bg-info",
            "inprogress" => "badge bg-warning text-dark",
            "completed" => "badge bg-success",
            "overdue" => "badge bg-danger",
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
        if (request.ScheduledDate.HasValue && request.ScheduledDate < DateTime.Now &&
            request.Status != "Completed" && request.Status != "Closed")
            return "table-danger"; // Overdue

        if (request.IsEmergency || request.UrgencyLevel == "Critical") return "table-warning"; // Emergency

        return "";
    }

    public string GetFormattedDate(DateTime date)
    {
        return date.ToString("MMM dd, yyyy");
    }

    public string GetFormattedDateTime(DateTime date)
    {
        return date.ToString("MMM dd, yyyy h:mm tt");
    }

    public bool IsOverdue(TenantRequestSummaryViewModel request)
    {
        return request.ScheduledDate.HasValue &&
               request.ScheduledDate < DateTime.Now &&
               request.Status != "Completed" &&
               request.Status != "Closed";
    }
}