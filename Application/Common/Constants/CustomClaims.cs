namespace RentalRepairs.Application.Common.Constants;

/// <summary>
/// Custom claims constants for the rental repairs system
/// Application layer version to avoid WebUI dependency on Infrastructure
/// </summary>
public static class CustomClaims
{
    public const string PropertyId = "property_id";
    public const string PropertyCode = "property_code";
    public const string PropertyName = "property_name";
    public const string UnitNumber = "unit_number";
    public const string TenantId = "tenant_id";
    public const string WorkerSpecialization = "worker_specialization";
    public const string WorkerId = "worker_id";
    public const string IsActive = "is_active";
}