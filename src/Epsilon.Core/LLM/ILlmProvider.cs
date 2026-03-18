using Epsilon.Core.Models;

namespace Epsilon.Core.LLM;

public interface ILlmProvider
{
    string Id { get; }
    string DisplayName { get; }
    bool RequiresApiKey { get; }

    Task<List<ModelInfo>> ListModelsAsync(string? apiKey = null);
    Task<ChatResponse> SendAsync(ChatRequest request, string? apiKey = null);
    IAsyncEnumerable<StreamChunk> StreamAsync(ChatRequest request, string? apiKey = null, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(string? apiKey = null);
}
