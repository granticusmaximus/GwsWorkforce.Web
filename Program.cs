using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GwsWorkforce.Web.Components;
using GwsWorkforce.Web.Components.Account;
using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Application.Contracts;
using GwsWorkforce.Web.Infrastructure.Services.Health;
using GwsWorkforce.Web.Infrastructure.Services;
using GwsWorkforce.Web.Services;
using GwsWorkforce.Web.Services.Ollama;

var builder = WebApplication.CreateBuilder(args);
var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";

if (!Uri.TryCreate(ollamaBaseUrl, UriKind.Absolute, out var ollamaBaseUri))
{
    throw new InvalidOperationException("Ollama:BaseUrl must be a valid absolute URL.");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHttpClient("ollama-health", client =>
{
    client.BaseAddress = ollamaBaseUri;
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient<OllamaChatService>(client =>
{
    client.BaseAddress = ollamaBaseUri;
    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<OllamaHealthCheck>("ollama", tags: new[] { "ready", "live" });
builder.Services.AddScoped<IWorkerCatalogService, WorkerCatalogService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<IChatOrchestrationService, ChatOrchestrationService>();
builder.Services.AddSingleton<ProjectDraftStore>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

await WorkforceSeedData.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        app.Logger.LogWarning(
            "404 Not Found: {Method} {Path}{Query} Referer={Referer}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.Request.Headers.Referer.ToString());
    }
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Compatibility redirects for removed starter-template pages.
app.MapGet("/counter", () => Results.Redirect("/projects", permanent: false));
app.MapGet("/weather", () => Results.Redirect("/projects", permanent: false));
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
