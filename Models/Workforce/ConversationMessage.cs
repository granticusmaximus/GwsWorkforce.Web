namespace GwsWorkforce.Web.Models.Workforce;

public class ConversationMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Conversation? Conversation { get; set; }
}