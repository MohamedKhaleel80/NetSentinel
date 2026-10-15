using NetSentinel.API.DTOs.Alerts;

namespace NetSentinel.API.Services.Interfaces;

public interface IAlertNotificationService
{
    Task NotifyNewAlertAsync(AlertResponse alert);
}