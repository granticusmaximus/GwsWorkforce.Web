using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Application.Models;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Models.Workforce;
using GwsWorkforce.Web.Services.Ollama;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace GwsWorkforce.Web.Infrastructure.Services;

public sealed class ChatOrchestrationService(
    ApplicationDbContext dbContext,
    OllamaChatService ollamaChatService,
    IConfiguration configuration) : IChatOrchestrationService
{
    private const string ResponseQualityGovernanceInstruction =
        "Response Governance Policy:\n" +
        "1. Do not fabricate facts, citations, links, code behavior, or external events.\n" +
        "2. If evidence is insufficient or uncertain, explicitly say what is unknown.\n" +
        "3. Separate facts, assumptions, and recommendations in your response.\n" +
        "4. Prefer concise, verifiable statements over speculation or filler wording.\n" +
        "5. Include a confidence rating (High/Medium/Low) and a short verification checklist.\n" +
        "6. If the request is ambiguous, ask clarifying questions before asserting conclusions.";

    private const string VerifierSystemPrompt =
        "You are a strict response verifier. Evaluate the candidate assistant response against the governance rubric. " +
        "Return ONLY JSON with this schema: {\"decision\":\"PASS|FAIL\",\"summary\":\"short text\",\"issues\":[\"issue\"]}. " +
        "Mark FAIL if any fabrication risk, unsupported claims, missing uncertainty signaling, excessive filler, or weak verification guidance exists.";

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
            BuildGovernedSystemPrompt(worker.SystemPrompt),
            trimmedPrompt,
            history,
            cancellationToken);

        var verifierResult = await VerifyWithSecondStageModelAsync(
            worker.ModelName,
            trimmedPrompt,
            assistantResponse,
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
            assistantResponse,
            verifierResult.Decision,
            verifierResult.Summary,
            verifierResult.Issues);
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
        if (TryExtractProjectNameFromPrompt(text, out var projectName))
        {
            return ProjectNaming.BuildConversationTitle(projectName!, "Pending");
        }

        if (text.Length <= 80)
        {
            return text;
        }

        return $"{text[..80]}...";
    }

    private static bool TryExtractProjectNameFromPrompt(string text, out string? projectName)
    {
        projectName = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return false;
        }

        const string prefix = "Project:";
        var first = lines[0];
        if (!first.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extracted = first[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(extracted))
        {
            return false;
        }

        projectName = extracted;
        return true;
    }

    private static string BuildGovernedSystemPrompt(string systemPrompt)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            return ResponseQualityGovernanceInstruction;
        }

        return $"{systemPrompt.Trim()}\n\n{ResponseQualityGovernanceInstruction}";
    }

    private async Task<VerifierDecision> VerifyWithSecondStageModelAsync(
        string primaryModelName,
        string userPrompt,
        string assistantResponse,
        CancellationToken cancellationToken)
    {
        var verifierModel = configuration["Ollama:VerifierModel"];
        if (string.IsNullOrWhiteSpace(verifierModel))
        {
            return new VerifierDecision(true, "PASS", "Verification skipped: no verifier model configured.", []);
        }

        if (string.Equals(verifierModel, primaryModelName, StringComparison.OrdinalIgnoreCase))
        {
            return new VerifierDecision(true, "PASS", "Verification skipped: verifier model matches primary model.", []);
        }

        var verifierPrompt =
            "User request:\n" + userPrompt + "\n\n" +
            "Candidate assistant response:\n" + assistantResponse + "\n\n" +
            "Evaluate against governance rubric and return JSON only.";

        var verifierRaw = await ollamaChatService.ChatAsync(
            verifierModel,
            VerifierSystemPrompt,
            verifierPrompt,
            history: null,
            cancellationToken);

        return ParseVerifierDecision(verifierRaw);
    }

    private static VerifierDecision ParseVerifierDecision(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var decision = root.TryGetProperty("decision", out var decisionProp)
                ? decisionProp.GetString()
                : null;

            var summary = root.TryGetProperty("summary", out var summaryProp)
                ? summaryProp.GetString()
                : null;

            var normalized = decision?.Trim().ToUpperInvariant();
            if (normalized == "PASS")
            {
                return new VerifierDecision(true, "PASS", summary ?? "Verifier PASS.", []);
            }

            if (normalized == "FAIL")
            {
                var issues = root.TryGetProperty("issues", out var issuesProp) && issuesProp.ValueKind == JsonValueKind.Array
                    ? issuesProp.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList()
                    : [];

                var issueText = string.Join("; ", issues);

                var merged = string.IsNullOrWhiteSpace(issueText)
                    ? (summary ?? "Verifier returned FAIL.")
                    : $"{summary ?? "Verifier returned FAIL."} Issues: {issueText}";

                return new VerifierDecision(false, "FAIL", merged, issues);
            }
        }
        catch (JsonException)
        {
            // If verifier does not return parseable JSON, treat as failure and block output.
        }

        return new VerifierDecision(false, "FAIL", "Verifier did not return valid PASS/FAIL JSON output.", []);
    }

    private sealed record VerifierDecision(bool IsPass, string Decision, string Summary, IReadOnlyList<string> Issues);
}
