using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequests;

public class GetTenantRequestsQueryHandler : IRequestHandler<GetTenantRequestsQuery, List<TenantRequestDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantRequestDto>> Handle(GetTenantRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TenantRequests.AsQueryable();

        // Apply filters
        if (request.TenantId.HasValue)
        {
            query = query.Where(tr => tr.TenantId == request.TenantId.Value);
        }

        // ? Fix status filtering - convert enum to string for comparison
        if (request.Status.HasValue)
        {
            query = query.Where(tr => tr.Status == request.Status.Value);
        }

        if (request.PropertyId.HasValue)
        {
            query = query.Where(tr => tr.PropertyId == request.PropertyId.Value);
        }

        if (request.IsEmergencyOnly == true)
        {
            query = query.Where(tr => tr.IsEmergency);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDescending ? 
                query.OrderByDescending(tr => tr.Title) : 
                query.OrderBy(tr => tr.Title),
            "status" => request.SortDescending ? 
                query.OrderByDescending(tr => tr.Status) : 
                query.OrderBy(tr => tr.Status),
            "urgency" => request.SortDescending ? 
                query.OrderByDescending(tr => tr.UrgencyLevel) : 
                query.OrderBy(tr => tr.UrgencyLevel),
            _ => request.SortDescending ? 
                query.OrderByDescending(tr => tr.CreatedAt) : 
                query.OrderBy(tr => tr.CreatedAt)
        };

        // Apply pagination
        if (request.PageSize > 0)
        {
            query = query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);
        }

        // Join with Properties to get the actual PropertyCode and PropertyName
        var tenantRequests = await query
            .Join(_context.Properties, 
                  tr => tr.PropertyId, 
                  p => p.Id, 
                  (tr, p) => new { TenantRequest = tr, Property = p })
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
                // Fix: Map SubmittedDate properly - use CreatedAt for submitted requests, null for drafts
                SubmittedDate = joined.TenantRequest.Status.ToString() != "Draft" ? joined.TenantRequest.CreatedAt : (DateTime?)null,
                ScheduledDate = joined.TenantRequest.ScheduledDate,
                CompletedDate = joined.TenantRequest.CompletedDate,
                TenantId = joined.TenantRequest.TenantId,
                TenantFullName = joined.TenantRequest.TenantFullName,
                TenantEmail = joined.TenantRequest.TenantEmail,
                TenantUnit = joined.TenantRequest.TenantUnit,
                PropertyId = joined.TenantRequest.PropertyId,
                // Fix: Use actual PropertyCode from joined Property table
                PropertyCode = joined.Property.Code,
                // Fix: Use actual PropertyName from joined Property table instead of denormalized field
                PropertyName = joined.Property.Name,
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

        return tenantRequests;
    }
}
