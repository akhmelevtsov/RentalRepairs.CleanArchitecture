using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Commands.Tenants.RegisterTenant;

public class RegisterTenantCommand : ICommand<Guid>
{
    public Guid PropertyId { get; set; }
    public PersonContactInfoDto ContactInfo { get; set; } = default!;
    public string UnitNumber { get; set; } = default!;
}