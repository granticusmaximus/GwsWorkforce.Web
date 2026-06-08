namespace GwsWorkforce.Web.Services;

public sealed class ProjectDraftStore
{
    private readonly object sync = new();
    private readonly Dictionary<string, ProjectDraft> drafts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProjectPrompt> projectPrompts = new(StringComparer.OrdinalIgnoreCase);

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

            if (!string.Equals(draft.ApplicationUserId, applicationUserId, StringComparison.Ordinal))
            {
                return null;
            }

            return draft.Prompt;
        }
    }

    public void SetProjectPrompt(string applicationUserId, string projectName, string prompt)
    {
        var normalizedProjectName = projectName.Trim();
        var normalizedPrompt = prompt.Trim();

        if (string.IsNullOrWhiteSpace(normalizedProjectName) || string.IsNullOrWhiteSpace(normalizedPrompt))
        {
            return;
        }

        var key = BuildProjectPromptKey(applicationUserId, normalizedProjectName);
        var expiresAtUtc = DateTime.UtcNow.AddHours(24);

        lock (sync)
        {
            CleanupExpiredProjectPrompts_NoLock();
            projectPrompts[key] = new ProjectPrompt(normalizedPrompt, expiresAtUtc);
        }
    }

    public string? GetProjectPrompt(string applicationUserId, string projectName)
    {
        var normalizedProjectName = projectName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProjectName))
        {
            return null;
        }

        var key = BuildProjectPromptKey(applicationUserId, normalizedProjectName);

        lock (sync)
        {
            CleanupExpiredProjectPrompts_NoLock();

            if (!projectPrompts.TryGetValue(key, out var storedPrompt))
            {
                return null;
            }

            return storedPrompt.Prompt;
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

    private void CleanupExpiredProjectPrompts_NoLock()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = projectPrompts
            .Where(x => x.Value.ExpiresAtUtc <= now)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            projectPrompts.Remove(key);
        }
    }

    private static string BuildProjectPromptKey(string applicationUserId, string projectName)
    {
        return $"{applicationUserId}|{projectName}";
    }

    private sealed record ProjectDraft(string ApplicationUserId, string Prompt, DateTime ExpiresAtUtc);

    private sealed record ProjectPrompt(string Prompt, DateTime ExpiresAtUtc);
}
