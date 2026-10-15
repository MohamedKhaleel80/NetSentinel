namespace NetSentinel.API.DTOs.Auth;

public class AuthResponse
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset AccessTokenExpiresAt { get; set; }

    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
}