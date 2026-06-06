using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace GwsWorkforce.Web.Infrastructure.Services;

public sealed class WorkerCatalogService(ApplicationDbContext dbContext) : IWorkerCatalogService
{
    public async Task<IReadOnlyList<WorkerDefinition>> GetEnabledWorkersAsync(CancellationToken cancellationToken = default)
    {
        var enabledWorkers = await dbContext.WorkerDefinitions
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return enabledWorkers
            .Where(SupportsChat)
            .ToList();
    }

    private static bool SupportsChat(WorkerDefinition worker)
    {
        // Image-generation or diffusion-focused workers frequently reject /api/chat requests.
        if (worker.Key.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (worker.ModelName.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
