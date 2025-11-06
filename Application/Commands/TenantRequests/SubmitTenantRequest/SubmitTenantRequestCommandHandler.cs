using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Exceptions;

namespace RentalRepairs.Application.Commands.TenantRequests.SubmitTenantRequest;

/// <summary>
/// Command handler for submitting tenant requests using direct EF Core access
/// Handles the Guid-based SubmitTenantRequestCommand (not legacy int-based versions)
/// </summary>
public sealed class
    SubmitTenantRequestCommandHandler : ICommandHandler<SubmitTenantRequestCommand, SubmitTenantRequestResult>
{
    private readonly IApplicationDbContext _context;

    public SubmitTenantRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SubmitTenantRequestResult> Handle(SubmitTenantRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantRequest = await _context.TenantRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TenantRequestId, cancellationToken);

        if (tenantRequest == null) throw new NotFoundException("TenantRequest", request.TenantRequestId);

        try
        {
            // Use rich domain model method
            tenantRequest.Submit();

            await _context.SaveChangesAsync(cancellationToken);

            return new SubmitTenantRequestResult
            {
                IsSuccess = true,
                RequestId = tenantRequest.Id,
                Status = tenantRequest.Status.ToString()
            };
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            return new SubmitTenantRequestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}