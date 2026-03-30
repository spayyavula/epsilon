namespace Epsilon.Web.Contracts;

public record SaveApiKeyRequest(string ApiKey);
public record ApiKeyStatusDto(string ProviderId, bool IsConfigured, DateTime? UpdatedAt);
public record ProviderDto(string Id, string Name, bool RequiresApiKey);
public record ModelDto(string Id, string Name, string Provider);
