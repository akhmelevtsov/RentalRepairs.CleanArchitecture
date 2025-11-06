using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Queries.Properties.GetProperties;
using RentalRepairs.Application.Queries.Properties.GetPropertyByCode;
using RentalRepairs.Application.Queries.TenantRequests.GetTenantRequests;
using RentalRepairs.Application.Queries.Workers.GetWorkerRequests;
using RentalRepairs.WebUI.Models;
using RentalRepairs.Application.Common.Constants;
using System.Security.Claims;
using Mapster;

namespace RentalRepairs.WebUI.Pages;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMediator mediator, ILogger<IndexModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public DashboardViewModel Dashboard { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    [TempData] public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            await LoadDashboardDataAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            TempData["Error"] = "Unable to load dashboard data. Please try again.";
            return Page();
        }
    }

    private async Task LoadDashboardDataAsync()
    {
        // Determine user role from claims (simplified for demo)
        var userRole = GetUserRoleFromClaims();
        Dashboard.UserRole = userRole;
        Dashboard.UserName = User.Identity?.Name ?? "Guest";
        Dashboard.UserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "";

        switch (userRole)
        {
            case UserRole.SystemAdmin:
                await LoadSystemAdminDashboardAsync();
                break;
            case UserRole.PropertySuperintendent:
                await LoadPropertySuperintendentDashboardAsync();
                break;
            case UserRole.Tenant:
                await LoadTenantDashboardAsync();
                break;
            case UserRole.Worker:
                await LoadWorkerDashboardAsync();
                break;
        }
    }

    private async Task LoadSystemAdminDashboardAsync()
    {
        try
        {
            // Get ALL properties to calculate comprehensive statistics
            // Use a large page size to get all properties at once for admin dashboard
            var allPropertiesQuery = new GetPropertiesQuery { PageSize = 1000, PageNumber = 1 };
            var allPropertiesResult = await _mediator.Send(allPropertiesQuery);

            var requestsQuery = new GetTenantRequestsQuery { PageSize = 10, PageNumber = 1 };
            var requestsResult = await _mediator.Send(requestsQuery);

            // Calculate comprehensive system statistics from all properties
            var allProperties = allPropertiesResult.Items.ToList();

            Dashboard.TotalSystemProperties = allPropertiesResult.TotalCount;
            Dashboard.TotalSystemRequests = requestsResult.Count;

            // Calculate system-wide unit and occupancy statistics
            Dashboard.TotalSystemUnits = allProperties.Sum(p => p.Units?.Count ?? 0);
            Dashboard.TotalOccupiedUnits = allProperties.Sum(p => p.Tenants?.Count ?? 0);
            Dashboard.TotalVacantUnits = Dashboard.TotalSystemUnits - Dashboard.TotalOccupiedUnits;
            Dashboard.SystemOccupancyRate = Dashboard.TotalSystemUnits > 0
                ? (double)Dashboard.TotalOccupiedUnits / Dashboard.TotalSystemUnits * 100
                : 0;

            // Calculate request statistics - this is a simple count for now
            // In a real system, you might want to get more detailed request data
            Dashboard.ActiveSystemRequests = requestsResult.Count(r =>
                r.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                r.Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                r.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase));
            Dashboard.TotalSystemRequestsAllTime = requestsResult.Count;

            // Map recent properties with their statistics for display
            Dashboard.RecentProperties = allProperties
                .OrderByDescending(p => p.CreatedDate)
                .Take(5)
                .Select(p => new PropertySummaryViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    City = p.Address?.City ?? "Unknown",
                    State = "N/A", // Address doesn't have state field in current model
                    TotalUnits = p.Units?.Count ?? 0,
                    OccupiedUnits = p.Tenants?.Count ?? 0,
                    VacantUnits = (p.Units?.Count ?? 0) - (p.Tenants?.Count ?? 0),
                    OccupancyRate = (p.Units?.Count ?? 0) > 0
                        ? (double)(p.Tenants?.Count ?? 0) / (p.Units?.Count ?? 0) * 100
                        : 0,
                    TenantCount = p.Tenants?.Count ?? 0,
                    ActiveRequestsCount = 0, // TODO: Calculate from requests when available
                    SuperintendentName = $"{p.Superintendent?.FirstName} {p.Superintendent?.LastName}".Trim()
                })
                .ToList();

            // Recent requests for admin overview
            Dashboard.RecentRequests = requestsResult.Adapt<List<TenantRequestSummaryViewModel>>();

            _logger.LogInformation(
                "System Admin dashboard loaded - Properties: {Properties}, Units: {Units}, Occupancy: {Occupancy:F1}%, Active Requests: {ActiveRequests}",
                Dashboard.TotalSystemProperties, Dashboard.TotalSystemUnits, Dashboard.SystemOccupancyRate,
                Dashboard.ActiveSystemRequests);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load system admin dashboard data");
            // Continue with default values
        }
    }

    private async Task LoadPropertySuperintendentDashboardAsync()
    {
        try
        {
            var userEmail = Dashboard.UserEmail;

            // SECURITY FIX: Get superintendent's property from claims
            var propertyCode = User.FindFirst("property_code")?.Value;
            var propertyName = User.FindFirst("property_name")?.Value;

            _logger.LogInformation(
                "Loading superintendent dashboard for email: {Email}, Property: {PropertyCode} ({PropertyName})",
                userEmail, propertyCode, propertyName);

            if (string.IsNullOrEmpty(propertyCode))
            {
                _logger.LogWarning("Property code not found in claims for superintendent {Email}", userEmail);
                Dashboard.PendingRequests = 0;
                Dashboard.EmergencyRequests = 0;
                Dashboard.RecentRequests = new List<TenantRequestSummaryViewModel>();
                return;
            }

            // SECURITY FIX: Get property ID for filtering
            Guid? propertyId = null;
            try
            {
                var propertyQuery = new GetPropertyByCodeQuery(propertyCode);
                var property = await _mediator.Send(propertyQuery);
                if (property != null)
                {
                    propertyId = property.Id;
                    _logger.LogInformation("Found property ID {PropertyId} for code {PropertyCode}", propertyId,
                        propertyCode);
                }
                else
                {
                    _logger.LogWarning("Property not found for code {PropertyCode}", propertyCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property for code {PropertyCode}", propertyCode);
            }

            // SECURITY FIX: Filter requests by superintendent's property only
            var requestsQuery = new GetTenantRequestsQuery
            {
                PropertyId = propertyId, // CRITICAL: Only show requests for superintendent's property
                PageSize = 100,
                PageNumber = 1
            };
            var requestsResult = await _mediator.Send(requestsQuery);

            _logger.LogInformation(
                "Retrieved {TotalCount} requests for property {PropertyCode} (ID: {PropertyId}) for superintendent analysis",
                requestsResult.Count, propertyCode, propertyId);

            var allRequests = requestsResult.ToList();

            // Count requests using clean string comparisons
            Dashboard.PendingRequests = allRequests.Count(r =>
                r.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase) ||
                r.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase));

            // Emergency requests should exclude completed/closed/done requests
            Dashboard.EmergencyRequests = allRequests.Count(r =>
                (r.UrgencyLevel.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                 r.UrgencyLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
                 r.UrgencyLevel.Equals("Emergency", StringComparison.OrdinalIgnoreCase) ||
                 r.IsEmergency) &&
                !r.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) &&
                !r.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) &&
                !r.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));

            Dashboard.RecentRequests = allRequests
                .OrderByDescending(r => r.CreatedDate)
                .Take(10)
                .Adapt<List<TenantRequestSummaryViewModel>>();

            _logger.LogInformation(
                "Superintendent dashboard loaded for {PropertyCode} - Pending: {Pending}, Emergency: {Emergency}, Recent: {Recent}",
                propertyCode, Dashboard.PendingRequests, Dashboard.EmergencyRequests, Dashboard.RecentRequests.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load superintendent dashboard data");
            Dashboard.PendingRequests = 0;
            Dashboard.EmergencyRequests = 0;
            Dashboard.RecentRequests = new List<TenantRequestSummaryViewModel>();
        }
    }

    private async Task LoadTenantDashboardAsync()
    {
        _logger.LogInformation("Loading tenant dashboard");

        var tenantIdClaim = User.FindFirst("tenant_id")?.Value ?? User.FindFirst(CustomClaims.TenantId)?.Value;
        var propertyName = User.FindFirst("property_name")?.Value;
        var unitNumber = User.FindFirst(CustomClaims.UnitNumber)?.Value ?? User.FindFirst("unit_number")?.Value;
        var propertyCode = User.FindFirst("property_code")?.Value ?? User.FindFirst(CustomClaims.PropertyId)?.Value;

        if (string.IsNullOrEmpty(propertyName) && !string.IsNullOrEmpty(propertyCode))
            propertyName = GetPropertyNameFromCode(propertyCode);

        Dashboard.PropertyName = propertyName ?? "Unknown Property";
        Dashboard.UnitNumber = unitNumber ?? "Unknown Unit";
        Dashboard.MyRequests = new List<TenantRequestSummaryViewModel>();

        try
        {
            var tenantEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;

            if (!string.IsNullOrEmpty(tenantEmail))
            {
                _logger.LogInformation("Loading requests for tenant email: {TenantEmail}", tenantEmail);

                // Use a fresh query to get the latest data (no caching)
                var allRequestsQuery = new GetTenantRequestsQuery
                {
                    PageSize = 200, // Increased to ensure we get all tenant requests
                    PageNumber = 1,
                    SortBy = "CreatedDate",
                    SortDescending = true // Get most recent first
                };
                var allRequestsResult = await _mediator.Send(allRequestsQuery);

                _logger.LogInformation("Retrieved {TotalRequests} total requests, filtering for tenant {TenantEmail}",
                    allRequestsResult.Count, tenantEmail);

                var tenantRequests = allRequestsResult.Where(r => // ? allRequestsResult is already a List<T>
                        !string.IsNullOrEmpty(r.TenantEmail) &&
                        r.TenantEmail.Equals(tenantEmail, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(10)
                    .ToList();

                _logger.LogInformation("Found {TenantRequestCount} requests for tenant {TenantEmail}",
                    tenantRequests.Count, tenantEmail);

                // Debug log the request details
                foreach (var request in tenantRequests.Take(3))
                {
                    var descriptionPreview = request.Description?.Length > 50
                        ? request.Description.Substring(0, 50)
                        : request.Description ?? "No description";

                    _logger.LogInformation(
                        "Request: ID={RequestId}, CreatedDate={CreatedDate}, Status={Status}, Description={Description}",
                        request.Id, request.CreatedDate, request.Status, descriptionPreview);
                }

                // ? Use global Mapster configuration
                Dashboard.MyRequests = tenantRequests.Adapt<List<TenantRequestSummaryViewModel>>();
            }
            else
            {
                _logger.LogWarning("No tenant email found in claims for dashboard loading");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load tenant requests for dashboard");
        }
    }

    private async Task LoadWorkerDashboardAsync()
    {
        try
        {
            var workerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            _logger.LogInformation("Loading worker dashboard for email: {WorkerEmail}", workerEmail);

            if (!string.IsNullOrEmpty(workerEmail))
            {
                var requestsQuery = new GetWorkerRequestsQuery(workerEmail)
                {
                    PageSize = 50,
                    PageNumber = 1
                };
                var requestsResult = await _mediator.Send(requestsQuery);

                Dashboard.AssignedRequests = requestsResult.TotalCount;

                Dashboard.CompletedThisMonth = requestsResult.Items.Count(r =>
                    r.Status == "Done" &&
                    r.CompletedDate?.Month == DateTime.Now.Month &&
                    r.CompletedDate?.Year == DateTime.Now.Year);

                // ? Include ALL scheduled work (today, future, and overdue) in UpcomingWork
                Dashboard.UpcomingWork = requestsResult.Items
                    .Where(r => r.Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) &&
                                r.ScheduledDate.HasValue)
                    .OrderBy(r => r.ScheduledDate)
                    .Adapt<List<TenantRequestSummaryViewModel>>();

                _logger.LogInformation(
                    "Worker dashboard loaded - Total Assigned: {Assigned}, Active Scheduled: {ActiveScheduled}, Completed This Month: {Completed}, All Upcoming Work: {Upcoming}",
                    Dashboard.AssignedRequests, Dashboard.UpcomingWork.Count, Dashboard.CompletedThisMonth,
                    Dashboard.UpcomingWork.Count);
            }
            else
            {
                Dashboard.AssignedRequests = 0;
                Dashboard.CompletedThisMonth = 0;
                Dashboard.UpcomingWork = new List<TenantRequestSummaryViewModel>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load worker dashboard data");
            Dashboard.AssignedRequests = 0;
            Dashboard.CompletedThisMonth = 0;
            Dashboard.UpcomingWork = new List<TenantRequestSummaryViewModel>();
        }
    }

    private string? GetPropertyNameFromCode(string propertyCode)
    {
        return propertyCode?.ToUpperInvariant() switch
        {
            "SUN001" => "Sunset Apartments",
            "MAP001" => "Maple Grove Condos",
            "OAK001" => "Oak Hill Residences",
            "PIN001" => "Pine Valley Apartments",
            _ => null
        };
    }

    private UserRole GetUserRoleFromClaims()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim switch
        {
            "SystemAdmin" => UserRole.SystemAdmin,
            "PropertySuperintendent" => UserRole.PropertySuperintendent,
            "Tenant" => UserRole.Tenant,
            "Worker" => UserRole.Worker,
            _ => UserRole.Tenant
        };
    }
}