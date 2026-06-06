using GwsWorkforce.Web.Application.Models;

namespace GwsWorkforce.Web.Application.Contracts;

public interface IChatOrchestrationService
{
    Task<ChatSendResult> SendPromptAsync(
        string applicationUserId,
        int workerId,
        string prompt,
        int? selectedConversationId,
        CancellationToken cancellationToken = default);
}
