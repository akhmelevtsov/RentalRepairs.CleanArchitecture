using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.Common.Models;
using RentalRepairs.Application.DTOs;
using RentalRepairs.Application.Queries.Workers.GetWorkerRequests;

namespace RentalRepairs.Application.Queries.Workers.Handlers;

/// <summary>
/// ? Query handler to get requests assigned to a specific worker
/// </summary>
public class GetWorkerRequestsQueryHandler : IQueryHandler<GetWorkerRequestsQuery, PagedResult<TenantRequestDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkerRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TenantRequestDto>> Handle(GetWorkerRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TenantRequests
            .Where(tr => tr.AssignedWorkerEmail == request.WorkerEmail);

        var totalCount = await query.CountAsync(cancellationToken);

        // Join with Properties to get the actual PropertyName and PropertyCode
        var items = await query
            .Join(_context.Properties,
                  tr => tr.PropertyId,
                  p => p.Id,
                  (tr, p) => new { TenantRequest = tr, Property = p })
            .OrderByDescending(joined => joined.TenantRequest.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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
                // Fix: Use actual PropertyName from joined Property entity
                PropertyName = joined.Property.Name,
                // Fix: Use actual PropertyCode from joined Property entity
                PropertyCode = joined.Property.Code,
                PropertyPhone = joined.TenantRequest.PropertyPhone,
                SuperintendentFullName = joined.TenantRequest.SuperintendentFullName,
                SuperintendentEmail = joined.TenantRequest.SuperintendentEmail,
                AssignedWorkerEmail = joined.TenantRequest.AssignedWorkerEmail,
                AssignedWorkerName = joined.TenantRequest.AssignedWorkerName,
                WorkOrderNumber = joined.TenantRequest.WorkOrderNumber,
                CompletionNotes = joined.TenantRequest.CompletionNotes,
                WorkCompletedSuccessfully = joined.TenantRequest.WorkCompletedSuccessfully
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}