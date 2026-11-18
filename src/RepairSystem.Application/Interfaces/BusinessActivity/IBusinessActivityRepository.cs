using RepairSystem.Application.Contracts.BusinessActivity;
using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.BusinessActivity;

public interface IBusinessActivityRepository
{
    Task<(IReadOnlyList<BusinessActivityEvent> Items, int TotalCount)>
        SearchAsync(
            BusinessActivityQuery query,
            CancellationToken cancellationToken);
}