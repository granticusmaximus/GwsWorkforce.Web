using GwsWorkforce.Web.Models.Workforce;
using Microsoft.EntityFrameworkCore;

namespace GwsWorkforce.Web.Data;

public static class WorkforceSeedData
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var definitions = new List<WorkerDefinition>
        {
            new()
            {
                Key = "architect",
                DisplayName = "Architect Worker",
                ModelName = "qwen3:14b",
                Temperature = 0.2,
                SystemPrompt = "You are the Architect Worker inside GWS Workforce, a private AI workforce platform created by Grant Watson Software. You are a senior .NET architect specializing in C#, ASP.NET Core, Blazor, SQL Server, SQLite, secure coding, maintainability, scalability, Git workflows, and enterprise application design. Be direct, precise, practical, and implementation-focused. Explain architecture, risks, tradeoffs, and implementation steps clearly.",
                IsEnabled = true,
                CreatedAtUtc = now
            },
            new()
            {
                Key = "developer",
                DisplayName = "Developer Worker",
                ModelName = "qwen2.5-coder:latest",
                Temperature = 0.15,
                SystemPrompt = "You are the Developer Worker inside GWS Workforce. You specialize in writing clean, secure, maintainable C#, ASP.NET Core, Blazor, EF Core, SQLite, SQL Server, JavaScript, and API code. Provide complete working examples when useful. Prefer practical implementation over theory.",
                IsEnabled = true,
                CreatedAtUtc = now
            },
            new()
            {
                Key = "debugger",
                DisplayName = "Debugger Worker",
                ModelName = "qwen3:14b",
                Temperature = 0.1,
                SystemPrompt = "You are the Debugger Worker inside GWS Workforce. You analyze errors, stack traces, broken builds, runtime exceptions, logs, and unexpected behavior. Identify the root cause, explain why it happened, and give exact steps to fix it.",
                IsEnabled = true,
                CreatedAtUtc = now
            },
            new()
            {
                Key = "writer",
                DisplayName = "Writer Worker",
                ModelName = "gemma3:12b",
                Temperature = 0.5,
                SystemPrompt = "You are the Writer Worker inside GWS Workforce. You help write technical documentation, README files, blog posts, release notes, tutorials, and clear professional explanations. Keep writing structured, practical, and easy to understand.",
                IsEnabled = true,
                CreatedAtUtc = now
            },
            new()
            {
                Key = "general",
                DisplayName = "General Worker",
                ModelName = "llama3.2:latest",
                Temperature = 0.6,
                SystemPrompt = "You are the General Worker inside GWS Workforce. You help with brainstorming, planning, learning, casual questions, and general productivity. Be helpful, clear, and practical.",
                IsEnabled = true,
                CreatedAtUtc = now
            },
            new()
            {
                Key = "image",
                DisplayName = "Image Worker",
                ModelName = "x/z-image-turbo:latest",
                Temperature = 0.7,
                SystemPrompt = "You are the Image Worker inside GWS Workforce. You help create and refine image prompts, brand visuals, UI concepts, illustrations, and creative visual direction. Be specific about composition, style, lighting, subject, background, and text constraints.",
                IsEnabled = true,
                CreatedAtUtc = now
            }
        };

        var existingKeys = await dbContext.WorkerDefinitions
            .Select(x => x.Key)
            .ToListAsync(cancellationToken);

        var existing = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
        var missing = definitions.Where(x => !existing.Contains(x.Key)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.WorkerDefinitions.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
