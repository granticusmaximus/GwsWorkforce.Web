using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GwsWorkforce.Web.Models.Workforce;

namespace GwsWorkforce.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<WorkerDefinition> WorkerDefinitions => Set<WorkerDefinition>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    public DbSet<UserKnowledgeItem> UserKnowledgeItems => Set<UserKnowledgeItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<WorkerDefinition>()
            .HasIndex(x => x.Key)
            .IsUnique();

        builder.Entity<Conversation>()
            .HasIndex(x => x.ApplicationUserId);

        builder.Entity<Conversation>()
            .HasIndex(x => new { x.ApplicationUserId, x.UpdatedAtUtc, x.CreatedAtUtc });

        builder.Entity<ConversationMessage>()
            .HasIndex(x => new { x.ConversationId, x.CreatedAtUtc });

        builder.Entity<UserKnowledgeItem>()
            .HasIndex(x => x.ApplicationUserId);

        builder.Entity<UserKnowledgeItem>()
            .HasIndex(x => new { x.ApplicationUserId, x.Category });
    }
}
