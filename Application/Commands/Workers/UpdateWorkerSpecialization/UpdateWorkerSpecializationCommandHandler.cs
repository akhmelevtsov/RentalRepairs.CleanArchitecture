using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Repositories;

namespace RentalRepairs.Application.Commands.Workers.UpdateWorkerSpecialization;

public class UpdateWorkerSpecializationCommandHandler : ICommandHandler<UpdateWorkerSpecializationCommand>
{
    private readonly IWorkerRepository _workerRepository;

    public UpdateWorkerSpecializationCommandHandler(IWorkerRepository workerRepository)
    {
        _workerRepository = workerRepository;
    }

    public async Task Handle(UpdateWorkerSpecializationCommand request, CancellationToken cancellationToken)
    {
        var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken);
        if (worker == null)
        {
            throw new WorkerDomainException($"Worker with ID '{request.WorkerId}' not found");
        }

        // Update specialization
        worker.SetSpecialization(request.Specialization);

        await _workerRepository.SaveChangesAsync(cancellationToken);
    }
}