using GwsWorkforce.Web.Application.Models;
using GwsWorkforce.Web.Models.Workforce;

namespace GwsWorkforce.Web.Application.Contracts;

public interface IKnowledgeService
{
    Task<PagedResult<UserKnowledgeItem>> GetUserKnowledgeItemsAsync(string applicationUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<UserKnowledgeItem> AddKnowledgeItemAsync(string applicationUserId, string category, string title, string content, CancellationToken cancellationToken = default);

    Task<bool> ToggleKnowledgeItemAsync(string applicationUserId, int itemId, CancellationToken cancellationToken = default);

    Task<bool> DeleteKnowledgeItemAsync(string applicationUserId, int itemId, CancellationToken cancellationToken = default);
}
