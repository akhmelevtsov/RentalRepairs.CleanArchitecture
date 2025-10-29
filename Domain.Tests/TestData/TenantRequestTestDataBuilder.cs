using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Tests.TestData;

/// <summary>
/// Test data builder for creating TenantRequest entities with fluent interface
/// </summary>
public class TenantRequestTestDataBuilder
{
    private string _code = "TR-001";
    private string _title = "Test Request";
    private string _description = "Test Description";
    private string _urgencyLevel = "Normal";
    private Guid _tenantId = Guid.NewGuid();
    private Guid _propertyId = Guid.NewGuid();
    private string _tenantFullName = "John Doe";
    private string _tenantEmail = "john@test.com";
    private string _tenantUnit = "101";
    private string _propertyName = "Test Property";
    private string _propertyPhone = "555-1234";
    private string _superintendentFullName = "Jane Super";
    private string _superintendentEmail = "jane@test.com";

    public TenantRequestTestDataBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public TenantRequestTestDataBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TenantRequestTestDataBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TenantRequestTestDataBuilder WithUrgencyLevel(string urgencyLevel)
    {
        _urgencyLevel = urgencyLevel;
        return this;
    }

    public TenantRequestTestDataBuilder WithTenant(Guid tenantId, string fullName, string email, string unit)
    {
        _tenantId = tenantId;
        _tenantFullName = fullName;
        _tenantEmail = email;
        _tenantUnit = unit;
        return this;
    }

    public TenantRequestTestDataBuilder WithProperty(Guid propertyId, string name, string phone)
    {
        _propertyId = propertyId;
        _propertyName = name;
        _propertyPhone = phone;
        return this;
    }

    public TenantRequestTestDataBuilder WithSuperintendent(string fullName, string email)
    {
        _superintendentFullName = fullName;
        _superintendentEmail = email;
        return this;
    }

    public TenantRequestTestDataBuilder AsEmergency()
    {
        _urgencyLevel = "Emergency";
        return this;
    }

    public TenantRequestTestDataBuilder AsCritical()
    {
        _urgencyLevel = "Critical";
        return this;
    }

    public TenantRequestTestDataBuilder AsHigh()
    {
        _urgencyLevel = "High";
        return this;
    }

    public TenantRequestTestDataBuilder AsNormal()
    {
        _urgencyLevel = "Normal";
        return this;
    }

    public TenantRequestTestDataBuilder AsLow()
    {
        _urgencyLevel = "Low";
        return this;
    }

    public TenantRequest Build()
    {
        return TenantRequest.CreateNew(
            _code, _title, _description, _urgencyLevel,
            _tenantId, _propertyId,
            _tenantFullName, _tenantEmail, _tenantUnit,
            _propertyName, _propertyPhone,
            _superintendentFullName, _superintendentEmail);
    }

    public TenantRequest BuildSubmitted()
    {
        var request = Build();
        request.Submit();
        return request;
    }

    public TenantRequest BuildScheduled(DateTime? scheduledDate = null, string? workerEmail = null, string? workOrder = null)
    {
        var request = BuildSubmitted();
        request.Schedule(
            scheduledDate ?? DateTime.UtcNow.AddDays(1),
            workerEmail ?? "worker@test.com",
            workOrder ?? "WO-001");
        return request;
    }

    public TenantRequest BuildCompleted(bool successful = true, string? notes = null)
    {
        var request = BuildScheduled();
        request.ReportWorkCompleted(successful, notes ?? "Work completed");
        return request;
    }

    public TenantRequest BuildClosed(string? closureNotes = null)
    {
        var request = BuildCompleted();
        request.Close(closureNotes ?? "Request closed");
        return request;
    }

    public static TenantRequestTestDataBuilder Default() => new TenantRequestTestDataBuilder();

    public static TenantRequestTestDataBuilder ForPlumbingIssue() => new TenantRequestTestDataBuilder()
        .WithTitle("Leaky Faucet")
        .WithDescription("Kitchen faucet is dripping constantly")
        .WithCode("PL-001");

    public static TenantRequestTestDataBuilder ForElectricalIssue() => new TenantRequestTestDataBuilder()
        .WithTitle("Outlet Not Working")
        .WithDescription("Bathroom outlet has no power")
        .WithCode("EL-001");

    public static TenantRequestTestDataBuilder ForHVACIssue() => new TenantRequestTestDataBuilder()
        .WithTitle("Heating Not Working")
        .WithDescription("Unit has no heat")
        .WithCode("HV-001")
        .AsCritical();

    public static TenantRequestTestDataBuilder ForEmergencyIssue() => new TenantRequestTestDataBuilder()
        .WithTitle("Gas Leak")
        .WithDescription("Strong gas smell in kitchen")
        .WithCode("EM-001")
        .AsEmergency();

    public static TenantRequestTestDataBuilder ForMaintenanceIssue() => new TenantRequestTestDataBuilder()
        .WithTitle("Door Handle Loose")
        .WithDescription("Front door handle needs tightening")
        .WithCode("MT-001")
        .AsLow();
}