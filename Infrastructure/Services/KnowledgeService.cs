using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Application.Models;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace GwsWorkforce.Web.Infrastructure.Services;

public sealed class KnowledgeService(ApplicationDbContext dbContext) : IKnowledgeService
{
    public async Task<PagedResult<UserKnowledgeItem>> GetUserKnowledgeItemsAsync(string applicationUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, pageNumber);
        var normalizedSize = Math.Max(1, pageSize);

        var totalCount = await dbContext.UserKnowledgeItems
            .Where(x => x.ApplicationUserId == applicationUserId)
            .CountAsync(cancellationToken);

        var items = await dbContext.UserKnowledgeItems
            .Where(x => x.ApplicationUserId == applicationUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserKnowledgeItem>(items, normalizedPage, normalizedSize, totalCount);
    }

    public async Task<UserKnowledgeItem> AddKnowledgeItemAsync(string applicationUserId, string category, string title, string content, CancellationToken cancellationToken = default)
    {
        var item = new UserKnowledgeItem
        {
            ApplicationUserId = applicationUserId,
            Category = category.Trim(),
            Title = title.Trim(),
            Content = content.Trim(),
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.UserKnowledgeItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<bool> ToggleKnowledgeItemAsync(string applicationUserId, int itemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.UserKnowledgeItems
            .Where(x => x.Id == itemId && x.ApplicationUserId == applicationUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return false;
        }

        item.IsEnabled = !item.IsEnabled;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteKnowledgeItemAsync(string applicationUserId, int itemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.UserKnowledgeItems
            .Where(x => x.Id == itemId && x.ApplicationUserId == applicationUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return false;
        }

        dbContext.UserKnowledgeItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
