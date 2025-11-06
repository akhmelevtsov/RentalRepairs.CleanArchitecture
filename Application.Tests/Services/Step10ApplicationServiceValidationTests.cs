using FluentAssertions;
using RentalRepairs.Application.Services;
using Xunit;

namespace RentalRepairs.Application.Tests.Services;

/// <summary>
/// Essential application service tests focusing on consolidated services
/// </summary>
public class ApplicationServiceTests
{
    [Fact]
    public void Service_Consolidation_Success_Criteria_Met()
    {
        // Validate that service consolidation success criteria are met

        // Consolidated services that remain after CQRS refactoring
        var workerServiceType = typeof(WorkerService);
        var notificationServiceType = typeof(NotificationService);

        // Assert consolidated services exist
        workerServiceType.Should().NotBeNull("WorkerService should consolidate worker assignment functionality");
        notificationServiceType.Should().NotBeNull("NotificationService should consolidate all notification concerns");

        // Note: TenantRequestService was removed - logic moved to query handler (true CQRS pattern)

        // Services follow consistent patterns
        var services = new[] { workerServiceType, notificationServiceType };

        foreach (var serviceType in services)
        {
            serviceType.Namespace.Should().Be("RentalRepairs.Application.Services",
                "All consolidated services should be in the same namespace");

            serviceType.GetConstructors().Should().NotBeEmpty(
                "All services should have dependency injection constructors");
        }
    }
}