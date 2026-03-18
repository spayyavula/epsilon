using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Epsilon.Core.Models;

namespace Epsilon.Core.LLM;

public class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private const string BaseUrl = "http://localhost:11434";

    public string Id => "ollama";
    public string DisplayName => "Ollama (Local)";
    public bool RequiresApiKey => false;

    public OllamaProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ModelInfo>> ListModelsAsync(string? apiKey = null)
    {
        try
        {
            var json = await _http.GetFromJsonAsync<JsonElement>($"{BaseUrl}/api/tags");
            var models = new List<ModelInfo>();

            if (json.TryGetProperty("models", out var modelsArray))
            {
                foreach (var m in modelsArray.EnumerateArray())
                {
                    var name = m.GetProperty("name").GetString() ?? "unknown";
                    models.Add(new ModelInfo { Id = name, Name = name, Provider = Id });
                }
            }

            return models;
        }
        catch
        {
            return new List<ModelInfo>
            {
                new() { Id = "llama3.2", Name = "Llama 3.2 (pull required)", Provider = Id },
                new() { Id = "mistral", Name = "Mistral (pull required)", Provider = Id },
                new() { Id = "phi3", Name = "Phi-3 (pull required)", Provider = Id },
                new() { Id = "gemma2", Name = "Gemma 2 (pull required)", Provider = Id },
            };
        }
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, string? apiKey = null)
    {
        var messages = BuildMessages(request);
        var body = new
        {
            model = request.Model,
            messages,
            stream = false,
            options = new { temperature = request.Temperature },
        };

        var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/chat", body);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var content = json.GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        return new ChatResponse
        {
            Content = content,
            Model = request.Model,
            FinishReason = "stop",
        };
    }

    public async IAsyncEnumerable<StreamChunk> StreamAsync(
        ChatRequest request,
        string? apiKey = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = BuildMessages(request);
        var body = JsonSerializer.Serialize(new
        {
            model = request.Model,
            messages,
            stream = true,
            options = new { temperature = request.Temperature },
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/chat");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage? resp = null;
        string? connectionError = null;
        try
        {
            resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            connectionError = "Error: Cannot connect to Ollama. Make sure it is running (ollama serve).";
        }

        if (connectionError != null)
        {
            yield return new StreamChunk { Delta = connectionError, Done = true };
            yield break;
        }

        using (resp!)
        {
            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line)) continue;

                var parsed = JsonSerializer.Deserialize<JsonElement>(line);
                var done = parsed.TryGetProperty("done", out var d) && d.GetBoolean();
                var content = parsed.TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var c)
                        ? c.GetString() ?? ""
                        : "";

                yield return new StreamChunk { Delta = content, Done = done };

                if (done) yield break;
            }
        }

        yield return new StreamChunk { Done = true };
    }

    public async Task<bool> HealthCheckAsync(string? apiKey = null)
    {
        try
        {
            var resp = await _http.GetAsync($"{BaseUrl}/api/tags");
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
