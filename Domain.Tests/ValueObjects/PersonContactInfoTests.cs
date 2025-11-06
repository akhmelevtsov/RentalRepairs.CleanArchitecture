using RentalRepairs.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.ValueObjects;

public class PersonContactInfoTests
{
    [Fact]
    public void PersonContactInfo_ShouldBeCreated_WithValidData()
    {
        // Arrange & Act
        var contactInfo = new PersonContactInfo("John", "Doe", "john.doe@example.com", "+1-555-1234");

        // Assert
        contactInfo.FirstName.Should().Be("John");
        contactInfo.LastName.Should().Be("Doe");
        contactInfo.EmailAddress.Should().Be("john.doe@example.com");
        contactInfo.MobilePhone.Should().Be("+1-555-1234");
        contactInfo.GetFullName().Should().Be("John Doe");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PersonContactInfo_ShouldThrowException_WhenFirstNameIsInvalid(string firstName)
    {
        // Act & Assert
        Action act = () => new PersonContactInfo(firstName, "Doe", "john.doe@example.com");
        act.Should().Throw<ArgumentException>()
            .WithMessage("First name cannot be empty*");
    }

    [Fact]
    public void PersonContactInfo_ShouldThrowException_WhenFirstNameIsNull()
    {
        // Act & Assert
        Action act = () => new PersonContactInfo(null!, "Doe", "john.doe@example.com");
        act.Should().Throw<ArgumentException>()
            .WithMessage("First name cannot be empty*");
    }

    [Theory]
    [InlineData("invalid-email")] // No @ or .
    [InlineData("john.doe")] // No @ 
    [InlineData("@")] // Only @
    [InlineData("")] // Empty
    public void PersonContactInfo_ShouldThrowException_WhenEmailIsInvalid(string email)
    {
        // Act & Assert
        Action act = () => new PersonContactInfo("John", "Doe", email);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email address*");
    }

    [Fact]
    public void PersonContactInfo_ShouldNormalizeEmail_ToLowerCase()
    {
        // Arrange & Act
        var contactInfo = new PersonContactInfo("John", "Doe", "JOHN.DOE@EXAMPLE.COM");

        // Assert
        contactInfo.EmailAddress.Should().Be("john.doe@example.com");
    }
}