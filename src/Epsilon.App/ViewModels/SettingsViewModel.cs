using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Epsilon.Core.Database;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;
using Epsilon.Core.WebSearch;

namespace Epsilon.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly ProviderRegistry _registry;
    private readonly ExaClient _exa;

    [ObservableProperty]
    private string _openAiKey = "";

    [ObservableProperty]
    private string _anthropicKey = "";

    [ObservableProperty]
    private string _geminiKey = "";

    [ObservableProperty]
    private string _openAiStatus = "Not configured";

    [ObservableProperty]
    private string _anthropicStatus = "Not configured";

    [ObservableProperty]
    private string _geminiStatus = "Not configured";

    [ObservableProperty]
    private string _ollamaStatus = "Not checked";

    [ObservableProperty]
    private string _exaKey = "";

    [ObservableProperty]
    private string _exaStatus = "Not configured";

    [ObservableProperty]
    private string _statusMessage = "";

    public SettingsViewModel(DatabaseService db, ProviderRegistry registry, ExaClient exa)
    {
        _db = db;
        _registry = registry;
        _exa = exa;
        LoadKeys();
    }

    private void LoadKeys()
    {
        OpenAiKey = _db.GetSetting("openai_api_key") ?? "";
        AnthropicKey = _db.GetSetting("anthropic_api_key") ?? "";
        GeminiKey = _db.GetSetting("gemini_api_key") ?? "";

        ExaKey = _db.GetSetting("exa_api_key") ?? "";

        if (!string.IsNullOrEmpty(OpenAiKey)) OpenAiStatus = "Key saved";
        if (!string.IsNullOrEmpty(AnthropicKey)) AnthropicStatus = "Key saved";
        if (!string.IsNullOrEmpty(GeminiKey)) GeminiStatus = "Key saved";
        if (!string.IsNullOrEmpty(ExaKey)) ExaStatus = "Key saved";
    }

    [RelayCommand]
    private void SaveOpenAiKey()
    {
        _db.SetSetting("openai_api_key", OpenAiKey, true);
        OpenAiStatus = "Key saved";
        StatusMessage = "OpenAI API key saved.";
    }

    [RelayCommand]
    private void SaveAnthropicKey()
    {
        _db.SetSetting("anthropic_api_key", AnthropicKey, true);
        AnthropicStatus = "Key saved";
        StatusMessage = "Anthropic API key saved.";
    }

    [RelayCommand]
    private void SaveGeminiKey()
    {
        _db.SetSetting("gemini_api_key", GeminiKey, true);
        GeminiStatus = "Key saved";
        StatusMessage = "Gemini API key saved.";
    }

    [RelayCommand]
    private async Task TestOpenAi()
    {
        OpenAiStatus = "Testing...";
        var provider = _registry.Get("openai")!;
        var ok = await provider.HealthCheckAsync(OpenAiKey);
        OpenAiStatus = ok ? "Connected" : "Failed";
    }

    [RelayCommand]
    private async Task TestAnthropic()
    {
        AnthropicStatus = "Testing...";
        var provider = _registry.Get("anthropic")!;
        var ok = await provider.HealthCheckAsync(AnthropicKey);
        AnthropicStatus = ok ? "Connected" : "Failed";
    }

    [RelayCommand]
    private async Task TestGemini()
    {
        GeminiStatus = "Testing...";
        var provider = _registry.Get("gemini")!;
        var ok = await provider.HealthCheckAsync(GeminiKey);
        GeminiStatus = ok ? "Connected" : "Failed";
    }

    [RelayCommand]
    private void SaveExaKey()
    {
        _db.SetSetting("exa_api_key", ExaKey, true);
        ExaStatus = "Key saved";
        StatusMessage = "Exa API key saved. Toggle 'Web' in the chat to use web search.";
    }

    [RelayCommand]
    private async Task TestExa()
    {
        ExaStatus = "Testing...";
        var ok = await _exa.HealthCheckAsync(ExaKey);
        ExaStatus = ok ? "Connected" : "Failed";
    }

    [RelayCommand]
    private async Task TestOllama()
    {
        OllamaStatus = "Testing...";
        var provider = _registry.Get("ollama")!;
        var ok = await provider.HealthCheckAsync();
        OllamaStatus = ok ? "Connected" : "Not running";
    }
}
