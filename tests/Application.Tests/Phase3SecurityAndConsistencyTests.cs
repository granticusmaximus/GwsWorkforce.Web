using System.Net;
using System.Text;
using System.Text.Json;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Infrastructure.Services;
using GwsWorkforce.Web.Models.Workforce;
using GwsWorkforce.Web.Services.Ollama;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Tests;

public class Phase3SecurityAndConsistencyTests
{
    [Fact]
    public async Task QueryPath_Conversations_IsIsolatedPerUser()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Conversations.AddRange(
            new Conversation
            {
                ApplicationUserId = "user-a",
                WorkerDefinitionId = 1,
                Title = "A1",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new Conversation
            {
                ApplicationUserId = "user-b",
                WorkerDefinitionId = 1,
                Title = "B1",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var service = new ConversationService(dbContext);
        var userA = await service.GetUserConversationsAsync("user-a", pageNumber: 1, pageSize: 50);
        var userB = await service.GetUserConversationsAsync("user-b", pageNumber: 1, pageSize: 50);

        Assert.Single(userA.Items);
        Assert.Single(userB.Items);
        Assert.All(userA.Items, x => Assert.Equal("user-a", x.ApplicationUserId));
        Assert.All(userB.Items, x => Assert.Equal("user-b", x.ApplicationUserId));
    }

    [Fact]
    public async Task QueryPath_ConversationMessages_IsIsolatedPerUser()
    {
        await using var dbContext = CreateDbContext();

        var conversation = new Conversation
        {
            ApplicationUserId = "user-a",
            WorkerDefinitionId = 1,
            Title = "A",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        dbContext.ConversationMessages.AddRange(
            new ConversationMessage
            {
                ConversationId = conversation.Id,
                Role = "user",
                Content = "hello",
                CreatedAtUtc = DateTime.UtcNow
            },
            new ConversationMessage
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = "world",
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(1)
            });

        await dbContext.SaveChangesAsync();

        var service = new ConversationService(dbContext);

        var ownerMessages = await service.GetConversationMessagesAsync("user-a", conversation.Id);
        var otherUserMessages = await service.GetConversationMessagesAsync("user-b", conversation.Id);

        Assert.NotNull(ownerMessages);
        Assert.Equal(2, ownerMessages!.Count);
        Assert.Null(otherUserMessages);
    }

    [Fact]
    public async Task QueryPath_Knowledge_IsIsolatedPerUser()
    {
        await using var dbContext = CreateDbContext();

        dbContext.UserKnowledgeItems.AddRange(
            new UserKnowledgeItem
            {
                ApplicationUserId = "user-a",
                Category = "policy",
                Title = "A1",
                Content = "a",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new UserKnowledgeItem
            {
                ApplicationUserId = "user-b",
                Category = "policy",
                Title = "B1",
                Content = "b",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var service = new KnowledgeService(dbContext);

        var userA = await service.GetUserKnowledgeItemsAsync("user-a", pageNumber: 1, pageSize: 50);
        var userB = await service.GetUserKnowledgeItemsAsync("user-b", pageNumber: 1, pageSize: 50);

        Assert.Single(userA.Items);
        Assert.Single(userB.Items);
        Assert.All(userA.Items, x => Assert.Equal("user-a", x.ApplicationUserId));
        Assert.All(userB.Items, x => Assert.Equal("user-b", x.ApplicationUserId));
    }

    [Fact]
    public async Task Concurrency_RenameRace_CompletesWithConsistentFinalTitle()
    {
        var dbName = $"rename-race-{Guid.NewGuid()}";

        await using (var setupContext = CreateDbContext(dbName))
        {
            setupContext.Conversations.Add(new Conversation
            {
                ApplicationUserId = "user-a",
                WorkerDefinitionId = 1,
                Title = "Initial",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            await setupContext.SaveChangesAsync();
        }

        await using var context1 = CreateDbContext(dbName);
        await using var context2 = CreateDbContext(dbName);

        var service1 = new ConversationService(context1);
        var service2 = new ConversationService(context2);

        var conversationId = await context1.Conversations.Select(x => x.Id).SingleAsync();

        var rename1Task = service1.RenameConversationAsync("user-a", conversationId, "Title A");
        var rename2Task = service2.RenameConversationAsync("user-a", conversationId, "Title B");

        var renameResults = await Task.WhenAll(rename1Task, rename2Task);

        Assert.True(renameResults[0]);
        Assert.True(renameResults[1]);

        await using var assertContext = CreateDbContext(dbName);
        var finalTitle = await assertContext.Conversations
            .Where(x => x.Id == conversationId)
            .Select(x => x.Title)
            .SingleAsync();

        Assert.Contains(finalTitle, new[] { "Title A", "Title B" });
    }

    [Fact]
    public async Task Concurrency_UpdateOwnershipRace_OnlyOwnerWriteSucceeds()
    {
        var dbName = $"ownership-race-{Guid.NewGuid()}";

        await using (var setupContext = CreateDbContext(dbName))
        {
            setupContext.Conversations.Add(new Conversation
            {
                ApplicationUserId = "owner",
                WorkerDefinitionId = 1,
                Title = "Initial",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            await setupContext.SaveChangesAsync();
        }

        await using var ownerContext = CreateDbContext(dbName);
        await using var attackerContext = CreateDbContext(dbName);

        var ownerService = new ConversationService(ownerContext);
        var attackerService = new ConversationService(attackerContext);

        var conversationId = await ownerContext.Conversations.Select(x => x.Id).SingleAsync();

        var ownerRenameTask = ownerService.RenameConversationAsync("owner", conversationId, "Owner Update");
        var attackerRenameTask = attackerService.RenameConversationAsync("attacker", conversationId, "Attack Update");

        var renameResults = await Task.WhenAll(ownerRenameTask, attackerRenameTask);

        Assert.True(renameResults[0]);
        Assert.False(renameResults[1]);

        await using var assertContext = CreateDbContext(dbName);
        var final = await assertContext.Conversations.SingleAsync(x => x.Id == conversationId);
        Assert.Equal("Owner Update", final.Title);
    }

    [Fact]
    public async Task ChatFlow_SimulatedAssistantFailure_PersistsUserMessageWithoutAssistantMessage()
    {
        var dbName = $"chat-failure-{Guid.NewGuid()}";
        await using var dbContext = CreateDbContext(dbName);

        var worker = new WorkerDefinition
        {
            Key = "general",
            DisplayName = "General",
            ModelName = "test-model",
            SystemPrompt = "Be helpful",
            Temperature = 0.7,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.WorkerDefinitions.Add(worker);
        await dbContext.SaveChangesAsync();

        var failingHandler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("simulated assistant failure", Encoding.UTF8, "text/plain")
            }));

        var httpClient = new HttpClient(failingHandler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var ollamaService = new OllamaChatService(
            httpClient,
            BuildConfiguration(new Dictionary<string, string?>()));
        var orchestrationService = new ChatOrchestrationService(
            dbContext,
            ollamaService,
            BuildConfiguration(new Dictionary<string, string?>
            {
                ["Ollama:VerifierModel"] = "verifier-model"
            }));

        await Assert.ThrowsAsync<OllamaChatException>(() =>
            orchestrationService.SendPromptAsync("user-a", worker.Id, "Hello", selectedConversationId: null));

        var conversation = await dbContext.Conversations.SingleAsync(x => x.ApplicationUserId == "user-a");
        var messages = await dbContext.ConversationMessages
            .Where(x => x.ConversationId == conversation.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        Assert.Single(messages);
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("Hello", messages[0].Content);
        Assert.DoesNotContain(messages, x => x.Role == "assistant");
    }

    [Fact]
    public async Task ChatFlow_IncludesGovernanceInstructionInSystemPrompt()
    {
        var dbName = $"chat-governance-{Guid.NewGuid()}";
        await using var dbContext = CreateDbContext(dbName);

        var worker = new WorkerDefinition
        {
            Key = "research",
            DisplayName = "Research",
            ModelName = "test-model",
            SystemPrompt = "You are a careful research assistant.",
            Temperature = 0.2,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.WorkerDefinitions.Add(worker);
        await dbContext.SaveChangesAsync();

        var requestPayloads = new List<string>();
        var callCount = 0;

        var successHandler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            var payload = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(payload))
            {
                requestPayloads.Add(payload);
            }

            callCount++;

            if (callCount == 1)
            {
                const string primaryResponse = "{\"message\":{\"role\":\"assistant\",\"content\":\"Primary candidate answer\"}}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(primaryResponse, Encoding.UTF8, "application/json")
                };
            }

            const string verifierPass = "{\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"decision\\\":\\\"PASS\\\",\\\"summary\\\":\\\"Looks grounded\\\",\\\"issues\\\":[]}\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(verifierPass, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(successHandler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var ollamaService = new OllamaChatService(
            httpClient,
            BuildConfiguration(new Dictionary<string, string?>()));
        var orchestrationService = new ChatOrchestrationService(
            dbContext,
            ollamaService,
            BuildConfiguration(new Dictionary<string, string?>
            {
                ["Ollama:VerifierModel"] = "verifier-model"
            }));

        var result = await orchestrationService.SendPromptAsync("user-a", worker.Id, "Check this", selectedConversationId: null);

        Assert.Equal("Primary candidate answer", result.AssistantResponse);
        Assert.Equal("PASS", result.VerifierDecision);
        Assert.Contains("Looks grounded", result.VerifierSummary, StringComparison.Ordinal);
        Assert.Empty(result.VerifierIssues);

        Assert.Equal(2, requestPayloads.Count);

        using (var firstCallJson = JsonDocument.Parse(requestPayloads[0]))
        {
            var firstMessages = firstCallJson.RootElement.GetProperty("messages");
            var firstMessage = firstMessages[0];
            var role = firstMessage.GetProperty("role").GetString();
            var content = firstMessage.GetProperty("content").GetString();

            Assert.Equal("system", role);
            Assert.NotNull(content);
            Assert.Contains("Response Governance Policy", content, StringComparison.Ordinal);
            Assert.Contains("Do not fabricate facts", content, StringComparison.Ordinal);
            Assert.Contains("confidence rating", content, StringComparison.OrdinalIgnoreCase);
        }

        using (var secondCallJson = JsonDocument.Parse(requestPayloads[1]))
        {
            var secondModel = secondCallJson.RootElement.GetProperty("model").GetString();
            Assert.Equal("verifier-model", secondModel);
        }

    }

    [Fact]
    public async Task ChatFlow_VerifierRejects_ReturnsFailDecisionAndPersistsAssistantMessage()
    {
        var dbName = $"chat-verifier-reject-{Guid.NewGuid()}";
        await using var dbContext = CreateDbContext(dbName);

        var worker = new WorkerDefinition
        {
            Key = "governed",
            DisplayName = "Governed",
            ModelName = "primary-model",
            SystemPrompt = "You are a careful assistant.",
            Temperature = 0.2,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.WorkerDefinitions.Add(worker);
        await dbContext.SaveChangesAsync();

        var callCount = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                const string primaryResponse = "{\"message\":{\"role\":\"assistant\",\"content\":\"Primary candidate answer\"}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(primaryResponse, Encoding.UTF8, "application/json")
                });
            }

            const string verifierFail = "{\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"decision\\\":\\\"FAIL\\\",\\\"summary\\\":\\\"Unsupported claim\\\",\\\"issues\\\":[\\\"Missing uncertainty statement\\\"]}\"}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(verifierFail, Encoding.UTF8, "application/json")
            });
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var ollamaService = new OllamaChatService(
            httpClient,
            BuildConfiguration(new Dictionary<string, string?>()));
        var orchestrationService = new ChatOrchestrationService(
            dbContext,
            ollamaService,
            BuildConfiguration(new Dictionary<string, string?>
            {
                ["Ollama:VerifierModel"] = "verifier-model"
            }));

        var result = await orchestrationService.SendPromptAsync("user-a", worker.Id, "Please answer", selectedConversationId: null);

        Assert.Equal("FAIL", result.VerifierDecision);
        Assert.Contains("Unsupported claim", result.VerifierSummary, StringComparison.Ordinal);
        Assert.Contains("Missing uncertainty statement", result.VerifierIssues);
        Assert.Equal("Primary candidate answer", result.AssistantResponse);

        var conversation = await dbContext.Conversations.SingleAsync(x => x.ApplicationUserId == "user-a");
        var messages = await dbContext.ConversationMessages
            .Where(x => x.ConversationId == conversation.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        Assert.Equal(2, messages.Count);
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("assistant", messages[1].Role);
        Assert.Equal("Primary candidate answer", messages[1].Content);
    }

    [Fact]
    public async Task NonChatCapableModel_BadRequest_MapsToActionableErrorMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("model does not support chat")
        }));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var service = new OllamaChatService(
            client,
            BuildConfiguration(new Dictionary<string, string?>()));

        var ex = await Assert.ThrowsAsync<OllamaChatException>(() =>
            service.ChatAsync("x/z-image-turbo:latest", "sys", "user", null, CancellationToken.None));

        Assert.Contains("may not support chat completions", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? $"phase3-security-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request, cancellationToken);
    }
}
