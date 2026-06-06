namespace GwsWorkforce.Web.Models.Workforce;

public class Conversation
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public int WorkerDefinitionId { get; set; }

    public string Title { get; set; } = "New Conversation";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public WorkerDefinition? WorkerDefinition { get; set; }

    public List<ConversationMessage> Messages { get; set; } = [];
}