using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Epsilon.Core.Models;

namespace Epsilon.Core.LLM;

public class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.openai.com/v1";

    public string Id => "openai";
    public string DisplayName => "OpenAI";
    public bool RequiresApiKey => true;

    public OpenAiProvider(HttpClient http)
    {
        _http = http;
    }

    public Task<List<ModelInfo>> ListModelsAsync(string? apiKey = null)
    {
        return Task.FromResult(new List<ModelInfo>
        {
            new() { Id = "gpt-4o", Name = "GPT-4o", Provider = Id },
            new() { Id = "gpt-4o-mini", Name = "GPT-4o Mini", Provider = Id },
            new() { Id = "o3-mini", Name = "o3-mini", Provider = Id },
        });
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("OpenAI API key is required.");

        var messages = BuildMessages(request);
        var body = new
        {
            model = request.Model,
            messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = JsonContent.Create(body);

        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var content = json.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        return new ChatResponse
        {
            Content = content,
            Model = request.Model,
            FinishReason = json.GetProperty("choices")[0]
                .GetProperty("finish_reason")
                .GetString(),
        };
    }

    public async IAsyncEnumerable<StreamChunk> StreamAsync(
        ChatRequest request,
        string? apiKey = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("OpenAI API key is required.");

        var messages = BuildMessages(request);
        var body = JsonSerializer.Serialize(new
        {
            model = request.Model,
            messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = true,
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

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
            if (data == "[DONE]")
            {
                yield return new StreamChunk { Done = true };
                yield break;
            }

            var parsed = JsonSerializer.Deserialize<JsonElement>(data);
            var delta = parsed.GetProperty("choices")[0]
                .GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentEl))
            {
                var text = contentEl.GetString();
                if (!string.IsNullOrEmpty(text))
                    yield return new StreamChunk { Delta = text };
            }
        }

        yield return new StreamChunk { Done = true };
    }

    public async Task<bool> HealthCheckAsync(string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey)) return false;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/models");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static List<object> BuildMessages(ChatRequest request)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });
        foreach (var msg in request.Messages)
            messages.Add(new { role = msg.Role, content = msg.Content });
        return messages;
    }
}
