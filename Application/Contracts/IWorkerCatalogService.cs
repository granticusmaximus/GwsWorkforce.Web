using GwsWorkforce.Web.Models.Workforce;

namespace GwsWorkforce.Web.Application.Contracts;

public interface IWorkerCatalogService
{
    Task<IReadOnlyList<WorkerDefinition>> GetEnabledWorkersAsync(CancellationToken cancellationToken = default);
}
