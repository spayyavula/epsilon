using System.Runtime.CompilerServices;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;

namespace Epsilon.Web.Services;

public class WebSolverService
{
    private readonly ProviderRegistry _registry;
    private readonly UserKeyStore _keyStore;

    public WebSolverService(ProviderRegistry registry, UserKeyStore keyStore)
    {
        _registry = registry;
        _keyStore = keyStore;
    }

    public async IAsyncEnumerable<StreamChunk> SolveAsync(
        Guid userId, string equation, string providerId, string modelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var provider = _registry.Get(providerId)
            ?? throw new InvalidOperationException($"Provider '{providerId}' not found.");

        var apiKey = await _keyStore.GetKeyAsync(userId, providerId);

        var request = new ChatRequest
        {
            Model = modelId,
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = equation },
            },
            SystemPrompt = SolverSystemPrompt,
            Temperature = 0.3f,
            MaxTokens = 4096,
        };

        await foreach (var chunk in provider.StreamAsync(request, apiKey, ct))
        {
            yield return chunk;
            if (chunk.Done) break;
        }
    }

    private const string SolverSystemPrompt = """
        You are Epsilon, an expert equation solver and mathematical computation engine.

        When solving equations or mathematical expressions:
        1. **IDENTIFY** the equation type and state it clearly
        2. **SHOW EVERY STEP** as a numbered step with a clear label
        3. **FORMAT** each step as:
           ### Step N: [Description]
           [Show the mathematical work with LaTeX]
           [Brief explanation of what was done]
        4. **USE LATEX** for all mathematics: inline $...$ and display $$...$$
        5. **BOX THE FINAL ANSWER**: $$\boxed{answer}$$
        6. **VERIFY** the answer by substituting back or checking

        You can solve: linear equations, quadratic/cubic/polynomial equations,
        trigonometric equations, exponential and logarithmic equations,
        differential equations, integrals, limits, series, matrix equations,
        inequalities, and optimization problems.

        For each step, show the transformation clearly:
        $$\text{previous expression} \implies \text{next expression}$$

        If the problem has multiple solutions, find ALL of them.
        If the problem has no solution, explain why.
        Be precise with mathematical notation.
        """;
}
