namespace GwsWorkforce.Web.Models.Workforce;

public static class ProjectNaming
{
    private const string Prefix = "Project:";

    public static string EncodeProjectKey(string projectName)
    {
        return Uri.EscapeDataString(projectName.Trim());
    }

    public static string DecodeProjectKey(string projectKey)
    {
        return Uri.UnescapeDataString(projectKey).Trim();
    }

    public static string BuildConversationTitle(string projectName, string track)
    {
        return $"{Prefix} {projectName.Trim()} | {track.Trim()}";
    }

    public static bool TryExtractProjectName(string conversationTitle, out string? projectName)
    {
        projectName = null;

        if (string.IsNullOrWhiteSpace(conversationTitle) ||
            !conversationTitle.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var raw = conversationTitle[Prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var separatorIndex = raw.IndexOf('|');
        if (separatorIndex >= 0)
        {
            raw = raw[..separatorIndex].Trim();
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        projectName = raw;
        return true;
    }
}
