using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Tests.TestData;

/// <summary>
/// Test data builder for creating Property entities with fluent interface
/// </summary>
public class PropertyTestDataBuilder
{
    private string _name = "Test Property";
    private string _code = "TP001";
    private PropertyAddress _address = new("123", "Main St", "Test City", "12345");
    private string _phoneNumber = "+1-555-1234";
    private PersonContactInfo _superintendent = new("John", "Doe", "john@test.com");
    private List<string> _units = new() { "101", "102", "103" };
    private string _noReplyEmail = "noreply@testproperty.com";

    public PropertyTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PropertyTestDataBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public PropertyTestDataBuilder WithAddress(PropertyAddress address)
    {
        _address = address;
        return this;
    }

    public PropertyTestDataBuilder WithAddress(string streetNumber, string streetName, string city, string postalCode)
    {
        _address = new PropertyAddress(streetNumber, streetName, city, postalCode);
        return this;
    }

    public PropertyTestDataBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public PropertyTestDataBuilder WithSuperintendent(PersonContactInfo superintendent)
    {
        _superintendent = superintendent;
        return this;
    }

    public PropertyTestDataBuilder WithSuperintendent(string firstName, string lastName, string email, string? phone = null)
    {
        _superintendent = new PersonContactInfo(firstName, lastName, email, phone);
        return this;
    }

    public PropertyTestDataBuilder WithUnits(params string[] units)
    {
        _units = units.ToList();
        return this;
    }

    public PropertyTestDataBuilder WithNoReplyEmail(string noReplyEmail)
    {
        _noReplyEmail = noReplyEmail;
        return this;
    }

    public Property Build()
    {
        return new Property(_name, _code, _address, _phoneNumber, _superintendent, _units, _noReplyEmail);
    }

    public static PropertyTestDataBuilder Default() => new PropertyTestDataBuilder();
    
    public static PropertyTestDataBuilder ForApartmentComplex() => new PropertyTestDataBuilder()
        .WithName("Sunset Apartments")
        .WithCode("SA001")
        .WithAddress("456", "Oak Avenue", "Downtown", "98765")
        .WithUnits("101", "102", "103", "201", "202", "203");

    public static PropertyTestDataBuilder ForSingleFamily() => new PropertyTestDataBuilder()
        .WithName("Family Home")
        .WithCode("FH001")
        .WithAddress("789", "Pine Street", "Suburbia", "54321")
        .WithUnits("Main");
}