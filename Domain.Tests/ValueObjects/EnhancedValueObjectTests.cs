using FluentAssertions;
using RentalRepairs.Domain.ValueObjects;
using Xunit;

namespace RentalRepairs.Domain.Tests.ValueObjects;

/// <summary>
/// Enhanced tests for fully immutable PersonContactInfo value object
/// </summary>
public class EnhancedPersonContactInfoTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateInstance()
    {
        // Act
        var contactInfo = new PersonContactInfo("John", "Doe", "john.doe@example.com", "+1-555-123-4567");

        // Assert
        contactInfo.FirstName.Should().Be("John");
        contactInfo.LastName.Should().Be("Doe");
        contactInfo.EmailAddress.Should().Be("john.doe@example.com");
        contactInfo.MobilePhone.Should().Be("+1-555-123-4567");
    }

    [Fact]
    public void Constructor_WithMixedCaseNames_ShouldPreserveOriginalCase()
    {
        // Act
        var contactInfo = new PersonContactInfo("jOHN", "o'CONNOR", "john@example.com");

        // Assert - The implementation preserves the original case as entered
        contactInfo.FirstName.Should().Be("jOHN");
        contactInfo.LastName.Should().Be("o'CONNOR");
        contactInfo.EmailAddress.Should().Be("john@example.com"); // Email is normalized to lowercase
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("John123")]
    [InlineData("John.Smith")]
    public void Constructor_WithInvalidFirstName_ShouldThrowException(string invalidFirstName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PersonContactInfo(invalidFirstName, "Doe", "john@example.com"));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("john@")]
    [InlineData(".john@example.com")]
    [InlineData("john@example.com.")]
    [InlineData("john..doe@example.com")]
    public void Constructor_WithInvalidEmail_ShouldThrowException(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PersonContactInfo("John", "Doe", invalidEmail));
    }

    [Fact]
    public void WithName_ShouldCreateNewInstanceWithUpdatedNames()
    {
        // Arrange
        var original = new PersonContactInfo("John", "Doe", "john@example.com");

        // Act
        var updated = original.WithName("Jane", "Smith");

        // Assert
        updated.FirstName.Should().Be("Jane");
        updated.LastName.Should().Be("Smith");
        updated.EmailAddress.Should().Be("john@example.com"); // Unchanged
        original.FirstName.Should().Be("John"); // Original unchanged
    }

    [Fact]
    public void WithEmail_ShouldCreateNewInstanceWithUpdatedEmail()
    {
        // Arrange
        var original = new PersonContactInfo("John", "Doe", "john@example.com");

        // Act
        var updated = original.WithEmail("jane@example.com");

        // Assert
        updated.EmailAddress.Should().Be("jane@example.com");
        updated.FirstName.Should().Be("John"); // Unchanged
        original.EmailAddress.Should().Be("john@example.com"); // Original unchanged
    }

    [Fact]
    public void GetFullName_ShouldReturnFormattedName()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("John", "Doe", "john@example.com");

        // Act & Assert
        contactInfo.GetFullName().Should().Be("John Doe");
    }

    [Fact]
    public void GetFormalName_ShouldReturnFormattedName()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("John", "Doe", "john@example.com");

        // Act & Assert
        contactInfo.GetFormalName().Should().Be("Doe, John");
    }

    [Fact]
    public void GetInitials_ShouldReturnInitials()
    {
        // Arrange
        var contactInfo = new PersonContactInfo("John", "Doe", "john@example.com");

        // Act & Assert
        contactInfo.GetInitials().Should().Be("J.D.");
    }

    [Fact]
    public void ImmutabilityTest_OriginalUnchangedAfterFactoryMethods()
    {
        // Arrange
        var original = new PersonContactInfo("John", "Doe", "john@example.com", "555-1234");

        // Act - Create multiple variations
        var withNewName = original.WithName("Jane", "Smith");
        var withNewEmail = original.WithEmail("newemail@example.com");
        var withNewPhone = original.WithPhone("555-9999");

        // Assert - Original is completely unchanged
        original.FirstName.Should().Be("John");
        original.LastName.Should().Be("Doe");
        original.EmailAddress.Should().Be("john@example.com");
        original.MobilePhone.Should().Be("555-1234");

        // All variations are different instances
        withNewName.Should().NotBeSameAs(original);
        withNewEmail.Should().NotBeSameAs(original);
        withNewPhone.Should().NotBeSameAs(original);
    }
}

