using GwsWorkforce.Web.Application.Models;
using GwsWorkforce.Web.Models.Workforce;

namespace GwsWorkforce.Web.Application.Contracts;

public interface IConversationService
{
    Task<PagedResult<Conversation>> GetUserConversationsAsync(string applicationUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationMessage>?> GetConversationMessagesAsync(string applicationUserId, int conversationId, CancellationToken cancellationToken = default);

    Task<bool> RenameConversationAsync(string applicationUserId, int conversationId, string title, CancellationToken cancellationToken = default);

    Task<bool> DeleteConversationAsync(string applicationUserId, int conversationId, CancellationToken cancellationToken = default);
}
