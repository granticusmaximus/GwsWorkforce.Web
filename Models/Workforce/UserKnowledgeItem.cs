namespace GwsWorkforce.Web.Models.Workforce;

public class UserKnowledgeItem
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}