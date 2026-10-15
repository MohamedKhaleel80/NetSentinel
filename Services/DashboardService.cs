using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Dashboard;
using NetSentinel.API.Models.Enums;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class DashboardService : IDashboardService
{
    private readonly NetSentinelDbContext _dbContext;

    public DashboardService(
        NetSentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;
        var last24Hours = now.AddHours(-24);

        var totalAlerts =
            await _dbContext.Alerts.CountAsync();

        var criticalAlerts =
            await _dbContext.Alerts.CountAsync(
                x => x.Severity == AlertSeverity.Critical);

        var highAlerts =
            await _dbContext.Alerts.CountAsync(
                x => x.Severity == AlertSeverity.High);

        var mediumAlerts =
            await _dbContext.Alerts.CountAsync(
                x => x.Severity == AlertSeverity.Medium);

        var lowAlerts =
            await _dbContext.Alerts.CountAsync(
                x => x.Severity == AlertSeverity.Low);

        var openIncidents =
            await _dbContext.Incidents.CountAsync(
                x => x.Status == IncidentStatus.Open);

        var inProgressIncidents =
            await _dbContext.Incidents.CountAsync(
                x => x.Status == IncidentStatus.InProgress);

        var containedIncidents =
            await _dbContext.Incidents.CountAsync(
                x => x.Status == IncidentStatus.Contained);

        var resolvedIncidents =
            await _dbContext.Incidents.CountAsync(
                x => x.Status == IncidentStatus.Resolved);

        var blockedIps =
            await _dbContext.BlockedIps.CountAsync(
                x => x.Status == BlockActionStatus.Blocked);

        var onlineDevices =
            await _dbContext.Devices.CountAsync(
                x => x.Status == DeviceStatus.Online ||
                     x.Status == DeviceStatus.Monitoring);

        var offlineDevices =
            await _dbContext.Devices.CountAsync(
                x => x.Status == DeviceStatus.Offline);

        var alertsToday =
            await _dbContext.Alerts.CountAsync(
                x => x.OccurredAt >= today);

        var alertsLast24Hours =
            await _dbContext.Alerts.CountAsync(
                x => x.OccurredAt >= last24Hours);


        var lastEventReceivedAt = await _dbContext.Alerts
           .AsNoTracking()
           .OrderByDescending(x => x.CreatedAt)
           .Select(x => (DateTimeOffset?)x.CreatedAt)
           .FirstOrDefaultAsync();

        var monitoringStatus =
            lastEventReceivedAt.HasValue &&
            lastEventReceivedAt.Value >= now.AddMinutes(-2)
                ? "Receiving Events"
                : "No Recent Events";

        var topAttackingIps =
            await _dbContext.Alerts
                .AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceIp))
                .GroupBy(x => x.SourceIp)
                .Select(x => new TopIpResponse
                {
                    IpAddress = x.Key,
                    Count = x.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

        var topTriggeredRules =
            await _dbContext.Alerts
                .AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.SnortRule))
                .GroupBy(x => x.SnortRule)
                .Select(x => new TopRuleResponse
                {
                    Rule = x.Key,
                    Count = x.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

        return new DashboardResponse
        {
            TotalAlerts = totalAlerts,
            CriticalAlerts = criticalAlerts,
            HighAlerts = highAlerts,
            MediumAlerts = mediumAlerts,
            LowAlerts = lowAlerts,

            OpenIncidents = openIncidents,
            InProgressIncidents = inProgressIncidents,
            ContainedIncidents = containedIncidents,
            ResolvedIncidents = resolvedIncidents,

            BlockedIps = blockedIps,

            OnlineDevices = onlineDevices,
            OfflineDevices = offlineDevices,

            AlertsToday = alertsToday,
            AlertsLast24Hours = alertsLast24Hours,

            MonitoringStatus = monitoringStatus,
            LastEventReceivedAt = lastEventReceivedAt,

            TopAttackingIps = topAttackingIps,
            TopTriggeredRules = topTriggeredRules
        };
    }
}