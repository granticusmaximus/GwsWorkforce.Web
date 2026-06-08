using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Application.Models;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace GwsWorkforce.Web.Infrastructure.Services;

public sealed class ConversationService(ApplicationDbContext dbContext) : IConversationService
{
    public async Task<PagedResult<Conversation>> GetUserConversationsAsync(string applicationUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, pageNumber);
        var normalizedSize = Math.Max(1, pageSize);

        var totalCount = await dbContext.Conversations
            .Where(x => x.ApplicationUserId == applicationUserId)
            .CountAsync(cancellationToken);

        var items = await dbContext.Conversations
            .Where(x => x.ApplicationUserId == applicationUserId)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Conversation>(items, normalizedPage, normalizedSize, totalCount);
    }

    public async Task<int> EnsureProjectAnchorConversationAsync(string applicationUserId, string projectName, CancellationToken cancellationToken = default)
    {
        var normalizedProjectName = projectName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProjectName))
        {
            throw new InvalidOperationException("Project name is required.");
        }

        var titlePrefix = ProjectNaming.BuildConversationTitle(normalizedProjectName, string.Empty);
        var existingId = await dbContext.Conversations
            .Where(x => x.ApplicationUserId == applicationUserId)
            .Where(x => EF.Functions.Like(x.Title, titlePrefix + "%"))
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId != 0)
        {
            return existingId;
        }

        var fallbackWorkerId = await dbContext.WorkerDefinitions
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (fallbackWorkerId == 0)
        {
            fallbackWorkerId = await dbContext.WorkerDefinitions
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (fallbackWorkerId == 0)
        {
            throw new InvalidOperationException("No workers are configured. Configure at least one worker before creating projects.");
        }

        var now = DateTime.UtcNow;
        var anchor = new Conversation
        {
            ApplicationUserId = applicationUserId,
            WorkerDefinitionId = fallbackWorkerId,
            Title = ProjectNaming.BuildConversationTitle(normalizedProjectName, "Pending"),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Conversations.Add(anchor);
        await dbContext.SaveChangesAsync(cancellationToken);

        return anchor.Id;
    }

    public async Task<IReadOnlyList<ConversationMessage>?> GetConversationMessagesAsync(string applicationUserId, int conversationId, CancellationToken cancellationToken = default)
    {
        var conversationExists = await dbContext.Conversations
            .Where(x => x.Id == conversationId && x.ApplicationUserId == applicationUserId)
            .AnyAsync(cancellationToken);

        if (!conversationExists)
        {
            return null;
        }

        return await dbContext.ConversationMessages
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RenameConversationAsync(string applicationUserId, int conversationId, string title, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .Where(x => x.Id == conversationId && x.ApplicationUserId == applicationUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        conversation.Title = title.Trim();
        conversation.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteConversationAsync(string applicationUserId, int conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .Where(x => x.Id == conversationId && x.ApplicationUserId == applicationUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        dbContext.Conversations.Remove(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
