using GwsWorkforce.Web.Data;
using GwsWorkforce.Web.Infrastructure.Services.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Application.Tests;

public class HealthChecksTests
{
    [Fact]
    public async Task DatabaseHealthCheck_ReturnsHealthy_WhenDatabaseIsReachable()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"health-db-{Guid.NewGuid()}"));

        using var provider = services.BuildServiceProvider();
        var check = new DatabaseHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task DatabaseHealthCheck_ReturnsUnhealthy_WhenDatabaseIsNotReachable()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite("Data Source=/root/gws-workforce-unreachable/health.db"));

        using var provider = services.BuildServiceProvider();
        var check = new DatabaseHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task OllamaHealthCheck_ReturnsHealthy_WhenProbeSucceeds()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("ollama-health")
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:11434"))
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK))));

        using var provider = services.BuildServiceProvider();
        var check = new OllamaHealthCheck(provider.GetRequiredService<IHttpClientFactory>());

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task OllamaHealthCheck_ReturnsUnhealthy_WhenProbeFails()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("ollama-health")
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:11434"))
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable))));

        using var provider = services.BuildServiceProvider();
        var check = new OllamaHealthCheck(provider.GetRequiredService<IHttpClientFactory>());

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request, cancellationToken);
    }
}