using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestsForProperty;

public class GetTenantRequestsForPropertyQueryHandler : IRequestHandler<GetTenantRequestsForPropertyQuery, List<TenantRequestDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantRequestsForPropertyQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// ? Fixed: Use direct EF query since handler uses IApplicationDbContext with proper Property join
    /// </summary>
    public async Task<List<TenantRequestDto>> Handle(GetTenantRequestsForPropertyQuery request, CancellationToken cancellationToken)
    {
        // Join with Properties to get the actual PropertyName and PropertyCode
        var tenantRequests = await _context.TenantRequests
            .Where(tr => tr.PropertyId == request.PropertyId)
            .Join(_context.Properties,
                  tr => tr.PropertyId,
                  p => p.Id,
                  (tr, p) => new { TenantRequest = tr, Property = p })
            .OrderByDescending(joined => joined.TenantRequest.CreatedAt)
            .Select(joined => new TenantRequestDto
            {
                Id = joined.TenantRequest.Id,
                Code = joined.TenantRequest.Code,
                Title = joined.TenantRequest.Title,
                Description = joined.TenantRequest.Description,
                Status = joined.TenantRequest.Status.ToString(),
                UrgencyLevel = joined.TenantRequest.UrgencyLevel,
                IsEmergency = joined.TenantRequest.IsEmergency,
                CreatedDate = joined.TenantRequest.CreatedAt,
                ScheduledDate = joined.TenantRequest.ScheduledDate,
                CompletedDate = joined.TenantRequest.CompletedDate,
                TenantId = joined.TenantRequest.TenantId,
                TenantFullName = joined.TenantRequest.TenantFullName,
                TenantEmail = joined.TenantRequest.TenantEmail,
                TenantUnit = joined.TenantRequest.TenantUnit,
                PropertyId = joined.TenantRequest.PropertyId,
                // Fix: Use actual PropertyCode from joined Property entity
                PropertyCode = joined.Property.Code,
                // Fix: Use actual PropertyName from joined Property entity
                PropertyName = joined.Property.Name,
                PropertyPhone = joined.TenantRequest.PropertyPhone,
                SuperintendentFullName = joined.TenantRequest.SuperintendentFullName,
                SuperintendentEmail = joined.TenantRequest.SuperintendentEmail,
                AssignedWorkerEmail = joined.TenantRequest.AssignedWorkerEmail,
                WorkOrderNumber = joined.TenantRequest.WorkOrderNumber,
                CompletionNotes = joined.TenantRequest.CompletionNotes,
                WorkCompletedSuccessfully = joined.TenantRequest.WorkCompletedSuccessfully
            })
            .ToListAsync(cancellationToken);

        return tenantRequests;
    }

}