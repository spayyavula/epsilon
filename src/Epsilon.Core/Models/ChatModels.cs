namespace Epsilon.Core.Models;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Chat";
    public string? ProviderId { get; set; }
    public string? ModelId { get; set; }
    public string? SystemPromptId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = "";
    public string Role { get; set; } = "user"; // "system", "user", "assistant"
    public string Content { get; set; } = "";
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatRequest
{
    public string Model { get; set; } = "";
    public List<ChatMessage> Messages { get; set; } = new();
    public string? SystemPrompt { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 4096;
}

public class ChatResponse
{
    public string Content { get; set; } = "";
    public string Model { get; set; } = "";
    public string? FinishReason { get; set; }
}

public class StreamChunk
{
    public string Delta { get; set; } = "";
    public bool Done { get; set; }
}

public class ModelInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
}

public class ProviderInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool RequiresApiKey { get; set; }
    public bool IsConnected { get; set; }
}

public class DocumentInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long SizeBytes { get; set; }
    public int? PageCount { get; set; }
    public int ChunkCount { get; set; }
    public string Status { get; set; } = "pending";
    public string? FolderId { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}

public class LibraryFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Path { get; set; } = "";
    public string Label { get; set; } = "";
    public int DocumentCount { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastScannedAt { get; set; }
}

public class RetrievedChunk
{
    public string Text { get; set; } = "";
    public string DocumentId { get; set; } = "";
    public string FileName { get; set; } = "";
}

public class SystemPrompt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string? Domain { get; set; }
    public string Content { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
