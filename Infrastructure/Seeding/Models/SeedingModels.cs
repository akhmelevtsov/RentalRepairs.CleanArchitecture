namespace RentalRepairs.Infrastructure.Seeding.Models;

public class SeedingOptions
{
    public const string SectionName = "Seeding";

    public bool EnableSeeding { get; set; } = true;
    public int PropertyCount { get; set; } = 4;
    public int TenantsPerProperty { get; set; } = 4;
    public int WorkerCount { get; set; } = 12;
    public string DefaultPassword { get; set; } = "Demo123!";
    public string CredentialsFileName { get; set; } = "development-credentials.md";
}

public class TestPropertyData
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public TestAddressData Address { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public TestPersonData Superintendent { get; set; } = default!;
    public List<string> Units { get; set; } = new();
    public string NoReplyEmail { get; set; } = default!;
    public List<TestTenantData> Tenants { get; set; } = new();
}

public class TestAddressData
{
    public string StreetNumber { get; set; } = default!;
    public string StreetName { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
}

public class TestPersonData
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? MobilePhone { get; set; }
}

public class TestTenantData
{
    public TestPersonData ContactInfo { get; set; } = default!;
    public string UnitNumber { get; set; } = default!;
}

public class TestWorkerData
{
    public TestPersonData ContactInfo { get; set; } = default!;
    public string Specialization { get; set; } = default!;
}

public class CredentialEntry
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string? AdditionalInfo { get; set; }
}