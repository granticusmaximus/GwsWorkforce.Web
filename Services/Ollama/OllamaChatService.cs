using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GwsWorkforce.Web.Services.Ollama;

public sealed class OllamaChatService(HttpClient httpClient)
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);

    public async Task<string> ChatAsync(
        string modelName,
        string systemPrompt,
        string userPrompt,
        IReadOnlyCollection<ChatMessageInput>? history = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<OllamaMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new OllamaMessage("system", systemPrompt));
        }

        if (history is not null)
        {
            messages.AddRange(history
                .Where(x => !string.IsNullOrWhiteSpace(x.Role) && !string.IsNullOrWhiteSpace(x.Content))
                .Select(x => new OllamaMessage(x.Role, x.Content)));
        }

        messages.Add(new OllamaMessage("user", userPrompt));

        var request = new OllamaChatRequest(modelName, messages, false);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        try
        {
            using var response = await httpClient.PostAsJsonAsync("/api/chat", request, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                throw new OllamaChatException(CreateFailureMessage(response.StatusCode, modelName, body));
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: timeoutCts.Token);
            var content = payload?.Message?.Content;

            return string.IsNullOrWhiteSpace(content)
                ? "No response was returned from Ollama."
                : content.Trim();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OllamaChatException("Ollama timed out while generating a response. Try a shorter prompt, switch workers, or retry in a moment.");
        }
        catch (HttpRequestException)
        {
            throw new OllamaChatException("Unable to reach Ollama at http://localhost:11434. Make sure Ollama is running locally.");
        }
        catch (JsonException)
        {
            throw new OllamaChatException("Ollama returned an unexpected response format. Try again, or check the Ollama logs.");
        }
    }

    private static string CreateFailureMessage(System.Net.HttpStatusCode statusCode, string modelName, string body)
    {
        var lower = body.ToLowerInvariant();
        var modelMissing = lower.Contains("model") && (lower.Contains("not found") || lower.Contains("unknown") || lower.Contains("pull"));
        var notChatCapable = lower.Contains("chat") && (lower.Contains("not supported") || lower.Contains("unsupported") || lower.Contains("cannot"));

        if (statusCode == System.Net.HttpStatusCode.NotFound || modelMissing)
        {
            return $"The selected model '{modelName}' is not available in Ollama. Confirm it is installed and try again.";
        }

        if (statusCode == System.Net.HttpStatusCode.BadRequest || notChatCapable)
        {
            return $"The selected model '{modelName}' rejected /api/chat and may not support chat completions. Use a chat-capable model and retry. Ollama says: {body}";
        }

        return $"Ollama request failed ({(int)statusCode}). {body}";
    }

    private sealed record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyCollection<OllamaMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaMessage? Message);

    private sealed record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);
}

public sealed record ChatMessageInput(string Role, string Content);

public sealed class OllamaChatException(string message) : Exception(message);
