using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Auth;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Models.Identity;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NetSentinelDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IAuditLogService _auditLogService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        NetSentinelDbContext dbContext,
        IJwtService jwtService,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtService = jwtService;
        _auditLogService = auditLogService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request)
    {
        var existingUser =
            await _userManager
                .FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            IsActive = true
        };

        var result =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!result.Succeeded)
        {
            var errors =
                string.Join(
                    "; ",
                    result.Errors.Select(
                        x => x.Description));

            throw new InvalidOperationException(
                errors);
        }

        var isFirstUser = !await _dbContext.Users.AnyAsync(u => u.Id != user.Id);
        var defaultRole = isFirstUser ? "Admin" : "SecurityAnalyst";
        await _userManager.AddToRoleAsync(user, defaultRole);

        await _auditLogService.LogAsync(
            user.Id,
            "Register",
            "ApplicationUser",
            user.Id.ToString(),
            "New user account registered.",
            null);

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request)
    {
        var user =
            await _userManager
                .FindByEmailAsync(request.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "This account is inactive.");
        }

        var passwordValid =
            await _userManager
                .CheckPasswordAsync(
                    user,
                    request.Password);

        if (!passwordValid)
        {
            await _userManager
                .AccessFailedAsync(user);

            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        await _userManager
            .ResetAccessFailedCountAsync(user);

        await _auditLogService.LogAsync(
            user.Id,
            "Login",
            "ApplicationUser",
            user.Id.ToString(),
            "User logged in successfully.",
            null);

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request)
    {
        var refreshToken =
            await _dbContext.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Token ==
                         request.RefreshToken);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        if (!refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Refresh token is expired or revoked.");
        }

        var user = refreshToken.User;

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "This account is inactive.");
        }

        refreshToken.RevokedAt =
            DateTimeOffset.UtcNow;

        var newRefreshTokenValue =
            _jwtService.GenerateRefreshToken();

        var newRefreshToken =
            new RefreshToken
            {
                UserId = user.Id,

                Token =
                    newRefreshTokenValue,

                ExpiresAt =
                    _jwtService
                        .GetRefreshTokenExpiration()
            };

        refreshToken.ReplacedByToken =
            newRefreshTokenValue;

        _dbContext.RefreshTokens.Add(
            newRefreshToken);

        await _dbContext.SaveChangesAsync();

        var accessToken =
            await _jwtService
                .GenerateAccessTokenAsync(user);

        await _auditLogService.LogAsync(
            user.Id,
            "RefreshToken",
            "RefreshToken",
            newRefreshToken.Id.ToString(),
            "Access token refreshed successfully.",
            null);

        return new AuthResponse
        {
            UserId = user.Id,
            DisplayName =
                user.DisplayName ?? string.Empty,
            Email =
                user.Email ?? string.Empty,
            AccessToken = accessToken,
            RefreshToken =
                newRefreshTokenValue,
            AccessTokenExpiresAt =
                _jwtService
                    .GetAccessTokenExpiration(),
            RefreshTokenExpiresAt =
                newRefreshToken.ExpiresAt
        };
    }

    private async Task<AuthResponse>
        CreateAuthResponseAsync(
            ApplicationUser user)
    {
        var accessToken =
            await _jwtService
                .GenerateAccessTokenAsync(user);

        var refreshTokenValue =
            _jwtService
                .GenerateRefreshToken();

        var refreshToken =
            new RefreshToken
            {
                UserId = user.Id,

                Token =
                    refreshTokenValue,

                ExpiresAt =
                    _jwtService
                        .GetRefreshTokenExpiration()
            };

        _dbContext.RefreshTokens.Add(
            refreshToken);

        await _dbContext.SaveChangesAsync();

        return new AuthResponse
        {
            UserId = user.Id,

            DisplayName =
                user.DisplayName ?? string.Empty,

            Email =
                user.Email ?? string.Empty,

            AccessToken =
                accessToken,

            RefreshToken =
                refreshTokenValue,

            AccessTokenExpiresAt =
                _jwtService
                    .GetAccessTokenExpiration(),

            RefreshTokenExpiresAt =
                refreshToken.ExpiresAt
        };
    }
}