using NetSentinel.API.DTOs.Alerts;
using NetSentinel.API.DTOs.Common;
using NetSentinel.API.Models.Enums;

namespace NetSentinel.API.Services.Interfaces;

public interface IAlertService
{
    Task<PagedResult<AlertResponse>> GetAlertsAsync(
        AlertQueryRequest request);

    Task<AlertResponse?> GetAlertByIdAsync(long id);

    Task<AlertResponse?> UpdateStatusAsync(long id, AlertStatus status, int? userId);
}