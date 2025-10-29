using FluentAssertions;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Services;
using Xunit;

namespace RentalRepairs.Application.Tests.Services;

/// <summary>
/// Essential application layer tests focusing on basic functionality
/// </summary>
public class ApplicationLayerEssentialTests
{
    [Fact]
    public void DTOs_Should_Have_Required_Properties()
    {
        // Test that essential DTOs have expected structure
        
        var tenantRequestDto = new TenantRequestDto();
        var tenantDto = new TenantDto();
        var propertyDto = new PropertyDto();

        // Verify DTOs can be instantiated and have basic properties
        tenantRequestDto.Should().NotBeNull();
        tenantDto.Should().NotBeNull();
        propertyDto.Should().NotBeNull();
        
        // Test property assignment
        tenantRequestDto.Title = "Test Title";
        tenantRequestDto.Title.Should().Be("Test Title");
        
        tenantDto.ContactInfo = new PersonContactInfoDto();
        tenantDto.ContactInfo.Should().NotBeNull();
        
        propertyDto.Name = "Test Property";
        propertyDto.Name.Should().Be("Test Property");
    }





    [Fact]
    public void Essential_DTOs_Should_Support_Property_Assignment()
    {
        // Test that DTOs support basic property operations
        
        var tenantRequestDto = new TenantRequestDto
        {
            Title = "Test Request",
            Description = "Test Description",
            Status = "Draft"
        };

        tenantRequestDto.Title.Should().Be("Test Request");
        tenantRequestDto.Description.Should().Be("Test Description");
        tenantRequestDto.Status.Should().Be("Draft");

        var propertyDto = new PropertyDto
        {
            Name = "Test Property",
            Code = "TP-001"
        };

        propertyDto.Name.Should().Be("Test Property");
        propertyDto.Code.Should().Be("TP-001");
    }
}