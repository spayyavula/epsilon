namespace Epsilon.Web.Contracts;

public record CreateConversationRequest(string Title, string? ProviderId = null, string? ModelId = null);
public record RenameConversationRequest(string Title);
public record SendMessageRequest(string Content, string ProviderId, string ModelId, bool WebSearchEnabled = false);

public record ConversationDto(
    string Id, string Title, string? ProviderId, string? ModelId,
    DateTime CreatedAt, DateTime UpdatedAt);

public record MessageDto(
    string Id, string Role, string Content, string? Model, DateTime CreatedAt);
