using Microsoft.AspNetCore.SignalR;
using NetSentinel.API.DTOs.Alerts;
using NetSentinel.API.Hubs;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class AlertNotificationService
    : IAlertNotificationService
{
    private readonly IHubContext<AlertHub> _hubContext;
    private readonly IFcmNotificationService _fcmService;

    public AlertNotificationService(
        IHubContext<AlertHub> hubContext,
        IFcmNotificationService fcmService)
    {
        _hubContext = hubContext;
        _fcmService = fcmService;
    }

    public async Task NotifyNewAlertAsync(
        AlertResponse alert)
    {
        await _hubContext.Clients
            .All
            .SendAsync(
                "NewAlert",
                alert);

        await _fcmService
            .SendAlertNotificationAsync(alert);
    }
}