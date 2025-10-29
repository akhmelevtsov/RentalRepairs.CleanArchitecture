using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Services;

/// <summary>
/// Comprehensive business context for a tenant request.
/// Contains all business-related information needed for request processing.
/// </summary>
public class TenantRequestBusinessContext
{
    public TenantRequest Request { get; set; } = null!;
    public Tenant? Tenant { get; set; }
    public WorkflowMetrics WorkflowMetrics { get; set; } = new();
    public WorkflowIntegrityResult WorkflowIntegrity { get; set; } = new();
    public EscalationRecommendation EscalationRecommendation { get; set; } = new();
    public List<RequestAction> AvailableActions { get; set; } = new();
    public List<WorkflowRecommendation> WorkflowRecommendations { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
}
