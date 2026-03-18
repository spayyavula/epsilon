using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Epsilon.Core.Models;

namespace Epsilon.Core.LLM;

public class AnthropicProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.anthropic.com/v1";

    public string Id => "anthropic";
    public string DisplayName => "Anthropic";
    public bool RequiresApiKey => true;

    public AnthropicProvider(HttpClient http)
    {
        _http = http;
    }

    public Task<List<ModelInfo>> ListModelsAsync(string? apiKey = null)
    {
        return Task.FromResult(new List<ModelInfo>
        {
            new() { Id = "claude-sonnet-4-20250514", Name = "Claude Sonnet 4", Provider = Id },
            new() { Id = "claude-haiku-4-5-20251001", Name = "Claude Haiku 4.5", Provider = Id },
            new() { Id = "claude-opus-4-20250514", Name = "Claude Opus 4", Provider = Id },
        });
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Anthropic API key is required.");

        var messages = request.Messages
            .Where(m => m.Role != "system")
            .Select(m => new { role = m.Role, content = m.Content })
            .ToList();

        var bodyDict = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["messages"] = messages,
            ["max_tokens"] = request.MaxTokens,
        };

        if (!string.IsNullOrEmpty(request.SystemPrompt))
            bodyDict["system"] = request.SystemPrompt;
        if (request.Temperature > 0)
            bodyDict["temperature"] = request.Temperature;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(
            JsonSerializer.Serialize(bodyDict), Encoding.UTF8, "application/json");

        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var content = json.GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "";

        return new ChatResponse
        {
            Content = content,
            Model = request.Model,
            FinishReason = json.TryGetProperty("stop_reason", out var sr) ? sr.GetString() : null,
        };
    }

    public async IAsyncEnumerable<StreamChunk> StreamAsync(
        ChatRequest request,
        string? apiKey = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Anthropic API key is required.");

        var messages = request.Messages
            .Where(m => m.Role != "system")
            .Select(m => new { role = m.Role, content = m.Content })
            .ToList();

        var bodyDict = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["messages"] = messages,
            ["max_tokens"] = request.MaxTokens,
            ["stream"] = true,
        };

        if (!string.IsNullOrEmpty(request.SystemPrompt))
            bodyDict["system"] = request.SystemPrompt;
        if (request.Temperature > 0)
            bodyDict["temperature"] = request.Temperature;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(
            JsonSerializer.Serialize(bodyDict), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line[6..];
            var parsed = JsonSerializer.Deserialize<JsonElement>(data);

            var eventType = parsed.TryGetProperty("type", out var t) ? t.GetString() : null;

            switch (eventType)
            {
                case "content_block_delta":
                    var delta = parsed.GetProperty("delta");
                    if (delta.TryGetProperty("text", out var textEl))
                    {
                        var text = textEl.GetString();
                        if (!string.IsNullOrEmpty(text))
                            yield return new StreamChunk { Delta = text };
                    }
                    break;

                case "message_stop":
                    yield return new StreamChunk { Done = true };
                    yield break;
            }
        }

        yield return new StreamChunk { Done = true };
    }

    public async Task<bool> HealthCheckAsync(string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey)) return false;
        try
        {
            var bodyDict = new Dictionary<string, object>
            {
                ["model"] = "claude-haiku-4-5-20251001",
                ["messages"] = new[] { new { role = "user", content = "ping" } },
                ["max_tokens"] = 1,
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages");
            req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = new StringContent(
                JsonSerializer.Serialize(bodyDict), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
