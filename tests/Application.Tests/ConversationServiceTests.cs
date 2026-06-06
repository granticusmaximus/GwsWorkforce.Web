using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Infrastructure.Services;
using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class ConversationServiceTests
{
    [Fact]
    public async Task GetUserConversationsAsync_ReturnsOnlyCurrentUserRecords()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Conversations.AddRange(
            new Conversation { ApplicationUserId = "user-a", WorkerDefinitionId = 1, Title = "A1", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
            new Conversation { ApplicationUserId = "user-a", WorkerDefinitionId = 1, Title = "A2", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1), UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-1) },
            new Conversation { ApplicationUserId = "user-b", WorkerDefinitionId = 1, Title = "B1", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });

        await dbContext.SaveChangesAsync();

        var service = new ConversationService(dbContext);
        var result = await service.GetUserConversationsAsync("user-a", 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Equal("user-a", x.ApplicationUserId));
    }

    [Fact]
    public async Task GetConversationMessagesAsync_ReturnsNullForDifferentOwner()
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

        dbContext.ConversationMessages.Add(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = "hello",
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var service = new ConversationService(dbContext);
        var result = await service.GetConversationMessagesAsync("user-b", conversation.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenameAndDelete_RespectOwnership()
    {
        await using var dbContext = CreateDbContext();

        var ownedConversation = new Conversation
        {
            ApplicationUserId = "user-a",
            WorkerDefinitionId = 1,
            Title = "Original",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Conversations.Add(ownedConversation);
        await dbContext.SaveChangesAsync();

        var service = new ConversationService(dbContext);

        var renameByOtherUser = await service.RenameConversationAsync("user-b", ownedConversation.Id, "Nope");
        var renameByOwner = await service.RenameConversationAsync("user-a", ownedConversation.Id, "Renamed");
        var deleteByOtherUser = await service.DeleteConversationAsync("user-b", ownedConversation.Id);
        var deleteByOwner = await service.DeleteConversationAsync("user-a", ownedConversation.Id);

        Assert.False(renameByOtherUser);
        Assert.True(renameByOwner);
        Assert.False(deleteByOtherUser);
        Assert.True(deleteByOwner);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"conversation-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
