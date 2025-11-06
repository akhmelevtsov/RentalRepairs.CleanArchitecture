using RentalRepairs.Domain.Common;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Entities;

public class Tenant : BaseEntity
{
    private readonly List<TenantRequest> _requests = [];


    protected Tenant()
    {
        // For EF Core
        ContactInfo = null!;
        PropertyCode = string.Empty;
        UnitNumber = string.Empty;
    }

    public Tenant(Guid propertyId, string propertyCode, PersonContactInfo contactInfo, string unitNumber) : base()
    {
        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("Property ID must be valid", nameof(propertyId));
        }

        if (string.IsNullOrWhiteSpace(propertyCode))
        {
            throw new ArgumentException("Property code cannot be empty", nameof(propertyCode));
        }

        if (contactInfo == null)
        {
            throw new ArgumentNullException(nameof(contactInfo));
        }

        if (string.IsNullOrWhiteSpace(unitNumber))
        {
            throw new ArgumentException("Unit number cannot be empty", nameof(unitNumber));
        }

        PropertyId = propertyId;
        PropertyCode = propertyCode;
        ContactInfo = contactInfo;
        UnitNumber = unitNumber;
        RequestsCount = 0;
    }

    public PersonContactInfo ContactInfo { get; private set; }
    public string PropertyCode { get; }
    public string UnitNumber { get; }

    public Guid PropertyId { get; }

    public int RequestsCount { get; private set; }

    public IReadOnlyCollection<TenantRequest> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Rich domain method that creates a new request in Draft status.
    /// Business validation is applied when the request is actually submitted.
    /// </summary>
    public TenantRequest SubmitRequest(string title, string description, TenantRequestUrgency urgency)
    {
        // Only validate input parameters when creating - business rules are applied when submitting
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new TenantRequestDomainException("Request title cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new TenantRequestDomainException("Request description cannot be empty");
        }

        RequestsCount++;
        string requestCode = $"{PropertyCode}-{UnitNumber}-{RequestsCount:D4}";

        // Create the request using factory method - starts in Draft status
        var tenantRequest = TenantRequest.CreateNew(
            requestCode,
            title,
            description,
            urgency.GetDisplayName(), // Convert enum to string for backward compatibility
            Id,
            PropertyId,
            ContactInfo.GetFullName(),
            ContactInfo.EmailAddress,
            UnitNumber,
            "Property Name", // This should come from Property aggregate
            "Property Phone", // This should come from Property aggregate
            "Superintendent Name", // This should come from Property aggregate
            "superintendent@example.com" // This should come from Property aggregate
        );

        _requests.Add(tenantRequest);

        // Domain event is already raised by TenantRequest.CreateNew factory method
        return tenantRequest;
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// Consider using SubmitRequest with TenantRequestUrgency enum instead.
    /// </summary>
    public TenantRequest CreateRequest(string title, string description, string urgencyLevel)
    {
        TenantRequestUrgency urgencyEnum = TenantRequestUrgencyExtensions.FromString(urgencyLevel);
        return SubmitRequest(title, description, urgencyEnum);
    }

    public void UpdateContactInfo(PersonContactInfo newContactInfo)
    {
        if (newContactInfo == null)
        {
            throw new ArgumentNullException(nameof(newContactInfo));
        }

        PersonContactInfo oldContactInfo = ContactInfo;
        ContactInfo = newContactInfo;

        // Domain event for contact info change
        AddDomainEvent(new TenantContactInfoChangedEvent(this, oldContactInfo, newContactInfo));
    }
}
