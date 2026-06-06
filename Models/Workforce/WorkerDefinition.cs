namespace GwsWorkforce.Web.Models.Workforce;

public class WorkerDefinition
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string SystemPrompt { get; set; } = string.Empty;

    public double Temperature { get; set; } = 0.2;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}