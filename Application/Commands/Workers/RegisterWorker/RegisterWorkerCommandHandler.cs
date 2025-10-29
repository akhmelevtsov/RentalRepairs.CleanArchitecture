using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Repositories;

namespace RentalRepairs.Application.Commands.Workers.RegisterWorker;

public class RegisterWorkerCommandHandler : ICommandHandler<RegisterWorkerCommand, Guid>
{
    private readonly IWorkerRepository _workerRepository;

    public RegisterWorkerCommandHandler(IWorkerRepository workerRepository)
    {
        _workerRepository = workerRepository;
    }

    public async Task<Guid> Handle(RegisterWorkerCommand request, CancellationToken cancellationToken)
    {
        // Check if worker with same email already exists
        var existingWorker = await _workerRepository.GetByEmailAsync(request.ContactInfo.EmailAddress, cancellationToken);
        if (existingWorker != null)
        {
            throw new WorkerDomainException($"Worker with email '{request.ContactInfo.EmailAddress}' already exists");
        }

        // Create contact info value object
        var contactInfo = new PersonContactInfo(
            request.ContactInfo.FirstName,
            request.ContactInfo.LastName,
            request.ContactInfo.EmailAddress,
            request.ContactInfo.MobilePhone);

        // Create worker entity
        var worker = new Worker(contactInfo);

        // Set specialization if provided
        if (!string.IsNullOrEmpty(request.Specialization))
        {
            worker.SetSpecialization(request.Specialization);
        }

        // Save to repository
        await _workerRepository.AddAsync(worker, cancellationToken);
        await _workerRepository.SaveChangesAsync(cancellationToken);

        return worker.Id;
    }
}
