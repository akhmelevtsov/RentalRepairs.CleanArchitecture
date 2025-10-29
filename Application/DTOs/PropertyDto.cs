using RentalRepairs.Application.Commands.Properties.RegisterProperty;

namespace RentalRepairs.Application.DTOs;

public class PropertyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public PropertyAddressDto Address { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public PersonContactInfoDto Superintendent { get; set; } = default!;
    public List<string> Units { get; set; } = new();
    public string NoReplyEmailAddress { get; set; } = default!; // Changed from NotificationEmail
    public DateTime CreatedDate { get; set; }
    public List<TenantDto> Tenants { get; set; } = new();
}
