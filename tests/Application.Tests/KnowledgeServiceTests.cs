using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Infrastructure.Services;
using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class KnowledgeServiceTests
{
    [Fact]
    public async Task GetUserKnowledgeItemsAsync_PagesAndFiltersByUser()
    {
        await using var dbContext = CreateDbContext();

        dbContext.UserKnowledgeItems.AddRange(
            new UserKnowledgeItem { ApplicationUserId = "user-a", Category = "cat", Title = "A1", Content = "c1", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow },
            new UserKnowledgeItem { ApplicationUserId = "user-a", Category = "cat", Title = "A2", Content = "c2", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1) },
            new UserKnowledgeItem { ApplicationUserId = "user-b", Category = "cat", Title = "B1", Content = "c3", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });

        await dbContext.SaveChangesAsync();

        var service = new KnowledgeService(dbContext);
        var page = await service.GetUserKnowledgeItemsAsync("user-a", 1, 1);

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("user-a", page.Items[0].ApplicationUserId);
    }

    [Fact]
    public async Task ToggleAndDelete_RespectOwnership()
    {
        await using var dbContext = CreateDbContext();

        var item = new UserKnowledgeItem
        {
            ApplicationUserId = "user-a",
            Category = "cat",
            Title = "A1",
            Content = "c1",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.UserKnowledgeItems.Add(item);
        await dbContext.SaveChangesAsync();

        var service = new KnowledgeService(dbContext);

        var toggleByOtherUser = await service.ToggleKnowledgeItemAsync("user-b", item.Id);
        var toggleByOwner = await service.ToggleKnowledgeItemAsync("user-a", item.Id);
        var deleteByOtherUser = await service.DeleteKnowledgeItemAsync("user-b", item.Id);
        var deleteByOwner = await service.DeleteKnowledgeItemAsync("user-a", item.Id);

        Assert.False(toggleByOtherUser);
        Assert.True(toggleByOwner);
        Assert.False(deleteByOtherUser);
        Assert.True(deleteByOwner);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"knowledge-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
