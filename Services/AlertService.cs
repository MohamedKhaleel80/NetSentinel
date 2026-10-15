using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Alerts;
using NetSentinel.API.DTOs.Common;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class AlertService : IAlertService
{
    private readonly NetSentinelDbContext _dbContext;
    private readonly IAlertNotificationService _notificationService;

    public AlertService(
        NetSentinelDbContext dbContext,
        IAlertNotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<AlertResponse>> GetAlertsAsync(
        AlertQueryRequest request)
    {
        var page = request.Page < 1
            ? 1
            : request.Page;

        var pageSize = request.PageSize < 1
            ? 20
            : Math.Min(request.PageSize, 100);

        var query = _dbContext.Alerts
            .AsNoTracking()
            .AsQueryable();

        if (request.Severity.HasValue)
        {
            query = query.Where(
                x => x.Severity == request.Severity.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(
                x => x.Status == request.Status.Value);
        }

        if (request.Protocol.HasValue)
        {
            query = query.Where(
                x => x.Protocol == request.Protocol.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SourceIp))
        {
            query = query.Where(
                x => x.SourceIp == request.SourceIp);
        }

        if (!string.IsNullOrWhiteSpace(
            request.DestinationIp))
        {
            query = query.Where(
                x => x.DestinationIp ==
                     request.DestinationIp);
        }

        var totalCount =
            await query.CountAsync();

        var alerts = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AlertResponse
            {
                Id = x.Id,

                OccurredAt =
                    x.OccurredAt,

                PacketNumber =
                    x.PacketNumber,

                Protocol =
                    x.Protocol.ToString(),

                SourceIp =
                    x.SourceIp,

                SourcePort =
                    x.SourcePort,

                DestinationIp =
                    x.DestinationIp,

                DestinationPort =
                    x.DestinationPort,

                SnortRule =
                    x.SnortRule,

                Action =
                    x.Action,

                Message =
                    x.Message,

                Severity =
                    x.Severity.ToString(),

                Status =
                    x.Status.ToString(),

                CreatedAt =
                    x.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<AlertResponse>
        {
            Items = alerts,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AlertResponse?> GetAlertByIdAsync(
        long id)
    {
        return await _dbContext.Alerts
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AlertResponse
            {
                Id = x.Id,

                OccurredAt =
                    x.OccurredAt,

                PacketNumber =
                    x.PacketNumber,

                Protocol =
                    x.Protocol.ToString(),

                SourceIp =
                    x.SourceIp,

                SourcePort =
                    x.SourcePort,

                DestinationIp =
                    x.DestinationIp,

                DestinationPort =
                    x.DestinationPort,

                SnortRule =
                    x.SnortRule,

                Action =
                    x.Action,

                Message =
                    x.Message,

                Severity =
                    x.Severity.ToString(),

                Status =
                    x.Status.ToString(),

                CreatedAt =
                    x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AlertResponse?> UpdateStatusAsync(long id, NetSentinel.API.Models.Enums.AlertStatus status, int? userId)
    {
        var alert = await _dbContext.Alerts.FirstOrDefaultAsync(x => x.Id == id);
        if (alert == null) return null;

        alert.Status = status;
        alert.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return await GetAlertByIdAsync(id);
    }
}