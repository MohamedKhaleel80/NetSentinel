using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetSentinel.API.Models.Identity;
using NetSentinel.API.Models.Settings;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtService(
        IOptions<JwtSettings> options,
        UserManager<ApplicationUser> userManager)
    {
        _settings = options.Value;
        _userManager = userManager;
    }

    public async Task<string> GenerateAccessTokenAsync(
        ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty),

            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                user.DisplayName ?? user.Email ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.SecretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiresAt = GetAccessTokenExpiration();

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }

    public DateTimeOffset GetAccessTokenExpiration()
    {
        return DateTimeOffset.UtcNow.AddMinutes(
            _settings.AccessTokenExpirationMinutes);
    }

    public DateTimeOffset GetRefreshTokenExpiration()
    {
        return DateTimeOffset.UtcNow.AddDays(
            _settings.RefreshTokenExpirationDays);
    }
}