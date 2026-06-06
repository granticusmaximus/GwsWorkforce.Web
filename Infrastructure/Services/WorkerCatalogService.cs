using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace GwsWorkforce.Web.Infrastructure.Services;

public sealed class WorkerCatalogService(ApplicationDbContext dbContext) : IWorkerCatalogService
{
    public async Task<IReadOnlyList<WorkerDefinition>> GetEnabledWorkersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkerDefinitions
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }
}
