using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Application.Commands.Workers.UpdateWorkerSpecialization;

/// <summary>
/// Command handler for updating worker specialization.
/// Phase 2: Now uses SpecializationDeterminationService to parse string specializations to enum.
/// </summary>
public class UpdateWorkerSpecializationCommandHandler : ICommandHandler<UpdateWorkerSpecializationCommand>
{
    private readonly IWorkerRepository _workerRepository;
    private readonly SpecializationDeterminationService _specializationService;

    public UpdateWorkerSpecializationCommandHandler(
        IWorkerRepository workerRepository,
        SpecializationDeterminationService specializationService)
    {
        _workerRepository = workerRepository;
        _specializationService = specializationService;
    }

    public async Task Handle(UpdateWorkerSpecializationCommand request, CancellationToken cancellationToken)
    {
        var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken);
        if (worker == null)
            throw new WorkerDomainException($"Worker with ID '{request.WorkerId}' not found");

        // Parse string specialization to enum and update
        var specializationEnum = _specializationService.ParseSpecialization(request.Specialization);
        worker.SetSpecialization(specializationEnum);

        await _workerRepository.SaveChangesAsync(cancellationToken);
    }
}