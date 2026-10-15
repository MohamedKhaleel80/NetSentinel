using NetSentinel.API.DTOs.Auth;

namespace NetSentinel.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request);

    Task<AuthResponse> LoginAsync(
        LoginRequest request);

    Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request);
}