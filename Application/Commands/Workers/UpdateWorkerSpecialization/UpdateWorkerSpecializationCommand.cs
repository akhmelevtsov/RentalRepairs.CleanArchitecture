using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.Workers.UpdateWorkerSpecialization;

/// <summary>
/// Command to update a worker's specialization.
/// Used in worker profile management and skills tracking workflows.
/// </summary>
public class UpdateWorkerSpecializationCommand : ICommand
{
    public Guid WorkerId { get; set; }
    public string Specialization { get; set; } = default!;
}