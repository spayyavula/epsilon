using Epsilon.Core.Models;

namespace Epsilon.Core.LLM;

public class ProviderRegistry
{
    private readonly Dictionary<string, ILlmProvider> _providers = new();

    public void Register(ILlmProvider provider)
    {
        _providers[provider.Id] = provider;
    }

    public ILlmProvider? Get(string id)
    {
        return _providers.TryGetValue(id, out var provider) ? provider : null;
    }

    public List<ProviderInfo> ListProviders()
    {
        return _providers.Values.Select(p => new ProviderInfo
        {
            Id = p.Id,
            Name = p.DisplayName,
            RequiresApiKey = p.RequiresApiKey,
        }).ToList();
    }

    public IReadOnlyDictionary<string, ILlmProvider> All => _providers;
}
