using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Domain.Specifications.TenantRequests;

/// <summary>
/// Specification for finding tenant requests by assigned worker.
/// </summary>
public class TenantRequestByWorkerSpecification : BaseSpecification<TenantRequest>
{
    public TenantRequestByWorkerSpecification(string workerEmail)
        : base(tr => tr.AssignedWorkerEmail == workerEmail)
    {
    }
}
