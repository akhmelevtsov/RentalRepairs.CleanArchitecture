using RentalRepairs.WebUI.Services;

namespace RentalRepairs.WebUI.Tests.Services;

/// <summary>
/// Unit tests for RandomTenantRequestGenerator service
/// </summary>
public class RandomTenantRequestGeneratorTests
{
    [Fact]
    public void GenerateRandomRequest_ShouldReturnValidRequest()
    {
        // Act
        var result = RandomTenantRequestGenerator.GenerateRandomRequest();

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.ProblemDescription));
        Assert.False(string.IsNullOrWhiteSpace(result.UrgencyLevel));
        Assert.False(string.IsNullOrWhiteSpace(result.PreferredContactTime));

        // Verify problem description has reasonable length
        Assert.True(result.ProblemDescription.Length >= 50);
        Assert.True(result.ProblemDescription.Length <= 1000);

        // Verify urgency level is valid
        var validUrgencyLevels = new[] { "Low", "Normal", "High", "Critical" };
        Assert.Contains(result.UrgencyLevel, validUrgencyLevels);

        // Verify contact time is valid
        var validContactTimes = new[]
        {
            "Morning (8 AM - 12 PM)",
            "Afternoon (12 PM - 5 PM)",
            "Evening (5 PM - 8 PM)",
            "Anytime"
        };
        Assert.Contains(result.PreferredContactTime, validContactTimes);
    }

    [Fact]
    public void GenerateRandomRequest_ShouldReturnDifferentRequests()
    {
        // Act
        var request1 = RandomTenantRequestGenerator.GenerateRandomRequest();
        var request2 = RandomTenantRequestGenerator.GenerateRandomRequest();

        // Assert - requests should be different (very high probability)
        Assert.NotEqual(request1.ProblemDescription, request2.ProblemDescription);
    }

    [Fact]
    public void GenerateMultipleRequests_ShouldReturnCorrectCount()
    {
        // Arrange
        var requestCount = 5;

        // Act
        var results = RandomTenantRequestGenerator.GenerateMultipleRequests(requestCount);

        // Assert
        Assert.Equal(requestCount, results.Count);
        Assert.All(results, request =>
        {
            Assert.False(string.IsNullOrWhiteSpace(request.ProblemDescription));
            Assert.False(string.IsNullOrWhiteSpace(request.UrgencyLevel));
            Assert.False(string.IsNullOrWhiteSpace(request.PreferredContactTime));
        });
    }

    [Fact]
    public void GenerateRandomRequest_ShouldContainRealisticContent()
    {
        // Act
        var result = RandomTenantRequestGenerator.GenerateRandomRequest();

        // Assert - problem description should contain maintenance-related keywords
        var maintenanceKeywords = new[]
        {
            // Water/Plumbing related
            "leak", "drip", "broken", "water", "sink", "toilet", "faucet", "drain", "heater", "plumbing",
            "backing up", "pooling", "pressure", "running", "humming", "garbage disposal", "loose", "wobbles",

            // Electrical related  
            "electrical", "outlet", "light", "switch", "sparking", "flicker", "ceiling fan", "grinding",
            "tripping", "doorbell", "circuit", "GFCI", "wiring", "buzzing", "sounds",

            // HVAC related
            "heating", "cooling", "air conditioning", "heater", "thermostat", "vents", "exhaust fan",
            "temperature", "dusty", "musty", "grinding", "vibrating", "steamy", "blowing",

            // General maintenance
            "not working", "noise", "crack", "damage", "repair", "fix", "replace", "paint",
            "door", "window", "creak", "creaking", "stain", "peeling", "tile", "vinyl", "laminate",
            "buckling", "spongy", "burns", "crooked", "sways", "mounting", "stuck", "jiggled",
            "cloudy", "faulty", "malfunctioning", "treatment", "professional", "safety", "hazard",

            // Pest related
            "insects", "pest", "ants", "cockroaches", "mice", "wasps", "spiders", "nest", "droppings",
            "scratching", "fruit flies", "swarming", "buzzing",

            // Structural issues
            "ceiling", "roof", "floor", "wall", "foundation", "structural", "sagging", "cracked",
            "discolored", "condensation", "flaking", "tiles", "insulation", "attic"
        };

        var descriptionLower = result.ProblemDescription.ToLowerInvariant();
        var containsMaintenanceKeyword = maintenanceKeywords.Any(keyword =>
            descriptionLower.Contains(keyword));

        Assert.True(containsMaintenanceKeyword,
            $"Problem description should contain maintenance-related keywords. Description: {result.ProblemDescription}");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void GenerateMultipleRequests_WithDifferentCounts_ShouldWork(int count)
    {
        // Act
        var results = RandomTenantRequestGenerator.GenerateMultipleRequests(count);

        // Assert
        Assert.Equal(count, results.Count);
        Assert.All(results, request => Assert.NotNull(request.ProblemDescription));
    }
}