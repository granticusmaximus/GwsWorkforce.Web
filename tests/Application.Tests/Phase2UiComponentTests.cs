using System.Security.Claims;
using Bunit;
using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Application.Models;
using GwsWorkforce.Web.Components.Pages;
using GwsWorkforce.Web.Models.Workforce;
using GwsWorkforce.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests;

public class Phase2UiComponentTests : TestContext
{
    [Fact]
    public void Workforce_RendersConversationPagingMetadata()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 11));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Workforce>());

        cut.WaitForAssertion(() =>
            Assert.Contains("Page 1 of 2 (11 conversations)", cut.Markup));
    }

    [Fact]
    public void Workforce_SelectingConversation_ShowsManageControls()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 2));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Workforce>());

        var conversationSelect = cut.Find("#conversationSelect");
        conversationSelect.Change("1");

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("#renameTitle"));
            Assert.Contains("Delete", cut.Markup);
        });
    }

    [Fact]
    public void Knowledge_AddItem_ValidatesRequiredFields()
    {
        Services.AddScoped<IKnowledgeService>(_ => new FakeKnowledgeService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Knowledge>());

        cut.Find(".kn-create-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Category, title, and content are required.", cut.Markup));
    }

    [Fact]
    public void Pages_ExposeExpectedAriaLabels()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 2));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<IKnowledgeService>(_ => new FakeKnowledgeService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var workforce = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Workforce>());

        workforce.WaitForAssertion(() =>
        {
            Assert.NotNull(workforce.Find("select[aria-label='Select worker']"));
            Assert.NotNull(workforce.Find("select[aria-label='Select conversation']"));
            Assert.NotNull(workforce.Find("textarea[aria-label='Prompt input']"));
            Assert.NotNull(workforce.Find("nav[aria-label='Conversation list paging']"));
        });

        var knowledge = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Knowledge>());

        knowledge.WaitForAssertion(() =>
        {
            Assert.NotNull(knowledge.Find("input[aria-label='Knowledge category']"));
            Assert.NotNull(knowledge.Find("input[aria-label='Knowledge title']"));
            Assert.NotNull(knowledge.Find("textarea[aria-label='Knowledge content']"));
            Assert.NotNull(knowledge.Find("nav[aria-label='Knowledge list paging']"));
        });
    }

    [Fact]
    public void Workforce_KeyboardTabReachableControls_AppearInExpectedFlow()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 11));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Workforce>());

        cut.WaitForAssertion(() =>
        {
            var focusables = cut.FindAll(".wf-page select, .wf-page input, .wf-page textarea, .wf-page button");
            Assert.NotEmpty(focusables);

            var idsInOrder = focusables
                .Select(x => x.GetAttribute("id"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            Assert.True(idsInOrder.Count >= 3);
            Assert.Equal("workerSelect", idsInOrder[0]);
            Assert.Equal("conversationSelect", idsInOrder[1]);
            Assert.Equal("promptInput", idsInOrder[2]);

            Assert.Contains(focusables, x => x.TagName.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) && x.TextContent.Contains("Send", StringComparison.Ordinal));
            Assert.DoesNotContain(focusables, x => string.Equals(x.GetAttribute("tabindex"), "-1", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void Knowledge_KeyboardTabReachableControls_AppearInExpectedFlow()
    {
        Services.AddScoped<IKnowledgeService>(_ => new FakeKnowledgeService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Knowledge>());

        cut.WaitForAssertion(() =>
        {
            var focusables = cut.FindAll(".kn-page select, .kn-page input, .kn-page textarea, .kn-page button");
            Assert.NotEmpty(focusables);

            var idsInOrder = focusables
                .Select(x => x.GetAttribute("id"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            Assert.True(idsInOrder.Count >= 3);
            Assert.Equal("knowledgeCategory", idsInOrder[0]);
            Assert.Equal("knowledgeTitle", idsInOrder[1]);
            Assert.Equal("knowledgeContent", idsInOrder[2]);

            Assert.Contains(focusables, x => x.TagName.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) && x.TextContent.Contains("Add", StringComparison.Ordinal));
            Assert.DoesNotContain(focusables, x => string.Equals(x.GetAttribute("tabindex"), "-1", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void Workforce_VisualBaseline_DefaultState_MatchesSignature()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 11));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Workforce>());

        cut.WaitForAssertion(() =>
        {
            var signature = BuildWorkforceVisualSignature(cut);
            const string expected = "title:AI Worker Console|panels:Worker,Conversations,Prompt|pager:Page 1 of 2 (11 conversations)|errorCount:0|errorText:none|assistantCount:0|messageCount:0";
            Assert.Equal(expected, signature);
        });
    }

    [Fact]
    public void Workforce_VisualBaseline_ValidationState_MatchesSignature()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 11));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Workforce>());

        cut.Find(".wf-send-row button").Click();

        cut.WaitForAssertion(() =>
        {
            var signature = BuildWorkforceVisualSignature(cut);
            const string expected = "title:AI Worker Console|panels:Worker,Conversations,Prompt|pager:Page 1 of 2 (11 conversations)|errorCount:1|errorText:Prompt is required.|assistantCount:0|messageCount:0";
            Assert.Equal(expected, signature);
        });
    }

    [Fact]
    public void Knowledge_VisualBaseline_DefaultState_MatchesSignature()
    {
        Services.AddScoped<IKnowledgeService>(_ => new FakeKnowledgeService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Knowledge>());

        cut.WaitForAssertion(() =>
        {
            var signature = BuildKnowledgeVisualSignature(cut);
            const string expected = "title:Knowledge Vault|panels:Add Knowledge Item,Your Knowledge Items|pager:Page 1 of 1 (0 total items)|errorCount:0|errorText:none|successCount:0|emptyCount:1";
            Assert.Equal(expected, signature);
        });
    }

    [Fact]
    public void Knowledge_VisualBaseline_ValidationState_MatchesSignature()
    {
        Services.AddScoped<IKnowledgeService>(_ => new FakeKnowledgeService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Knowledge>());

        cut.Find(".kn-create-btn").Click();

        cut.WaitForAssertion(() =>
        {
            var signature = BuildKnowledgeVisualSignature(cut);
            const string expected = "title:Knowledge Vault|panels:Add Knowledge Item,Your Knowledge Items|pager:Page 1 of 1 (0 total items)|errorCount:1|errorText:Category, title, and content are required.|successCount:0|emptyCount:1";
            Assert.Equal(expected, signature);
        });
    }

    [Fact]
    public void Workforce_SendPrompt_ShowsVerificationReportPanel()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 2));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Workforce>());

        cut.Find("#promptInput").Change("Summarize this.");
        cut.Find(".wf-send-row button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Verification Report", cut.Markup);
            var verifierPanel = cut.Find(".wf-verifier-panel");
            Assert.Contains("is-pass", verifierPanel.ClassList);
            var verifierPill = cut.Find(".wf-verifier-pill");
            Assert.Contains("PASS", verifierPill.TextContent, StringComparison.Ordinal);
            Assert.Contains("is-pass", verifierPill.ClassList);
            Assert.Equal("+", cut.Find(".wf-verifier-icon").TextContent.Trim());
            Assert.Contains("Verifier accepted response.", cut.Markup);
        });
    }

    [Fact]
    public void Projects_RendersForAuthenticatedUser()
    {
        Services.AddScoped<IWorkerCatalogService>(_ => new FakeWorkerCatalogService());
        Services.AddScoped<IConversationService>(_ => new FakeConversationService(totalConversations: 2));
        Services.AddScoped<IChatOrchestrationService>(_ => new FakeChatOrchestrationService());
        Services.AddScoped<ProjectDraftStore>(_ => new ProjectDraftStore());
        Services.AddScoped<AuthenticationStateProvider>(_ => BuildAuthProvider("user-a"));

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Projects>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Projects", cut.Markup);
            Assert.NotNull(cut.Find(".pj-empty-start"));
            Assert.Contains("Start a New Project", cut.Markup);
        });
    }

    private static string BuildWorkforceVisualSignature(IRenderedFragment cut)
    {
        var title = cut.Find(".wf-header h1").TextContent.Trim();
        var panels = string.Join(",", cut.FindAll(".wf-panel h2").Select(x => x.TextContent.Trim()));
        var pager = cut.Find(".wf-meta").TextContent.Trim();
        var errorCount = cut.FindAll(".status-banner-error").Count;
        var errorText = errorCount > 0
            ? cut.Find(".status-banner-error").TextContent.Trim()
            : "none";
        var assistantCount = cut.FindAll("section[aria-live='polite']").Count;
        var messageCount = cut.FindAll(".wf-msg").Count;

        return string.Join('|',
        [
            $"title:{title}",
            $"panels:{panels}",
            $"pager:{pager}",
            $"errorCount:{errorCount}",
            $"errorText:{errorText}",
            $"assistantCount:{assistantCount}",
            $"messageCount:{messageCount}"
        ]);
    }

    private static string BuildKnowledgeVisualSignature(IRenderedFragment cut)
    {
        var title = cut.Find(".kn-header h1").TextContent.Trim();
        var panels = string.Join(",", cut.FindAll(".kn-panel h2").Select(x => x.TextContent.Trim()));
        var pager = cut.Find(".kn-meta").TextContent.Trim();
        var errorCount = cut.FindAll(".status-banner-error").Count;
        var errorText = errorCount > 0
            ? cut.Find(".status-banner-error").TextContent.Trim()
            : "none";
        var successCount = cut.FindAll(".status-banner-success").Count;
        var emptyCount = cut.FindAll(".kn-empty").Count;

        return string.Join('|',
        [
            $"title:{title}",
            $"panels:{panels}",
            $"pager:{pager}",
            $"errorCount:{errorCount}",
            $"errorText:{errorText}",
            $"successCount:{successCount}",
            $"emptyCount:{emptyCount}"
        ]);
    }

    private static AuthenticationStateProvider BuildAuthProvider(string userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId)
        ],
        "TestAuth");

        var principal = new ClaimsPrincipal(identity);
        var state = new AuthenticationState(principal);
        return new TestAuthenticationStateProvider(state);
    }

    private sealed class TestAuthenticationStateProvider(AuthenticationState state) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state);
    }

    private sealed class FakeWorkerCatalogService : IWorkerCatalogService
    {
        public Task<IReadOnlyList<WorkerDefinition>> GetEnabledWorkersAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WorkerDefinition> workers =
            [
                new WorkerDefinition
                {
                    Id = 1,
                    Key = "general-assistant",
                    DisplayName = "General Assistant",
                    SystemPrompt = "You are helpful.",
                    ModelName = "llama3.2:latest",
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                }
            ];

            return Task.FromResult(workers);
        }
    }

    private sealed class FakeConversationService(int totalConversations) : IConversationService
    {
        public Task<PagedResult<Conversation>> GetUserConversationsAsync(string applicationUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Conversation> items =
            [
                new Conversation
                {
                    Id = 1,
                    ApplicationUserId = applicationUserId,
                    WorkerDefinitionId = 1,
                    Title = "First Conversation",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new Conversation
                {
                    Id = 2,
                    ApplicationUserId = applicationUserId,
                    WorkerDefinitionId = 1,
                    Title = "Second Conversation",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            ];

            return Task.FromResult(new PagedResult<Conversation>(items, pageNumber, pageSize, totalConversations));
        }

        public Task<int> EnsureProjectAnchorConversationAsync(string applicationUserId, string projectName, CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<IReadOnlyList<ConversationMessage>?> GetConversationMessagesAsync(string applicationUserId, int conversationId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ConversationMessage> items =
            [
                new ConversationMessage
                {
                    Id = 1,
                    ConversationId = conversationId,
                    Role = "user",
                    Content = "Hello",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ConversationMessage
                {
                    Id = 2,
                    ConversationId = conversationId,
                    Role = "assistant",
                    Content = "Hi there",
                    CreatedAtUtc = DateTime.UtcNow
                }
            ];

            return Task.FromResult<IReadOnlyList<ConversationMessage>?>(items);
        }

        public Task<bool> RenameConversationAsync(string applicationUserId, int conversationId, string title, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> DeleteConversationAsync(string applicationUserId, int conversationId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class FakeKnowledgeService : IKnowledgeService
    {
        public Task<PagedResult<UserKnowledgeItem>> GetUserKnowledgeItemsAsync(string applicationUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var result = new PagedResult<UserKnowledgeItem>([], pageNumber, pageSize, 0);
            return Task.FromResult(result);
        }

        public Task<UserKnowledgeItem> AddKnowledgeItemAsync(string applicationUserId, string category, string title, string content, CancellationToken cancellationToken = default)
        {
            var item = new UserKnowledgeItem
            {
                Id = 1,
                ApplicationUserId = applicationUserId,
                Category = category,
                Title = title,
                Content = content,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            return Task.FromResult(item);
        }

        public Task<bool> ToggleKnowledgeItemAsync(string applicationUserId, int itemId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> DeleteKnowledgeItemAsync(string applicationUserId, int itemId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class FakeChatOrchestrationService : IChatOrchestrationService
    {
        public Task<ChatSendResult> SendPromptAsync(string applicationUserId, int workerId, string prompt, int? selectedConversationId, CancellationToken cancellationToken = default)
        {
            var result = new ChatSendResult(
                1,
                "First Conversation",
                "Acknowledged",
                "PASS",
                "Verifier accepted response.",
                []);
            return Task.FromResult(result);
        }
    }
}
