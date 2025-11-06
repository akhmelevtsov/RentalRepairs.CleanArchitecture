using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalRepairs.Application.Common.Interfaces;
using WebUIDemoUserInfo = RentalRepairs.WebUI.Models.DemoUserInfo;

namespace RentalRepairs.WebUI.Pages.Account;

/// <summary>
/// Demo status page showing concurrent login support
/// </summary>
public class DemoStatusModel : PageModel
{
    private readonly IDemoUserService _demoUserService;
    private readonly ILogger<DemoStatusModel> _logger;

    public DemoStatusModel(
        IDemoUserService demoUserService,
        ILogger<DemoStatusModel> logger)
    {
        _demoUserService = demoUserService;
        _logger = logger;
    }

    public List<WebUIDemoUserInfo> DemoAccounts { get; set; } = new();
    public bool IsDemoModeEnabled { get; set; }
    public string StatusMessage { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        try
        {
            IsDemoModeEnabled = _demoUserService.IsDemoModeEnabled();

            if (IsDemoModeEnabled)
            {
                var demoUsers = await _demoUserService.GetDemoUsersForDisplayAsync();

                DemoAccounts = demoUsers.Select(u => new WebUIDemoUserInfo
                {
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    Roles = u.Roles,
                    Description = u.Description ?? $"{string.Join(", ", u.Roles)} - Demo User",
                    Claims = u.Claims
                }).ToList();

                StatusMessage = $"Concurrent login support enabled - {DemoAccounts.Count} demo accounts available";
                _logger.LogInformation("Demo status page loaded - concurrent logins supported for {Count} accounts",
                    DemoAccounts.Count);
            }
            else
            {
                StatusMessage = "Demo mode is currently disabled";
                _logger.LogInformation("Demo status page loaded - demo mode disabled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading demo status page");
            StatusMessage = "Error loading demo account information";
        }
    }
}