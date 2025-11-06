using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;
using RentalRepairs.Domain.Exceptions;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Services;

namespace RentalRepairs.Application.Commands.Workers.RegisterWorker;

/// <summary>
/// Command handler for registering new workers.
/// Phase 2: Now uses SpecializationDeterminationService to parse string specializations to enum.
/// </summary>
public class RegisterWorkerCommandHandler : ICommandHandler<RegisterWorkerCommand, Guid>
{
    private readonly IWorkerRepository _workerRepository;
    private readonly SpecializationDeterminationService _specializationService;

    public RegisterWorkerCommandHandler(
        IWorkerRepository workerRepository,
        SpecializationDeterminationService specializationService)
    {
        _workerRepository = workerRepository;
        _specializationService = specializationService;
    }

    public async Task<Guid> Handle(RegisterWorkerCommand request, CancellationToken cancellationToken)
    {
        // Check if worker with same email already exists
        var existingWorker =
            await _workerRepository.GetByEmailAsync(request.ContactInfo.EmailAddress, cancellationToken);
        if (existingWorker != null)
            throw new WorkerDomainException($"Worker with email '{request.ContactInfo.EmailAddress}' already exists");

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
            var specializationEnum = _specializationService.ParseSpecialization(request.Specialization);
            worker.SetSpecialization(specializationEnum);
        }

        // Save to repository
        await _workerRepository.AddAsync(worker, cancellationToken);
        await _workerRepository.SaveChangesAsync(cancellationToken);

        return worker.Id;
    }
}