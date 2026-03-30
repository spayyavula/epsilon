using System.Runtime.CompilerServices;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;
using Epsilon.Web.Data;
using Epsilon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Services;

public class WebChatService
{
    private readonly AppDbContext _db;
    private readonly ProviderRegistry _registry;
    private readonly WebDocumentRetriever _retriever;
    private readonly UserKeyStore _keyStore;

    public WebChatService(AppDbContext db, ProviderRegistry registry,
        WebDocumentRetriever retriever, UserKeyStore keyStore)
    {
        _db = db;
        _registry = registry;
        _retriever = retriever;
        _keyStore = keyStore;
    }

    public async Task<ConversationEntity> CreateConversationAsync(Guid userId, string title, string? providerId, string? modelId)
    {
        var conv = new ConversationEntity
        {
            UserId = userId,
            Title = title,
            ProviderId = providerId,
            ModelId = modelId,
        };
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();
        return conv;
    }

    public async Task<List<ConversationEntity>> ListConversationsAsync(Guid userId)
    {
        return await _db.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteConversationAsync(Guid userId, Guid conversationId)
    {
        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
        if (conv == null) return false;
        _db.Conversations.Remove(conv);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RenameConversationAsync(Guid userId, Guid conversationId, string title)
    {
        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
        if (conv == null) return false;
        conv.Title = title;
        conv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<MessageEntity>> GetMessagesAsync(Guid userId, Guid conversationId)
    {
        return await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async IAsyncEnumerable<StreamChunk> SendMessageStreamingAsync(
        Guid userId, Guid conversationId, string content,
        string providerId, string modelId, bool webSearchEnabled,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Save user message
        var userMsg = new MessageEntity
        {
            ConversationId = conversationId,
            UserId = userId,
            Role = "user",
            Content = content,
        };
        _db.Messages.Add(userMsg);
        await _db.SaveChangesAsync(ct);

        // Update conversation timestamp
        var conv = await _db.Conversations.FindAsync(new object[] { conversationId }, ct);
        if (conv != null)
        {
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // Get provider and API key
        var provider = _registry.Get(providerId)
            ?? throw new InvalidOperationException($"Provider '{providerId}' not found.");
        var apiKey = await _keyStore.GetKeyAsync(userId, providerId);

        // Get system prompt
        var systemPrompt = await _db.SystemPrompts
            .Where(s => (s.UserId == userId || s.UserId == null) && s.IsDefault)
            .OrderByDescending(s => s.UserId)
            .Select(s => s.Content)
            .FirstOrDefaultAsync(ct);

        // RAG search
        var ragChunks = await _retriever.SearchAsync(userId, content);
        if (ragChunks.Count > 0)
            systemPrompt = (systemPrompt ?? "") + _retriever.BuildContext(ragChunks);

        // Build message history (last 20)
        var history = await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage
            {
                Id = m.Id.ToString(),
                ConversationId = m.ConversationId.ToString(),
                Role = m.Role,
                Content = m.Content,
                Model = m.Model,
                CreatedAt = m.CreatedAt,
            })
            .ToListAsync(ct);

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
        var assistantMsg = new MessageEntity
        {
            ConversationId = conversationId,
            UserId = userId,
            Role = "assistant",
            Content = fullResponse.ToString(),
            Model = modelId,
        };
        _db.Messages.Add(assistantMsg);
        await _db.SaveChangesAsync(CancellationToken.None);
    }
}
