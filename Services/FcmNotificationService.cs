using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Alerts;
using NetSentinel.API.Models.Settings;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class FcmNotificationService
    : IFcmNotificationService
{
    private readonly NetSentinelDbContext _dbContext;
    private readonly FirebaseSettings _settings;

    public FcmNotificationService(
        NetSentinelDbContext dbContext,
        IOptions<FirebaseSettings> options)
    {
        _dbContext = dbContext;
        _settings = options.Value;
    }

    public async Task SendAlertNotificationAsync(
        AlertResponse alert)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var tokens =
            await _dbContext.FcmTokens
                .Where(x => x.IsActive)
                .Select(x => x.Token)
                .ToListAsync();

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            try
            {
                var message = new Message
                {
                    Token = token,

                    Notification = new Notification
                    {
                        Title = $"NetSentinel Alert: {alert.Severity}",
                        Body =
                            $"{alert.Protocol} alert from " +
                            $"{alert.SourceIp}"
                    },

                    Data = new Dictionary<string, string>
                    {
                        ["alertId"] =
                            alert.Id.ToString(),

                        ["severity"] =
                            alert.Severity,

                        ["sourceIp"] =
                            alert.SourceIp,

                        ["destinationIp"] =
                            alert.DestinationIp,

                        ["protocol"] =
                            alert.Protocol
                    }
                };

                await FirebaseMessaging.DefaultInstance
                    .SendAsync(message);
            }
            catch (FirebaseMessagingException)
            {
                await _dbContext.FcmTokens
                    .Where(x => x.Token == token)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            x => x.IsActive,
                            false));
            }
        }
    }
}