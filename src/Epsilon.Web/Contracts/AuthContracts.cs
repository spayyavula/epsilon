namespace Epsilon.Web.Contracts;

public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshRequest(string RefreshToken);
