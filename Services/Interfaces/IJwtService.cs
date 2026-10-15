using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Services.Interfaces;

public interface IJwtService
{
    Task<string> GenerateAccessTokenAsync(
        ApplicationUser user);

    string GenerateRefreshToken();

    DateTimeOffset GetAccessTokenExpiration();

    DateTimeOffset GetRefreshTokenExpiration();
}