namespace RentalRepairs.Application.DTOs;

public class PersonContactInfoDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string EmailAddress { get; set; } = default!;
    public string? MobilePhone { get; set; } // Changed from PhoneNumber
    public string FullName { get; set; } = default!;
}