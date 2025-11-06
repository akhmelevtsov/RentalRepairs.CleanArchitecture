using RentalRepairs.Domain.Exceptions;
using Xunit;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void TenantRequestDomainException_ShouldBeCreated_WithMessage()
    {
        // Arrange
        var message = "Test domain exception message";

        // Act
        var exception = new TenantRequestDomainException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Should().BeOfType<TenantRequestDomainException>();
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void TenantRequestDomainException_ShouldBeCreated_WithMessageAndInnerException()
    {
        // Arrange
        var message = "Test domain exception message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new TenantRequestDomainException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void PropertyDomainException_ShouldBeCreated_WithMessage()
    {
        // Arrange
        var message = "Test property domain exception";

        // Act
        var exception = new PropertyDomainException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Should().BeOfType<PropertyDomainException>();
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void PropertyDomainException_ShouldBeCreated_WithMessageAndInnerException()
    {
        // Arrange
        var message = "Test property domain exception";
        var innerException = new ArgumentException("Inner exception");

        // Act
        var exception = new PropertyDomainException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void WorkerDomainException_ShouldBeCreated_WithMessage()
    {
        // Arrange
        var message = "Test worker domain exception";

        // Act
        var exception = new WorkerDomainException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Should().BeOfType<WorkerDomainException>();
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void DomainException_ShouldBeProperlyConstructed()
    {
        // This test ensures domain exceptions have proper constructors and inheritance

        // Arrange & Act
        var tenantRequestException = new TenantRequestDomainException("Test message");
        var propertyException = new PropertyDomainException("Test message");
        var workerException = new WorkerDomainException("Test message");

        // Assert - All domain exceptions should inherit from DomainException
        tenantRequestException.Should().BeAssignableTo<DomainException>();
        propertyException.Should().BeAssignableTo<DomainException>();
        workerException.Should().BeAssignableTo<DomainException>();

        // All should ultimately inherit from Exception
        tenantRequestException.Should().BeAssignableTo<Exception>();
        propertyException.Should().BeAssignableTo<Exception>();
        workerException.Should().BeAssignableTo<Exception>();
    }
}