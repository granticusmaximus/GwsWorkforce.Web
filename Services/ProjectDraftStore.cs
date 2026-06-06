namespace GwsWorkforce.Web.Services;

public sealed class ProjectDraftStore
{
    private readonly Dictionary<string, string> drafts = new(StringComparer.OrdinalIgnoreCase);

    public void SetDraft(string projectKey, string prompt)
    {
        drafts[projectKey] = prompt;
    }

    public string? TakeDraft(string projectKey)
    {
        if (!drafts.TryGetValue(projectKey, out var prompt))
        {
            return null;
        }

        drafts.Remove(projectKey);
        return prompt;
    }
}
