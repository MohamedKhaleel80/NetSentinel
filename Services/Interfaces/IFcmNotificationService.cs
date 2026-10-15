using NetSentinel.API.DTOs.Alerts;

namespace NetSentinel.API.Services.Interfaces;

public interface IFcmNotificationService
{
    Task SendAlertNotificationAsync(
        AlertResponse alert);
}