/// <summary>
/// Enhanced tests for fully immutable PropertyAddress value object
/// </summary>
public class EnhancedPropertyAddressTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateInstance()
    {
        // Act
        var address = new PropertyAddress("123", "Main Street", "New York", "10001");

        // Assert
        address.StreetNumber.Should().Be("123");
        address.StreetName.Should().Be("Main Street");
        address.City.Should().Be("New York");
        address.PostalCode.Should().Be("10001");
    }

    [Fact]
    public void Constructor_WithMixedCaseInputs_ShouldPreserveOriginalCase()
    {
        // Act
        var address = new PropertyAddress("123a", "main street", "new york", "ny10001");

        // Assert - PropertyAddress preserves the original case as entered
        address.StreetNumber.Should().Be("123a");
        address.StreetName.Should().Be("main street");
        address.City.Should().Be("new york");
        address.PostalCode.Should().Be("ny10001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901")]
    [InlineData("ABC")]
    public void Constructor_WithInvalidStreetNumber_ShouldThrowException(string invalidStreetNumber)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PropertyAddress(invalidStreetNumber, "Main Street", "New York", "10001"));
    }

    [Fact]
    public void WithCity_ShouldCreateNewInstanceWithUpdatedCity()
    {
        // Arrange
        var original = new PropertyAddress("123", "Main Street", "New York", "10001");

        // Act
        var updated = original.WithCity("Boston");

        // Assert
        updated.City.Should().Be("Boston");
        updated.StreetNumber.Should().Be("123"); // Unchanged
        original.City.Should().Be("New York"); // Original unchanged
    }

    [Fact]
    public void FullAddress_ShouldReturnFormattedAddress()
    {
        // Arrange
        var address = new PropertyAddress("123", "Main Street", "New York", "10001");

        // Act & Assert
        address.FullAddress.Should().Be("123 Main Street, New York, 10001");
    }

    [Fact]
    public void ImmutabilityTest_OriginalUnchangedAfterFactoryMethods()
    {
        // Arrange
        var original = new PropertyAddress("123", "Main Street", "New York", "10001");

        // Act - Create variations
        var withNewStreet = original.WithStreetAddress("456", "Oak Avenue");
        var withNewCity = original.WithCity("Boston");
        var withNewPostal = original.WithPostalCode("02101");

        // Assert - Original is completely unchanged
        original.StreetNumber.Should().Be("123");
        original.StreetName.Should().Be("Main Street");
        original.City.Should().Be("New York");
        original.PostalCode.Should().Be("10001");

        // All variations are different instances
        withNewStreet.Should().NotBeSameAs(original);
        withNewCity.Should().NotBeSameAs(original);
        withNewPostal.Should().NotBeSameAs(original);
    }
}

/// <summary>
/// Enhanced tests for fully immutable ServiceWorkScheduleInfo value object
/// </summary>
public class EnhancedServiceWorkScheduleInfoTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateInstance()
    {
        // Arrange
        var futureDate = DateTime.Today.AddDays(1);

        // Act
        var scheduleInfo = new ServiceWorkScheduleInfo(
            futureDate,
            "worker@example.com",
            "WO-123",
            1);

        // Assert
        scheduleInfo.ServiceDate.Should().Be(futureDate);
        scheduleInfo.WorkerEmail.Should().Be("worker@example.com");
        scheduleInfo.WorkOrderNumber.Should().Be("WO-123");
        scheduleInfo.WorkOrderSequence.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithPastDate_ShouldThrowException()
    {
        // Arrange
        var pastDate = DateTime.Today.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ServiceWorkScheduleInfo(pastDate, "worker@example.com", "WO-123", 1));
    }

    [Fact]
    public void Reschedule_ShouldCreateNewInstanceWithIncrementedSequence()
    {
        // Arrange
        var futureDate = DateTime.Today.AddDays(1);
        var newDate = DateTime.Today.AddDays(2);
        var original = new ServiceWorkScheduleInfo(futureDate, "worker@example.com", "WO-123", 1);

        // Act
        var rescheduled = original.Reschedule(newDate);

        // Assert
        rescheduled.ServiceDate.Should().Be(newDate);
        rescheduled.WorkOrderSequence.Should().Be(2);
        rescheduled.WorkerEmail.Should().Be("worker@example.com"); // Unchanged
        original.WorkOrderSequence.Should().Be(1); // Original unchanged
    }

    [Fact]
    public void IsScheduledForToday_WithTodayDate_ShouldReturnTrue()
    {
        // Arrange
        var today = DateTime.Today.AddHours(10);
        var scheduleInfo = new ServiceWorkScheduleInfo(today, "worker@example.com", "WO-123", 1);

        // Act & Assert
        scheduleInfo.IsScheduledForToday().Should().BeTrue();
    }

    [Fact]
    public void DaysUntilService_ShouldReturnCorrectValue()
    {
        // Arrange
        var futureDate = DateTime.Today.AddDays(5);
        var scheduleInfo = new ServiceWorkScheduleInfo(futureDate, "worker@example.com", "WO-123", 1);

        // Act & Assert
        scheduleInfo.DaysUntilService().Should().Be(5);
    }

    [Fact]
    public void ImmutabilityTest_OriginalUnchangedAfterFactoryMethods()
    {
        // Arrange
        var futureDate = DateTime.Today.AddDays(1);
        var original = new ServiceWorkScheduleInfo(futureDate, "worker@example.com", "WO-123", 1);

        // Act - Create variations
        var withNewDate = original.WithServiceDate(DateTime.Today.AddDays(3));
        var withNewWorker = original.WithWorkerEmail("newworker@example.com");
        var rescheduled = original.Reschedule(DateTime.Today.AddDays(4));

        // Assert - Original is completely unchanged
        original.ServiceDate.Should().Be(futureDate);
        original.WorkerEmail.Should().Be("worker@example.com");
        original.WorkOrderNumber.Should().Be("WO-123");
        original.WorkOrderSequence.Should().Be(1);

        // All variations are different instances
        withNewDate.Should().NotBeSameAs(original);
        withNewWorker.Should().NotBeSameAs(original);
        rescheduled.Should().NotBeSameAs(original);
    }
}