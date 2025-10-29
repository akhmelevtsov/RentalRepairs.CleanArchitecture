using MediatR;
using Microsoft.EntityFrameworkCore;
using RentalRepairs.Application.Common.Interfaces;
using RentalRepairs.Application.DTOs;

namespace RentalRepairs.Application.Queries.TenantRequests.GetTenantRequestById;

public class GetTenantRequestByIdQueryHandler : IRequestHandler<GetTenantRequestByIdQuery, TenantRequestDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTenantRequestByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantRequestDto?> Handle(GetTenantRequestByIdQuery request, CancellationToken cancellationToken)
    {
        // Join with Properties to get current property information including phone and superintendent
        var tenantRequest = await _context.TenantRequests
            .Where(tr => tr.Id == request.Id)
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
                ScheduledDate = joined.TenantRequest.ScheduledDate,
                CompletedDate = joined.TenantRequest.CompletedDate,
                TenantId = joined.TenantRequest.TenantId,
                TenantFullName = joined.TenantRequest.TenantFullName,
                TenantEmail = joined.TenantRequest.TenantEmail,
                TenantUnit = joined.TenantRequest.TenantUnit,
                PropertyId = joined.TenantRequest.PropertyId,
                // Use actual PropertyCode from joined Property entity
                PropertyCode = joined.Property.Code,
                // Use actual PropertyName from joined Property entity
                PropertyName = joined.Property.Name,
                // FIXED: Use current PropertyPhone from Property entity instead of stale denormalized data
                PropertyPhone = joined.Property.PhoneNumber ?? "Not available",
                // FIXED: Use current Superintendent info from Property entity - build full name from first + last
                SuperintendentFullName = joined.Property.Superintendent != null 
                    ? (joined.Property.Superintendent.FirstName + " " + joined.Property.Superintendent.LastName)
                    : "Not assigned",
                SuperintendentEmail = joined.Property.Superintendent != null 
                    ? joined.Property.Superintendent.EmailAddress 
                    : "Not available",
                AssignedWorkerEmail = joined.TenantRequest.AssignedWorkerEmail,
                AssignedWorkerName = joined.TenantRequest.AssignedWorkerName,
                WorkOrderNumber = joined.TenantRequest.WorkOrderNumber,
                CompletionNotes = joined.TenantRequest.CompletionNotes,
                ClosureNotes = joined.TenantRequest.ClosureNotes,
                WorkCompletedSuccessfully = joined.TenantRequest.WorkCompletedSuccessfully,
                PreferredContactTime = joined.TenantRequest.PreferredContactTime
            })
            .FirstOrDefaultAsync(cancellationToken);

        return tenantRequest;
    }
}
