using Epsilon.Core.LLM;
using Epsilon.Web.Auth;
using Epsilon.Web.Contracts;
using Epsilon.Web.Services;

namespace Epsilon.Web.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings").RequireAuthorization().WithTags("Settings");

        group.MapGet("/providers", (ProviderRegistry registry) =>
        {
            return registry.ListProviders().Select(p => new ProviderDto(p.Id, p.Name, p.RequiresApiKey));
        });

        group.MapGet("/providers/{providerId}/models", async (string providerId,
            UserContext user, ProviderRegistry registry, UserKeyStore keyStore) =>
        {
            var provider = registry.Get(providerId);
            if (provider == null) return Results.NotFound();

            var apiKey = await keyStore.GetKeyAsync(user.UserId, providerId);
            try
            {
                if (!string.IsNullOrEmpty(apiKey))
                {
                    var models = await provider.ListModelsAsync(apiKey);
                    if (models.Count > 0)
                        return Results.Ok(models.Select(m => new ModelDto(m.Id, m.Name, m.Provider)));
                }
            }
            catch { /* Fall through to defaults */ }

            // Return frontier model defaults when no API key or API call fails
            var defaults = GetDefaultModels(providerId);
            return Results.Ok(defaults);
        });

        group.MapPost("/providers/{providerId}/test", async (string providerId,
            UserContext user, ProviderRegistry registry, UserKeyStore keyStore) =>
        {
            var provider = registry.Get(providerId);
            if (provider == null) return Results.NotFound();

            var apiKey = await keyStore.GetKeyAsync(user.UserId, providerId);
            var ok = await provider.HealthCheckAsync(apiKey);
            return Results.Ok(new { connected = ok });
        });

        group.MapGet("/api-keys", async (UserContext user, UserKeyStore keyStore) =>
        {
            var keys = await keyStore.ListConfiguredAsync(user.UserId);
            return keys.Select(k => new ApiKeyStatusDto(k.ProviderId, true, k.UpdatedAt));
        });

        group.MapPut("/api-keys/{providerId}", async (string providerId,
            SaveApiKeyRequest req, UserContext user, UserKeyStore keyStore) =>
        {
            await keyStore.SaveKeyAsync(user.UserId, providerId, req.ApiKey);
            return Results.NoContent();
        });

        group.MapDelete("/api-keys/{providerId}", async (string providerId,
            UserContext user, UserKeyStore keyStore) =>
        {
            await keyStore.DeleteKeyAsync(user.UserId, providerId);
            return Results.NoContent();
        });
    }

    private static List<ModelDto> GetDefaultModels(string providerId) => providerId switch
    {
        "openai" => new()
        {
            new("gpt-4.1", "GPT-4.1", "openai"),
            new("gpt-4.1-mini", "GPT-4.1 Mini", "openai"),
            new("gpt-4.1-nano", "GPT-4.1 Nano", "openai"),
            new("gpt-4o", "GPT-4o", "openai"),
            new("gpt-4o-mini", "GPT-4o Mini", "openai"),
            new("o3", "o3", "openai"),
            new("o3-mini", "o3-mini", "openai"),
            new("o4-mini", "o4-mini", "openai"),
        },
        "anthropic" => new()
        {
            new("claude-sonnet-4-20250514", "Claude Sonnet 4", "anthropic"),
            new("claude-opus-4-20250514", "Claude Opus 4", "anthropic"),
            new("claude-haiku-4-5-20251001", "Claude Haiku 4.5", "anthropic"),
        },
        "gemini" => new()
        {
            new("gemini-2.5-pro", "Gemini 2.5 Pro", "gemini"),
            new("gemini-2.5-flash", "Gemini 2.5 Flash", "gemini"),
            new("gemini-2.0-flash", "Gemini 2.0 Flash", "gemini"),
        },
        "ollama" => new()
        {
            new("llama3.3", "Llama 3.3 70B", "ollama"),
            new("qwen3", "Qwen 3", "ollama"),
            new("deepseek-r1", "DeepSeek R1", "ollama"),
            new("phi4", "Phi-4", "ollama"),
        },
        _ => new(),
    };
}
