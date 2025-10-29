using Microsoft.EntityFrameworkCore;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantRequest> TenantRequests { get; }
    DbSet<Worker> Workers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}