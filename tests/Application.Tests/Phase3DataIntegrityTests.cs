using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Infrastructure.Services;
using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class Phase3DataIntegrityTests
{
    [Fact]
    public async Task ConversationPaging_IsDeterministicByUpdatedOrCreatedTimestamp()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTime.UtcNow;

        dbContext.Conversations.AddRange(
            new Conversation
            {
                ApplicationUserId = "user-a",
                WorkerDefinitionId = 1,
                Title = "Old",
                CreatedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = null
            },
            new Conversation
            {
                ApplicationUserId = "user-a",
                WorkerDefinitionId = 1,
                Title = "Newest",
                CreatedAtUtc = now.AddMinutes(-5),
                UpdatedAtUtc = now.AddMinutes(1)
            },
            new Conversation
            {
                ApplicationUserId = "user-a",
                WorkerDefinitionId = 1,
                Title = "Middle",
                CreatedAtUtc = now.AddMinutes(-3),
                UpdatedAtUtc = now
            });

        await dbContext.SaveChangesAsync();

        var service = new ConversationService(dbContext);
        var page = await service.GetUserConversationsAsync("user-a", pageNumber: 1, pageSize: 3);

        Assert.Equal(3, page.Items.Count);
        Assert.Equal("Newest", page.Items[0].Title);
        Assert.Equal("Middle", page.Items[1].Title);
        Assert.Equal("Old", page.Items[2].Title);
    }

    [Fact]
    public async Task KnowledgePaging_IsDeterministicByCreatedTimestampDescending()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTime.UtcNow;

        dbContext.UserKnowledgeItems.AddRange(
            new UserKnowledgeItem
            {
                ApplicationUserId = "user-a",
                Category = "policy",
                Title = "Old",
                Content = "old",
                IsEnabled = true,
                CreatedAtUtc = now.AddMinutes(-10)
            },
            new UserKnowledgeItem
            {
                ApplicationUserId = "user-a",
                Category = "policy",
                Title = "Newest",
                Content = "new",
                IsEnabled = true,
                CreatedAtUtc = now.AddMinutes(-1)
            },
            new UserKnowledgeItem
            {
                ApplicationUserId = "user-a",
                Category = "policy",
                Title = "Middle",
                Content = "mid",
                IsEnabled = true,
                CreatedAtUtc = now.AddMinutes(-5)
            });

        await dbContext.SaveChangesAsync();

        var service = new KnowledgeService(dbContext);
        var page = await service.GetUserKnowledgeItemsAsync("user-a", pageNumber: 1, pageSize: 3);

        Assert.Equal(3, page.Items.Count);
        Assert.Equal("Newest", page.Items[0].Title);
        Assert.Equal("Middle", page.Items[1].Title);
        Assert.Equal("Old", page.Items[2].Title);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"phase3-data-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
