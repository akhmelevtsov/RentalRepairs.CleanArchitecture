using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Extensions;
using RentalRepairs.Domain.Enums;

namespace RentalRepairs.Domain.Tests.Extensions;

/// <summary>
/// Simple tests for Step 3: Enhanced Collection Extensions.
/// Verifies basic functionality without complex test frameworks.
/// </summary>
public class Step3CollectionExtensionsBasicTests
{
    public static void RunBasicTests()
    {
        TestTenantRequestExtensions();
        TestPropertyExtensions();
        TestCrossAggregateAnalytics();
    }

    private static void TestTenantRequestExtensions()
    {
        // Test RequiringImmediateAttention
        var requests = new List<TenantRequest>
        {
            CreateTestRequest("Normal request", "Regular issue", "Normal"),
            CreateEmergencyRequest("Emergency plumbing", "Burst pipe")
        };

        var urgentRequests = requests.RequiringImmediateAttention();
        if (urgentRequests.Count != 1)
            throw new Exception("RequiringImmediateAttention test failed");

        // Test WithStatuses
        var statusFilteredRequests = requests.WithStatuses(TenantRequestStatus.Submitted);
        if (statusFilteredRequests.Count != 2)
            throw new Exception("WithStatuses test failed");

        // Test performance metrics
        var performanceMetrics = requests.CalculateComprehensivePerformanceMetrics();
        if (performanceMetrics.TotalRequests != 2)
            throw new Exception("Performance metrics test failed");

        Console.WriteLine("TenantRequest extensions tests passed");
    }

    private static void TestPropertyExtensions()
    {
        var properties = new List<Property>
        {
            CreateTestProperty("Property A", "PA001"),
            CreateTestProperty("Property B", "PB001")
        };

        var portfolioMetrics = properties.CalculatePortfolioMetrics();
        if (portfolioMetrics.TotalProperties != 2)
            throw new Exception("Portfolio metrics test failed");

        var groupedBySize = properties.GroupBySize();
        if (!groupedBySize.ContainsKey("Very Small (<5 units)"))
            throw new Exception("GroupBySize test failed");

        Console.WriteLine("Property extensions tests passed");
    }

    private static void TestCrossAggregateAnalytics()
    {
        var properties = new List<Property>
        {
            CreateTestProperty("Test Property", "TP001")
        };

        var workers = new List<Worker>
        {
            CreateActiveWorker("Plumbing", "plumber@test.com")
        };

        var requests = new List<TenantRequest>
        {
            CreateTestRequest("Test request", "Description", "Normal")
        };

        var maintenanceAnalysis = properties.AnalyzeMaintenancePatterns(requests);
        if (maintenanceAnalysis.TotalProperties != 1)
            throw new Exception("Maintenance analysis test failed");

        var resourceAnalysis = properties.AnalyzeSystemResources(workers, requests);
        if (resourceAnalysis.PropertyUtilization.TotalProperties != 1)
            throw new Exception("Resource analysis test failed");

        Console.WriteLine("Cross-aggregate analytics tests passed");
    }

    #region Helper Methods

    private static TenantRequest CreateTestRequest(string title, string description, string urgencyLevel)
    {
        var property = CreateTestProperty("Test Property", "TP001");
        var tenant = property.RegisterTenant(
            new PersonContactInfo("Jane", "Smith", "jane@test.com"), 
            "101");

        var request = tenant.CreateRequest(title, description, urgencyLevel);
        request.SubmitForReview();
        return request;
    }

    private static TenantRequest CreateEmergencyRequest(string title, string description)
    {
        return CreateTestRequest(title, description, "Emergency");
    }

    private static Property CreateTestProperty(string name, string code)
    {
        return new Property(
            name,
            code,
            new PropertyAddress("123", "Test St", "Test City", "12345"),
            "555-1234",
            new PersonContactInfo("John", "Doe", "john@test.com"),
            new List<string> { "101", "102", "103" },
            "noreply@test.com");
    }

    private static Worker CreateActiveWorker(string specialization, string email)
    {
        var contactInfo = new PersonContactInfo("John", "Worker", email);
        var worker = new Worker(contactInfo);
        worker.SetSpecialization(specialization);
        return worker;
    }

    #endregion
}