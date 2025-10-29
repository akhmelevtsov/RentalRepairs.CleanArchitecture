using RentalRepairs.Application.Services.TenantRequestDetailsService;
using RentalRepairs.WebUI.Models.Actions;

namespace RentalRepairs.WebUI.Services;

/// <summary>
/// Presentation layer service for converting business actions to UI actions.
/// This service belongs to the WebUI layer and handles all UI concerns.
/// </summary>
public interface ITenantRequestPresentationService
{
    /// <summary>
    /// Gets UI actions for display in the presentation layer.
    /// Converts clean business actions to UI actions with styling, icons, and URLs.
    /// </summary>
    Task<List<TenantRequestUIAction>> GetUIActionsAsync(Guid requestId, string? currentUserRole = null);

    /// <summary>
    /// Gets UI actions grouped by presentation style for different UI sections.
    /// </summary>
    Task<Dictionary<UIActionStyle, List<TenantRequestUIAction>>> GetGroupedUIActionsAsync(Guid requestId, string? currentUserRole = null);

    /// <summary>
    /// Gets primary UI actions for main action buttons.
    /// </summary>
    Task<List<TenantRequestUIAction>> GetPrimaryUIActionsAsync(Guid requestId, string? currentUserRole = null);

    /// <summary>
    /// Gets secondary UI actions for dropdown menus.
    /// </summary>
    Task<List<TenantRequestUIAction>> GetSecondaryUIActionsAsync(Guid requestId, string? currentUserRole = null);
}

/// <summary>
/// Implementation of tenant request presentation service.
/// Handles conversion from clean business actions to UI-specific actions.
/// </summary>
public class TenantRequestPresentationService : ITenantRequestPresentationService
{
    private readonly ITenantRequestDetailsService _detailsService;

    public TenantRequestPresentationService(ITenantRequestDetailsService detailsService)
    {
        _detailsService = detailsService;
    }

    public async Task<List<TenantRequestUIAction>> GetUIActionsAsync(Guid requestId, string? currentUserRole = null)
    {
        // Get clean business actions from application layer
        var businessActions = await _detailsService.GetAvailableBusinessActionsAsync(requestId, currentUserRole);
        
        // Convert to UI actions with all presentation concerns
        return TenantRequestUIActionMapper.MapToUIActions(businessActions, requestId);
    }

    public async Task<Dictionary<UIActionStyle, List<TenantRequestUIAction>>> GetGroupedUIActionsAsync(Guid requestId, string? currentUserRole = null)
    {
        var uiActions = await GetUIActionsAsync(requestId, currentUserRole);
        return TenantRequestUIActionMapper.GroupActionsByStyle(uiActions);
    }

    public async Task<List<TenantRequestUIAction>> GetPrimaryUIActionsAsync(Guid requestId, string? currentUserRole = null)
    {
        var uiActions = await GetUIActionsAsync(requestId, currentUserRole);
        return TenantRequestUIActionMapper.GetPrimaryActions(uiActions);
    }

    public async Task<List<TenantRequestUIAction>> GetSecondaryUIActionsAsync(Guid requestId, string? currentUserRole = null)
    {
        var uiActions = await GetUIActionsAsync(requestId, currentUserRole);
        return TenantRequestUIActionMapper.GetSecondaryActions(uiActions);
    }
}