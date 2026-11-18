
using RepairSystem.Application.Contracts.BusinessActivity;

namespace RepairSystem.Application.Interfaces.BusinessActivity;

public interface IBusinessActivityService
{
    Task<PagedBusinessActivityDto> SearchAsync(
        BusinessActivityQuery query,
        CancellationToken cancellationToken);
}
