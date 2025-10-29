using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.CloseRequest;

/// <summary>
/// Command handler for closing tenant requests using direct EF Core access
/// </summary>
public class CloseRequestCommandHandler : IRequestHandler<CloseRequestCommand>
{
    private readonly IApplicationDbContext _context;

    public CloseRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(CloseRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantRequest = await _context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRequestId, cancellationToken);

        if (tenantRequest == null)
        {
            throw new InvalidOperationException($"Tenant request with ID {request.TenantRequestId} not found");
        }

        // Use rich domain model method
        tenantRequest.Close(request.ClosureNotes);

        await _context.SaveChangesAsync(cancellationToken);
    }
}