using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Events.Properties;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Services;
using Xunit;
using FluentAssertions;
using Moq;

namespace RentalRepairs.Domain.Tests.Entities;

public class TenantTests
{
    private readonly Mock<ITenantRequestSubmissionPolicy> _mockPolicy;

    public TenantTests()
    {
        _mockPolicy = new Mock<ITenantRequestSubmissionPolicy>();
        // Default behavior: allow submissions
        _mockPolicy.Setup(p => p.ValidateCanSubmitRequest(It.IsAny<Tenant>(), It.IsAny<TenantRequestUrgency>()));
        _mockPolicy.Setup(p => p.CanSubmitRequest(It.IsAny<Tenant>(), It.IsAny<TenantRequestUrgency>())).Returns(true);
        _mockPolicy.Setup(p => p.GetNextAllowedSubmissionTime(It.IsAny<Tenant>())).Returns((DateTime?)null);
        _mockPolicy.Setup(p => p.GetRemainingEmergencyRequests(It.IsAny<Tenant>())).Returns(3);
    }

    [Fact]
    public void Tenant_ShouldBeCreated_WithValidParameters()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var contactInfo = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");

        // Act
        var tenant = new Tenant(propertyId, "PROP001", contactInfo, "101");

        // Assert
        tenant.Should().NotBeNull();
        tenant.PropertyId.Should().Be(propertyId);
        tenant.PropertyCode.Should().Be("PROP001");
        tenant.ContactInfo.Should().Be(contactInfo);
        tenant.UnitNumber.Should().Be("101");
        tenant.RequestsCount.Should().Be(0);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "PROP001", "101")] // Empty Guid
    [InlineData("12345678-1234-1234-1234-123456789012", "", "101")]          // Empty property code
    [InlineData("12345678-1234-1234-1234-123456789012", "PROP001", "")]      // Empty unit number
    public void Tenant_ShouldThrowException_WithInvalidParameters(string propertyIdString, string propertyCode, string unitNumber)
    {
        // Arrange
        var propertyId = Guid.Parse(propertyIdString);
        var contactInfo = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");

        // Act & Assert
        Action act = () => new Tenant(propertyId, propertyCode, contactInfo, unitNumber);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Tenant_ShouldThrowException_WithNullContactInfo()
    {
        // Arrange
        var propertyId = Guid.NewGuid();

        // Act & Assert
        Action act = () => new Tenant(propertyId, "PROP001", null!, "101");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateRequest_ShouldCreateValidTenantRequest()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var request = tenant.CreateRequest("Leaky Faucet", "Kitchen faucet is dripping", "High");

        // Assert
        request.Should().NotBeNull();
        request.Title.Should().Be("Leaky Faucet");
        request.Description.Should().Be("Kitchen faucet is dripping");
        request.UrgencyLevel.Should().Be("High");
        request.TenantId.Should().Be(tenant.Id);
        request.Status.Should().Be(TenantRequestStatus.Draft);
        tenant.RequestsCount.Should().Be(1);
        tenant.Requests.Should().HaveCount(1);
        tenant.Requests.Should().Contain(request);
    }

    [Fact]
    public void SubmitRequest_ShouldCreateValidTenantRequest_WithEnum()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var request = tenant.SubmitRequest("Leaky Faucet", "Kitchen faucet is dripping", TenantRequestUrgency.High);

        // Assert
        request.Should().NotBeNull();
        request.Title.Should().Be("Leaky Faucet");
        request.Description.Should().Be("Kitchen faucet is dripping");
        request.UrgencyLevel.Should().Be("High");
        request.TenantId.Should().Be(tenant.Id);
        request.Status.Should().Be(TenantRequestStatus.Draft);
        tenant.RequestsCount.Should().Be(1);
        tenant.Requests.Should().HaveCount(1);
        tenant.Requests.Should().Contain(request);
    }

    [Theory]
    [InlineData("", "Description", "Normal")]    // Empty title
    [InlineData("Title", "", "Normal")]          // Empty description  
    [InlineData("Title", "Description", "")]     // Empty urgency
    [InlineData("Title", "Description", "Invalid")] // Invalid urgency
    public void CreateRequest_ShouldThrowException_WithInvalidInput(string title, string description, string urgencyLevel)
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act & Assert
        Action act = () => tenant.CreateRequest(title, description, urgencyLevel);
        act.Should().Throw<TenantRequestDomainException>();
    }

    [Theory]
    [InlineData("", "Description")]    // Empty title
    [InlineData("Title", "")]          // Empty description  
    public void SubmitRequest_ShouldThrowException_WithInvalidInput(string title, string description)
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act & Assert
        Action act = () => tenant.SubmitRequest(title, description, TenantRequestUrgency.Normal);
        act.Should().Throw<TenantRequestDomainException>();
    }

    [Fact]
    public void CreateRequest_ShouldGenerateUniqueRequestCodes()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var request1 = tenant.CreateRequest("Request 1", "Description 1", "Normal");
        var request2 = tenant.CreateRequest("Request 2", "Description 2", "High");

        // Assert
        request1.Code.Should().NotBe(request2.Code);
        request1.Code.Should().Contain("PROP001-101-");
        request2.Code.Should().Contain("PROP001-101-");
        tenant.RequestsCount.Should().Be(2);
    }

    [Fact]
    public void SubmitTenantRequest_ShouldEnforceRateLimit_WhenPolicyThrows()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var request1 = tenant.SubmitRequest("Request 1", "Description 1", TenantRequestUrgency.Normal);
        tenant.SubmitTenantRequest(request1, TenantRequestUrgency.Normal, _mockPolicy.Object);

        // Setup policy to throw rate limit exception for second request
        var mockPolicyForSecond = new Mock<ITenantRequestSubmissionPolicy>();
        mockPolicyForSecond.Setup(p => p.ValidateCanSubmitRequest(It.IsAny<Tenant>(), It.IsAny<TenantRequestUrgency>()))
            .Throws(new SubmissionRateLimitExceededException(TimeSpan.FromMinutes(50)));

        // Act & Assert - Should throw when trying to submit another request immediately
        var request2 = tenant.SubmitRequest("Request 2", "Description 2", TenantRequestUrgency.Normal);
        Action act = () => tenant.SubmitTenantRequest(request2, TenantRequestUrgency.Normal, mockPolicyForSecond.Object);
        act.Should().Throw<SubmissionRateLimitExceededException>();
    }

    [Fact]
    public void CanSubmitRequest_ShouldReturnTrue_WhenPolicyAllows()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act & Assert
        tenant.CanSubmitRequest(TenantRequestUrgency.Normal, _mockPolicy.Object).Should().BeTrue();
        tenant.CanSubmitRequest(TenantRequestUrgency.Emergency, _mockPolicy.Object).Should().BeTrue();
    }

    [Fact]
    public void CanSubmitRequest_ShouldReturnFalse_WhenPolicyBlocks()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var mockPolicy = new Mock<ITenantRequestSubmissionPolicy>();
        mockPolicy.Setup(p => p.CanSubmitRequest(It.IsAny<Tenant>(), It.IsAny<TenantRequestUrgency>())).Returns(false);

        // Act & Assert
        tenant.CanSubmitRequest(TenantRequestUrgency.Normal, mockPolicy.Object).Should().BeFalse();
    }

    [Fact]
    public void SubmitTenantRequest_ShouldThrowException_WhenRequestNotBelongToTenant()
    {
        // Arrange
        var tenant1 = CreateTestTenant();
        var tenant2 = CreateTestTenant();
        var request = tenant2.SubmitRequest("Request", "Description", TenantRequestUrgency.Normal);

        // Act & Assert
        Action act = () => tenant1.SubmitTenantRequest(request, TenantRequestUrgency.Normal, _mockPolicy.Object);
        act.Should().Throw<TenantRequestDomainException>()
           .WithMessage("*does not belong to this tenant*");
    }

    [Fact]
    public void SubmitTenantRequest_ShouldThrowException_WhenRequestNull()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act & Assert
        Action act = () => tenant.SubmitTenantRequest(null!, TenantRequestUrgency.Normal, _mockPolicy.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SubmitTenantRequest_ShouldThrowException_WhenPolicyNull()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var request = tenant.SubmitRequest("Request", "Description", TenantRequestUrgency.Normal);

        // Act & Assert
        Action act = () => tenant.SubmitTenantRequest(request, TenantRequestUrgency.Normal, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetNextAllowedSubmissionTime_ShouldReturnPolicyResult()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var expectedTime = DateTime.UtcNow.AddMinutes(30);
        var mockPolicy = new Mock<ITenantRequestSubmissionPolicy>();
        mockPolicy.Setup(p => p.GetNextAllowedSubmissionTime(tenant)).Returns(expectedTime);

        // Act
        var result = tenant.GetNextAllowedSubmissionTime(mockPolicy.Object);

        // Assert
        result.Should().Be(expectedTime);
    }

    [Fact]
    public void GetRemainingEmergencyRequests_ShouldReturnPolicyResult()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var mockPolicy = new Mock<ITenantRequestSubmissionPolicy>();
        mockPolicy.Setup(p => p.GetRemainingEmergencyRequests(tenant)).Returns(2);

        // Act
        var result = tenant.GetRemainingEmergencyRequests(mockPolicy.Object);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void UpdateContactInfo_ShouldUpdateContactInfoSuccessfully()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var newContactInfo = new PersonContactInfo("Jane", "Smith", "jane.smith@test.com", "555-9999");

        // Act
        tenant.UpdateContactInfo(newContactInfo);

        // Assert
        tenant.ContactInfo.Should().Be(newContactInfo);
        tenant.ContactInfo.GetFullName().Should().Be("Jane Smith");
        tenant.ContactInfo.EmailAddress.Should().Be("jane.smith@test.com");
        tenant.DomainEvents.Should().Contain(e => e is TenantContactInfoChangedEvent);
    }

    [Fact]
    public void UpdateContactInfo_ShouldThrowException_WithNullContactInfo()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act & Assert
        Action act = () => tenant.UpdateContactInfo(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateContactInfo_ShouldRaiseDomainEvent()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var oldContactInfo = tenant.ContactInfo;
        var newContactInfo = new PersonContactInfo("Updated", "Name", "updated@test.com");

        // Act
        tenant.UpdateContactInfo(newContactInfo);

        // Assert
        tenant.DomainEvents.Should().HaveCount(1);
        var changeEvent = tenant.DomainEvents.OfType<TenantContactInfoChangedEvent>().First();
        changeEvent.Tenant.Should().Be(tenant);
        changeEvent.OldContactInfo.Should().Be(oldContactInfo);
        changeEvent.NewContactInfo.Should().Be(newContactInfo);
    }

    [Fact]
    public void Tenant_Properties_ShouldBeReadOnly()
    {
        // Arrange & Act
        var tenant = CreateTestTenant();

        // Assert - Verify that properties cannot be modified externally
        // This is enforced by private setters in the properties
        tenant.PropertyCode.Should().Be("PROP001");
        tenant.UnitNumber.Should().Be("101");
        tenant.Requests.Should().BeAssignableTo<IReadOnlyCollection<TenantRequest>>();
    }

    [Fact]
    public void RequestsCollection_ShouldBeReadOnly()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.CreateRequest("Test Request", "Description", "Normal");

        // Act & Assert
        var requests = tenant.Requests;
        requests.Should().BeAssignableTo<IReadOnlyCollection<TenantRequest>>();
        requests.Should().HaveCount(1);
        
        // Verify collection cannot be cast to a mutable type
        requests.Should().NotBeAssignableTo<List<TenantRequest>>();
    }

    private static Tenant CreateTestTenant()
    {
        var propertyId = Guid.NewGuid();
        var contactInfo = new PersonContactInfo("John", "Doe", "john@test.com", "555-1234");
        return new Tenant(propertyId, "PROP001", contactInfo, "101");
    }
}