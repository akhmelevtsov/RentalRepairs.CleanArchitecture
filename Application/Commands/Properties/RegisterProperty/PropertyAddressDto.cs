namespace RentalRepairs.Application.Commands.Properties.RegisterProperty;

public class PropertyAddressDto
{
    public string StreetNumber { get; set; } = default!;
    public string StreetName { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string FullAddress { get; set; } = default!;
}
