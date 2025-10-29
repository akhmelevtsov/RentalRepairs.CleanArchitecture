using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Commands.Workers.RegisterWorker;

/// <summary>
/// Command to register a new worker in the system.
/// Used in worker onboarding and management workflows.
/// </summary>
public class RegisterWorkerCommand : ICommand<Guid>
{
    public PersonContactInfoDto ContactInfo { get; set; } = default!;
    public string? Specialization { get; set; }
}