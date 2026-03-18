using Epsilon.Core.Database;
using Epsilon.Core.Documents;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;
using Epsilon.Core.WebSearch;

namespace Epsilon.Core.Services;

public class ChatService
{
    private readonly DatabaseService _db;
    private readonly ProviderRegistry _registry;
    private readonly DocumentRetriever _retriever;
    private readonly WebSearchService _webSearch;

    // Exposed so ChatViewModel can show source cards and enable save-to-library
    public List<WebSearchResult> LastWebResults { get; private set; } = new();

    public ChatService(DatabaseService db, ProviderRegistry registry, DocumentRetriever retriever, WebSearchService webSearch)
    {
        _db = db;
        _registry = registry;
        _retriever = retriever;
        _webSearch = webSearch;
    }

    public Conversation CreateConversation(string title, string? providerId = null, string? modelId = null)
    {
        var conv = new Conversation
        {
            Title = title,
            ProviderId = providerId,
            ModelId = modelId,
        };
        _db.CreateConversation(conv);
        return conv;
    }

    public List<Conversation> ListConversations() => _db.ListConversations();

    public void DeleteConversation(string id) => _db.DeleteConversation(id);

    public void RenameConversation(string id, string title) => _db.UpdateConversationTitle(id, title);

    public List<ChatMessage> GetMessages(string conversationId) => _db.GetMessages(conversationId);

    public async IAsyncEnumerable<StreamChunk> SendMessageStreaming(
        string conversationId,
        string content,
        string providerId,
        string modelId,
        bool webSearchEnabled = false,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Save user message
        var userMsg = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = content,
        };
        _db.InsertMessage(userMsg);

        // Get provider
        var provider = _registry.Get(providerId)
            ?? throw new InvalidOperationException($"Provider '{providerId}' not found.");

        // Get API key
        var apiKey = _db.GetSetting($"{providerId}_api_key");

        // Get system prompt
        var systemPrompt = _db.GetDefaultSystemPrompt()?.Content;

        // Run RAG and web search in parallel
        var ragTask = Task.Run(() => _retriever.Search(content), ct);
        var webTask = webSearchEnabled
            ? _webSearch.SearchAsync(content, ct)
            : Task.FromResult(new List<WebSearchResult>());

        await Task.WhenAll(ragTask, webTask);

        var relevantChunks = ragTask.Result;
        LastWebResults = webTask.Result;

        // Inject RAG context
        if (relevantChunks.Count > 0)
        {
            systemPrompt = (systemPrompt ?? "") + _retriever.BuildContext(relevantChunks);
        }

        // Inject web search context
        if (LastWebResults.Count > 0)
        {
            systemPrompt = (systemPrompt ?? "") + _webSearch.BuildContext(LastWebResults);
        }

        // Build history (last 20 messages)
        var history = _db.GetMessages(conversationId);
        var recentMessages = history.TakeLast(20).ToList();

        var request = new ChatRequest
        {
            Model = modelId,
            Messages = recentMessages,
            SystemPrompt = systemPrompt,
            Temperature = 0.7f,
            MaxTokens = 4096,
        };

        // Stream response
        var fullResponse = new System.Text.StringBuilder();

        await foreach (var chunk in provider.StreamAsync(request, apiKey, ct))
        {
            fullResponse.Append(chunk.Delta);
            yield return chunk;

            if (chunk.Done) break;
        }

        // Save assistant message
        var assistantMsg = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "assistant",
            Content = fullResponse.ToString(),
            Model = modelId,
        };
        _db.InsertMessage(assistantMsg);
    }
}
