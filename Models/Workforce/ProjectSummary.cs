namespace GwsWorkforce.Web.Models.Workforce;

public sealed class ProjectSummary
{
    public required string Name { get; init; }

    public required int ConversationCount { get; init; }

    public required DateTime LastActivityUtc { get; init; }

    public required int LatestConversationId { get; init; }
}
