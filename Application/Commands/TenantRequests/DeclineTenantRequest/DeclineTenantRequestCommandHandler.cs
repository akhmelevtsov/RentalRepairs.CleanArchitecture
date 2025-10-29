using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.DeclineTenantRequest;

/// <summary>
/// Command handler for declining tenant requests using direct EF Core access
/// </summary>
public class DeclineTenantRequestCommandHandler : IRequestHandler<DeclineTenantRequestCommand>
{
    private readonly IApplicationDbContext _context;

    public DeclineTenantRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeclineTenantRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantRequest = await _context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRequestId, cancellationToken);

        if (tenantRequest == null)
        {
            throw new InvalidOperationException($"Tenant request with ID {request.TenantRequestId} not found");
        }

        // Use rich domain model method
        tenantRequest.Decline(request.Reason);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
