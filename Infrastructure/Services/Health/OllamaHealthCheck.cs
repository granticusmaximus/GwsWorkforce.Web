using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GwsWorkforce.Web.Infrastructure.Services.Health;

public sealed class OllamaHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ollama-health");
            using var response = await client.GetAsync("/api/tags", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama responded successfully.")
                : HealthCheckResult.Unhealthy($"Ollama health probe failed with status code {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Unable to reach Ollama health endpoint.", ex);
        }
    }
}