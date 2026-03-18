using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Epsilon.Core.Database;
using Epsilon.Core.Documents;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;
using Epsilon.Core.Services;
using Epsilon.Core.WebSearch;

namespace Epsilon.App.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatService _chatService;
    private readonly DatabaseService _db;
    private readonly ProviderRegistry _registry;
    private readonly DocumentProcessor _processor;
    private readonly string _docsDir;
    private CancellationTokenSource? _streamCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _messageInput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isStreaming;

    [ObservableProperty]
    private string _streamingContent = "";

    [ObservableProperty]
    private string? _activeConversationId;

    [ObservableProperty]
    private string _selectedProviderId = "openai";

    [ObservableProperty]
    private string _selectedModelId = "gpt-4o";

    [ObservableProperty]
    private bool _webSearchEnabled;

    [ObservableProperty]
    private bool _webSearchAvailable;

    public ObservableCollection<Conversation> Conversations { get; } = new();
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ProviderInfo> Providers { get; } = new();
    public ObservableCollection<ModelInfo> Models { get; } = new();

    public event Action<string, string>? MessageAdded;
    public event Action? ChatCleared;
    public event Action<string>? StreamingUpdated;
    public event Action? StreamingFinished;
    public event Action<string>? WebSourcesReady; // JSON payload of web sources

    public ChatViewModel(ChatService chatService, DatabaseService db, ProviderRegistry registry,
        DocumentProcessor processor, AppConfig config)
    {
        _chatService = chatService;
        _db = db;
        _registry = registry;
        _processor = processor;
        _docsDir = config.DocsDirectory;

        WebSearchAvailable = !string.IsNullOrEmpty(_db.GetSetting("exa_api_key"));

        LoadConversations();
        LoadProviders();
        LoadModels();
    }

    public void RefreshWebSearchAvailable()
    {
        WebSearchAvailable = !string.IsNullOrEmpty(_db.GetSetting("exa_api_key"));
    }

    private void LoadConversations()
    {
        Conversations.Clear();
        foreach (var conv in _chatService.ListConversations())
            Conversations.Add(conv);
    }

    private void LoadProviders()
    {
        Providers.Clear();
        foreach (var p in _registry.ListProviders())
            Providers.Add(p);
    }

    private async void LoadModels()
    {
        Models.Clear();
        var provider = _registry.Get(SelectedProviderId);
        if (provider == null) return;

        var apiKey = _db.GetSetting($"{SelectedProviderId}_api_key");
        try
        {
            var models = await provider.ListModelsAsync(apiKey);
            foreach (var m in models)
                Models.Add(m);

            if (Models.Count > 0 && !Models.Any(m => m.Id == SelectedModelId))
                SelectedModelId = Models[0].Id;
        }
        catch { /* Provider may not be configured yet */ }
    }

    partial void OnSelectedProviderIdChanged(string value)
    {
        LoadModels();
    }

    [RelayCommand]
    private void NewConversation()
    {
        ActiveConversationId = null;
        Messages.Clear();
        StreamingContent = "";
        ChatCleared?.Invoke();
    }

    [RelayCommand]
    private void SelectConversation(Conversation conv)
    {
        ActiveConversationId = conv.Id;
        Messages.Clear();
        ChatCleared?.Invoke();

        var messages = _chatService.GetMessages(conv.Id);
        foreach (var msg in messages)
        {
            Messages.Add(msg);
            MessageAdded?.Invoke(msg.Role, msg.Content);
        }
    }

    [RelayCommand]
    private void DeleteConversation(Conversation conv)
    {
        _chatService.DeleteConversation(conv.Id);
        Conversations.Remove(conv);
        if (ActiveConversationId == conv.Id)
            NewConversation();
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessage()
    {
        var content = MessageInput.Trim();
        if (string.IsNullOrEmpty(content)) return;

        // Create conversation if needed
        if (ActiveConversationId == null)
        {
            var title = content.Length > 50 ? content[..47] + "..." : content;
            var conv = _chatService.CreateConversation(title, SelectedProviderId, SelectedModelId);
            ActiveConversationId = conv.Id;
            Conversations.Insert(0, conv);
        }

        // Show user message in UI (ChatService will persist it)
        MessageAdded?.Invoke("user", content);
        MessageInput = "";

        // Stream response
        IsStreaming = true;
        StreamingContent = "";
        _streamCts = new CancellationTokenSource();

        try
        {
            await foreach (var chunk in _chatService.SendMessageStreaming(
                ActiveConversationId, content, SelectedProviderId, SelectedModelId,
                WebSearchEnabled, _streamCts.Token))
            {
                if (chunk.Done) break;
                StreamingContent += chunk.Delta;
                StreamingUpdated?.Invoke(StreamingContent);
            }

            // Show completed assistant message
            MessageAdded?.Invoke("assistant", StreamingContent);

            // Show web sources if any
            if (WebSearchEnabled && _chatService.LastWebResults.Count > 0)
            {
                var sourcesJson = JsonSerializer.Serialize(_chatService.LastWebResults.Select(r => new
                {
                    title = r.Title,
                    url = r.Url,
                    author = r.Author,
                    date = r.PublishedDate?.ToString("yyyy-MM-dd"),
                }));
                WebSourcesReady?.Invoke(sourcesJson);
            }

            StreamingFinished?.Invoke();
            LoadConversations();
        }
        catch (Exception ex)
        {
            MessageAdded?.Invoke("assistant", $"Error: {ex.Message}");
            StreamingFinished?.Invoke();
        }
        finally
        {
            IsStreaming = false;
            StreamingContent = "";
            _streamCts?.Dispose();
            _streamCts = null;
        }
    }

    private bool CanSendMessage() => !IsStreaming && !string.IsNullOrWhiteSpace(MessageInput);

    [RelayCommand]
    private void StopStreaming()
    {
        _streamCts?.Cancel();
    }

    [RelayCommand]
    private void SaveWebResult(int index)
    {
        if (index < 0 || index >= _chatService.LastWebResults.Count) return;

        var result = _chatService.LastWebResults[index];

        try
        {
            var filePath = PdfExporter.Export(result, _docsDir);
            var fileInfo = new FileInfo(filePath);

            var doc = new DocumentInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                MimeType = "application/pdf",
                SizeBytes = fileInfo.Length,
                Status = "processing",
            };

            _db.InsertDocument(doc);
            _ = _processor.ProcessDocumentAsync(doc);

            MessageAdded?.Invoke("assistant", $"Saved \"{result.Title}\" to your library. It will be available for future questions.");
        }
        catch (Exception ex)
        {
            MessageAdded?.Invoke("assistant", $"Failed to save: {ex.Message}");
        }
    }
}
