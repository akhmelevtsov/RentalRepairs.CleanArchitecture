using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.Specifications;

namespace RentalRepairs.Domain.Repositories;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByPropertyAndUnitAsync(Guid propertyId, string unitNumber,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Tenant>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> GetWithActiveRequestsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Tenant>> GetBySpecificationAsync(ISpecification<Tenant> specification,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInUnitAsync(string propertyCode, string unitNumber, CancellationToken cancellationToken = default);

    // Add method to get tenant request through tenant aggregate
    Task<TenantRequest?> GetTenantRequestByIdAsync(Guid tenantRequestId, CancellationToken cancellationToken = default);
}
