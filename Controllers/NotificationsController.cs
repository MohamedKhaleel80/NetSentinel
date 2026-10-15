using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Notifications;
using NetSentinel.API.Models.Entities;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NetSentinelDbContext _dbContext;

    public NotificationsController(
        NetSentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("fcm-token")]
    public async Task<IActionResult> RegisterToken(
        [FromBody] RegisterFcmTokenRequest request)
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdClaim,
            out var userId))
        {
            return Unauthorized();
        }

        var existing =
            await _dbContext.FcmTokens
                .FirstOrDefaultAsync(
                    x => x.Token == request.Token);

        if (existing != null)
        {
            existing.UserId = userId;
            existing.DeviceIdentifier =
                request.DeviceIdentifier;
            existing.Platform =
                request.Platform;
            existing.IsActive = true;
            existing.UpdatedAt =
                DateTimeOffset.UtcNow;
        }
        else
        {
            var token = new FcmToken
            {
                UserId = userId,
                Token = request.Token,
                DeviceIdentifier =
                    request.DeviceIdentifier,
                Platform = request.Platform,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.FcmTokens.Add(token);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "FCM token registered successfully."
        });
    }

    [HttpDelete("fcm-token")]
    public async Task<IActionResult> RemoveToken(
        [FromBody] RegisterFcmTokenRequest request)
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdClaim,
            out var userId))
        {
            return Unauthorized();
        }

        var token =
            await _dbContext.FcmTokens
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Token == request.Token);

        if (token == null)
        {
            return NotFound();
        }

        token.IsActive = false;
        token.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "FCM token deactivated."
        });
    }
}