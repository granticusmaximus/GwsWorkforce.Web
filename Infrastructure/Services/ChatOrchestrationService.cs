using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Application.Models;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Models.Workforce;
using GwsWorkforce.Web.Services.Ollama;
using Microsoft.EntityFrameworkCore;

namespace GwsWorkforce.Web.Infrastructure.Services;

public sealed class ChatOrchestrationService(
    ApplicationDbContext dbContext,
    OllamaChatService ollamaChatService) : IChatOrchestrationService
{
    public async Task<ChatSendResult> SendPromptAsync(
        string applicationUserId,
        int workerId,
        string prompt,
        int? selectedConversationId,
        CancellationToken cancellationToken = default)
    {
        var trimmedPrompt = prompt.Trim();

        if (string.IsNullOrWhiteSpace(trimmedPrompt))
        {
            throw new InvalidOperationException("Prompt is required.");
        }

        var worker = await dbContext.WorkerDefinitions
            .Where(x => x.Id == workerId && x.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);

        if (worker is null)
        {
            throw new InvalidOperationException("Please select a valid worker.");
        }

        var now = DateTime.UtcNow;
        var conversation = await GetOrCreateConversationAsync(applicationUserId, worker.Id, trimmedPrompt, selectedConversationId, now, cancellationToken);

        var history = await dbContext.ConversationMessages
            .Where(x => x.ConversationId == conversation.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ChatMessageInput(x.Role, x.Content))
            .ToListAsync(cancellationToken);

        var userMessage = new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = trimmedPrompt,
            CreatedAtUtc = now
        };

        dbContext.ConversationMessages.Add(userMessage);
        await dbContext.SaveChangesAsync(cancellationToken);

        var assistantResponse = await ollamaChatService.ChatAsync(
            worker.ModelName,
            worker.SystemPrompt,
            trimmedPrompt,
            history,
            cancellationToken);

        dbContext.ConversationMessages.Add(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = assistantResponse,
            CreatedAtUtc = DateTime.UtcNow
        });

        conversation.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ChatSendResult(
            conversation.Id,
            conversation.Title,
            assistantResponse);
    }

    private async Task<Conversation> GetOrCreateConversationAsync(
        string applicationUserId,
        int workerDefinitionId,
        string prompt,
        int? selectedConversationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (selectedConversationId.HasValue)
        {
            var existingConversation = await dbContext.Conversations
                .Where(x => x.Id == selectedConversationId.Value && x.ApplicationUserId == applicationUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingConversation is not null)
            {
                return existingConversation;
            }
        }

        var conversation = new Conversation
        {
            ApplicationUserId = applicationUserId,
            WorkerDefinitionId = workerDefinitionId,
            Title = CreateConversationTitle(prompt),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return conversation;
    }

    private static string CreateConversationTitle(string text)
    {
        if (text.Length <= 80)
        {
            return text;
        }

        return $"{text[..80]}...";
    }
}
