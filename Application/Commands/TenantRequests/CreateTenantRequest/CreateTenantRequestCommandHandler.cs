using MediatR;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Domain.Repositories;
using RentalRepairs.Domain.Services;
using RentalRepairs.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace RentalRepairs.Application.Commands.TenantRequests.CreateTenantRequest;

/// <summary>
/// Command handler for creating tenant requests using domain entities with configurable business rules
/// </summary>
public class CreateTenantRequestCommandHandler : IRequestHandler<CreateTenantRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantRequestSubmissionPolicy _submissionPolicy;
    private readonly ILogger<CreateTenantRequestCommandHandler> _logger;

    public CreateTenantRequestCommandHandler(
        IApplicationDbContext context,
        ITenantRepository tenantRepository,
        ITenantRequestSubmissionPolicy submissionPolicy,
        ILogger<CreateTenantRequestCommandHandler> logger)
    {
        _context = context;
        _tenantRepository = tenantRepository;
        _submissionPolicy = submissionPolicy;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateTenantRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the tenant from the repository with proper includes
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                throw new ArgumentException($"Tenant with ID {request.TenantId} not found");
            }

            // Convert urgency level string to enum
            var urgencyEnum = TenantRequestUrgencyExtensions.FromString(request.UrgencyLevel);

            // Use the tenant's domain method to create the request
            var tenantRequest = tenant.SubmitRequest(request.Title, request.Description, urgencyEnum);

            // Set the preferred contact time if provided
            if (!string.IsNullOrWhiteSpace(request.PreferredContactTime))
            {
                tenantRequest.SetPreferredContactTime(request.PreferredContactTime);
            }

            // Submit the request with configurable policy validation
            tenant.SubmitTenantRequest(tenantRequest, urgencyEnum, _submissionPolicy);

            // Save changes to the database
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created tenant request {RequestId} for tenant {TenantId}",
                tenantRequest.Id, request.TenantId);

            return tenantRequest.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant request for tenant {TenantId}", request.TenantId);
            throw;
        }
    }
}
