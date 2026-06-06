namespace GwsWorkforce.Web.Application.Models;

public sealed record ChatSendResult(
    int ConversationId,
    string ConversationTitle,
    string AssistantResponse);
