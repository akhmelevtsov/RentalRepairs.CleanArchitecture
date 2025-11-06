using RentalRepairs.Infrastructure.Seeding.Models;

namespace RentalRepairs.Infrastructure.Seeding.Data;

public static class SeedDataGenerator
{
    public static List<TestPropertyData> GenerateProperties(int count, int tenantsPerProperty)
    {
        var properties = new List<TestPropertyData>();
        var propertyTemplates = GetPropertyTemplates();

        for (var i = 0; i < count && i < propertyTemplates.Count; i++)
        {
            var template = propertyTemplates[i];
            var property = new TestPropertyData
            {
                Name = template.Name,
                Code = template.Code,
                Address = template.Address,
                PhoneNumber = template.PhoneNumber,
                NoReplyEmail = template.NoReplyEmail,
                Superintendent = new TestPersonData
                {
                    FirstName = template.Superintendent.FirstName,
                    LastName = template.Superintendent.LastName,
                    Email = $"super.{template.Code.ToLower()}@rentalrepairs.com",
                    MobilePhone = GeneratePhoneNumber()
                },
                Units = template.Units,
                Tenants = GenerateTenantsForProperty(template, tenantsPerProperty)
            };

            properties.Add(property);
        }

        return properties;
    }

    public static List<TestWorkerData> GenerateWorkers(int count)
    {
        var workers = new List<TestWorkerData>();
        var specializations = new[]
        {
            "Plumber", "Electrician", "HVAC", "General Maintenance", "Painter", "Carpenter", "Locksmith",
            "Appliance Repair"
        };
        var firstNames = new[]
        {
            "Mike", "Sarah", "Tom", "Lisa", "David", "Emma", "James", "Anna", "Robert", "Jessica", "Kevin", "Michelle"
        };
        var lastNames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
            "Hernandez", "Lopez"
        };

        for (var i = 0; i < count; i++)
        {
            var specialization = specializations[i % specializations.Length];
            var firstName = firstNames[i % firstNames.Length];
            var lastName = lastNames[i % lastNames.Length];

            workers.Add(new TestWorkerData
            {
                ContactInfo = new TestPersonData
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{specialization.ToLower().Replace(" ", "")}.{lastName.ToLower()}@workers.com",
                    MobilePhone = GeneratePhoneNumber()
                },
                Specialization = specialization
            });
        }

        return workers;
    }

    private static List<TestPropertyData> GetPropertyTemplates()
    {
        return new List<TestPropertyData>
        {
            new()
            {
                Name = "Sunset Apartments",
                Code = "SUN001",
                Address = new TestAddressData
                    { StreetNumber = "123", StreetName = "Sunset Blvd", City = "Los Angeles", PostalCode = "90210" },
                PhoneNumber = "(555) 123-4567",
                NoReplyEmail = "noreply@sunset-apartments.com",
                Superintendent = new TestPersonData { FirstName = "Robert", LastName = "Martinez" },
                Units = new List<string> { "101", "102", "103", "201", "202", "203", "301", "302" }
            },
            new()
            {
                Name = "Maple Grove Condos",
                Code = "MAP001",
                Address = new TestAddressData
                    { StreetNumber = "456", StreetName = "Maple Ave", City = "Portland", PostalCode = "97201" },
                PhoneNumber = "(555) 234-5678",
                NoReplyEmail = "noreply@maple-grove.com",
                Superintendent = new TestPersonData { FirstName = "Linda", LastName = "Thompson" },
                Units = new List<string> { "A1", "A2", "A3", "B1", "B2", "B3", "C1", "C2" }
            },
            new()
            {
                Name = "Oak Hill Residences",
                Code = "OAK001",
                Address = new TestAddressData
                    { StreetNumber = "789", StreetName = "Oak Hill Dr", City = "Seattle", PostalCode = "98101" },
                PhoneNumber = "(555) 345-6789",
                NoReplyEmail = "noreply@oakhill-residences.com",
                Superintendent = new TestPersonData { FirstName = "Michael", LastName = "Anderson" },
                Units = new List<string> { "1A", "1B", "2A", "2B", "3A", "3B", "4A", "4B" }
            },
            new()
            {
                Name = "Pine Valley Apartments",
                Code = "PIN001",
                Address = new TestAddressData
                    { StreetNumber = "321", StreetName = "Pine Valley Rd", City = "Denver", PostalCode = "80202" },
                PhoneNumber = "(555) 456-7890",
                NoReplyEmail = "noreply@pine-valley.com",
                Superintendent = new TestPersonData { FirstName = "Jennifer", LastName = "Wilson" },
                Units = new List<string> { "101", "102", "201", "202", "301", "302", "401", "402" }
            },
            new()
            {
                Name = "Cedar Park Townhomes",
                Code = "CED001",
                Address = new TestAddressData
                    { StreetNumber = "654", StreetName = "Cedar Park Way", City = "Austin", PostalCode = "73301" },
                PhoneNumber = "(555) 567-8901",
                NoReplyEmail = "noreply@cedar-park.com",
                Superintendent = new TestPersonData { FirstName = "Christopher", LastName = "Taylor" },
                Units = new List<string> { "TH1", "TH2", "TH3", "TH4", "TH5", "TH6" }
            }
        };
    }

    private static List<TestTenantData> GenerateTenantsForProperty(TestPropertyData property, int count)
    {
        var tenants = new List<TestTenantData>();
        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Frank", "Grace", "Henry", "Ivy" };
        var lastNames = new[]
            { "Smith", "Doe", "Johnson", "Brown", "Wilson", "Moore", "Taylor", "Anderson", "Thomas", "Jackson" };

        var availableUnits = property.Units.Take(count).ToList();

        for (var i = 0; i < Math.Min(count, availableUnits.Count); i++)
        {
            var firstName = firstNames[i % firstNames.Length];
            var lastName = lastNames[i % lastNames.Length];
            var unit = availableUnits[i];
            var propertyName = property.Name.ToLower().Replace(" ", "").Replace("apartments", "").Replace("condos", "")
                .Replace("residences", "").Replace("townhomes", "");

            tenants.Add(new TestTenantData
            {
                ContactInfo = new TestPersonData
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"tenant{i + 1}.unit{unit.ToLower()}@{propertyName}.com",
                    MobilePhone = GeneratePhoneNumber()
                },
                UnitNumber = unit
            });
        }

        return tenants;
    }

    private static string GeneratePhoneNumber()
    {
        var random = new Random();
        return $"({random.Next(200, 999)}) {random.Next(200, 999)}-{random.Next(1000, 9999)}";
    }

    public static List<CredentialEntry> GenerateSystemAdminCredentials()
    {
        return new List<CredentialEntry>
        {
            new()
            {
                Email = "admin@rentalrepairs.com",
                Password = "password123",
                DisplayName = "System Administrator",
                Role = "SystemAdmin",
                AdditionalInfo = "Full system access"
            }
        };
    }
}