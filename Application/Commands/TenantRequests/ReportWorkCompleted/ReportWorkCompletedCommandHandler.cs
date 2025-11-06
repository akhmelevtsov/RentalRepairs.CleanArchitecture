using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.ReportWorkCompleted;

/// <summary>
/// Command handler for completing work using direct EF Core access
/// </summary>
public class ReportWorkCompletedCommandHandler : IRequestHandler<ReportWorkCompletedCommand>
{
    private readonly IApplicationDbContext _context;

    public ReportWorkCompletedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReportWorkCompletedCommand request, CancellationToken cancellationToken)
    {
        var tenantRequest = await _context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRequestId, cancellationToken);

        if (tenantRequest == null)
            throw new InvalidOperationException($"Tenant request with ID {request.TenantRequestId} not found");

        // Update the worker assignment
        if (!string.IsNullOrEmpty(tenantRequest.AssignedWorkerEmail) &&
            !string.IsNullOrEmpty(tenantRequest.WorkOrderNumber))
        {
            var worker = await _context.Workers
                .Include(w => w.Assignments)
                .FirstOrDefaultAsync(w => w.ContactInfo.EmailAddress == tenantRequest.AssignedWorkerEmail,
                    cancellationToken);

            if (worker != null)
                worker.CompleteWork(tenantRequest.WorkOrderNumber, request.CompletedSuccessfully,
                    request.CompletionNotes);
        }

        // Update the tenant request
        tenantRequest.ReportWorkCompleted(request.CompletedSuccessfully, request.CompletionNotes ?? string.Empty);

        await _context.SaveChangesAsync(cancellationToken);
    }
}