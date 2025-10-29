using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Common.Constants;

namespace RentalRepairs.WebUI.Pages.Worker;

/// <summary>
/// Worker Dashboard - Main landing page for maintenance workers
/// </summary>
[Authorize(Roles = UserRoles.Worker)]
public class DashboardModel : PageModel
{
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(ILogger<DashboardModel> logger)
    {
        _logger = logger;
    }

    public string WorkerName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    public void OnGet()
    {
        WorkerName = User.Identity?.Name ?? "Worker";
        Specialization = User.FindFirst("worker_specialization")?.Value ?? "General";

        _logger.LogInformation("Worker dashboard accessed by {WorkerName} with specialization {Specialization}", 
            WorkerName, Specialization);
    }
}