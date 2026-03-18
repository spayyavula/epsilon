using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Epsilon.Core.Database;
using Epsilon.Core.Documents;
using Epsilon.Core.LLM;
using Epsilon.Core.Research;
using Epsilon.Core.Services;
using Epsilon.Core.WebSearch;
using Epsilon.App.ViewModels;

namespace Epsilon.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Epsilon");
        Directory.CreateDirectory(appData);

        var docsDir = Path.Combine(appData, "documents");
        Directory.CreateDirectory(docsDir);

        var dbPath = Path.Combine(appData, "epsilon.db");

        var services = new ServiceCollection();

        // Core services
        services.AddSingleton(new DatabaseService(dbPath));
        services.AddSingleton<HttpClient>();

        // LLM providers
        services.AddSingleton(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var registry = new ProviderRegistry();
            registry.Register(new OpenAiProvider(http));
            registry.Register(new AnthropicProvider(http));
            registry.Register(new GeminiProvider(http));
            registry.Register(new OllamaProvider(http));
            return registry;
        });

        // Document processing & RAG
        services.AddSingleton<DocumentProcessor>();
        services.AddSingleton<DocumentRetriever>();
        services.AddSingleton<FolderScanner>();

        // Web search
        services.AddSingleton<ExaClient>();
        services.AddSingleton<WebSearchService>();

        // Services
        services.AddSingleton<ChatService>();
        services.AddSingleton<ResearchService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DocumentsViewModel>();
        services.AddTransient<ResearchToolkitViewModel>();
        services.AddTransient<SolverViewModel>();
        services.AddTransient<FlashcardViewModel>();

        // Configuration values
        services.AddSingleton(new AppConfig { DocsDirectory = docsDir });

        Services = services.BuildServiceProvider();
    }
}

public class AppConfig
{
    public string DocsDirectory { get; init; } = "";
}
