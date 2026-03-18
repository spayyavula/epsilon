using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Epsilon.Core.Models;

namespace Epsilon.Core.LLM;

public class GeminiProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    public string Id => "gemini";
    public string DisplayName => "Google Gemini";
    public bool RequiresApiKey => true;

    public GeminiProvider(HttpClient http)
    {
        _http = http;
    }

    public Task<List<ModelInfo>> ListModelsAsync(string? apiKey = null)
    {
        return Task.FromResult(new List<ModelInfo>
        {
            new() { Id = "gemini-2.5-flash", Name = "Gemini 2.5 Flash", Provider = Id },
            new() { Id = "gemini-2.5-pro", Name = "Gemini 2.5 Pro", Provider = Id },
        });
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Gemini API key is required.");

        var contents = BuildContents(request);
        var bodyDict = new Dictionary<string, object>
        {
            ["contents"] = contents,
            ["generationConfig"] = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens,
            },
        };

        if (!string.IsNullOrEmpty(request.SystemPrompt))
            bodyDict["systemInstruction"] = new { parts = new[] { new { text = request.SystemPrompt } } };

        var url = $"{BaseUrl}/models/{request.Model}:generateContent?key={apiKey}";
        var resp = await _http.PostAsJsonAsync(url, bodyDict);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var content = json.GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";

        return new ChatResponse
        {
            Content = content,
            Model = request.Model,
            FinishReason = json.GetProperty("candidates")[0]
                .TryGetProperty("finishReason", out var fr) ? fr.GetString() : null,
        };
    }

    public async IAsyncEnumerable<StreamChunk> StreamAsync(
        ChatRequest request,
        string? apiKey = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Gemini API key is required.");

        var contents = BuildContents(request);
        var bodyDict = new Dictionary<string, object>
        {
            ["contents"] = contents,
            ["generationConfig"] = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens,
            },
        };

        if (!string.IsNullOrEmpty(request.SystemPrompt))
            bodyDict["systemInstruction"] = new { parts = new[] { new { text = request.SystemPrompt } } };

        var url = $"{BaseUrl}/models/{request.Model}:streamGenerateContent?key={apiKey}&alt=sse";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
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

            if (parsed.TryGetProperty("candidates", out var candidates))
            {
                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

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
            var resp = await _http.GetAsync($"{BaseUrl}/models?key={apiKey}");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static List<object> BuildContents(ChatRequest request)
    {
        return request.Messages
            .Where(m => m.Role != "system")
            .Select(m => (object)new
            {
                role = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } },
            })
            .ToList();
    }
}
