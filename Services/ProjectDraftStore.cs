namespace GwsWorkforce.Web.Services;

public sealed class ProjectDraftStore
{
    private readonly object sync = new();
    private readonly Dictionary<string, ProjectDraft> drafts = new(StringComparer.OrdinalIgnoreCase);

    public string SetDraft(string applicationUserId, string prompt)
    {
        var draftId = Guid.NewGuid().ToString("N");
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        lock (sync)
        {
            CleanupExpiredDrafts_NoLock();
            drafts[draftId] = new ProjectDraft(applicationUserId, prompt, expiresAtUtc);
        }

        return draftId;
    }

    public string? TakeDraft(string applicationUserId, string draftId)
    {
        lock (sync)
        {
            CleanupExpiredDrafts_NoLock();

            if (!drafts.TryGetValue(draftId, out var draft))
            {
                return null;
            }

            drafts.Remove(draftId);

            if (!string.Equals(draft.ApplicationUserId, applicationUserId, StringComparison.Ordinal))
            {
                return null;
            }

            return draft.Prompt;
        }
    }

    private void CleanupExpiredDrafts_NoLock()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = drafts
            .Where(x => x.Value.ExpiresAtUtc <= now)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            drafts.Remove(key);
        }
    }

    private sealed record ProjectDraft(string ApplicationUserId, string Prompt, DateTime ExpiresAtUtc);
}
