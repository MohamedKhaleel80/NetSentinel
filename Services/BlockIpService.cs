using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.BlockedIps;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Models.Enums;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class BlockIpService : IBlockIpService
{
    private readonly NetSentinelDbContext _dbContext;
    private readonly IFirewallService _firewallService;
    private readonly IAuditLogService _auditLogService;

    public BlockIpService(
        NetSentinelDbContext dbContext,
        IFirewallService firewallService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _firewallService = firewallService;
        _auditLogService = auditLogService;
    }

    public async Task<BlockedIpResponse> BlockAsync(
        int incidentId,
        BlockIpRequest request,
        int? requestedByUserId)
    {
        var incident = await _dbContext.Incidents
            .FirstOrDefaultAsync(
                x => x.Id == incidentId);

        if (incident == null)
        {
            throw new KeyNotFoundException(
                "Incident not found.");
        }

        if (string.IsNullOrWhiteSpace(
            incident.SourceIp))
        {
            throw new InvalidOperationException(
                "The incident does not contain a valid source IP.");
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException(
                "Cannot block an IP for a closed incident.");
        }

        var existingBlock =
            await _dbContext.BlockedIps
                .FirstOrDefaultAsync(
                    x =>
                        x.IncidentId == incidentId &&
                        (x.Status ==
                             BlockActionStatus.Pending ||
                         x.Status ==
                             BlockActionStatus.Blocked));

        if (existingBlock != null)
        {
            throw new InvalidOperationException(
                "This incident already has an active block action.");
        }

        var blockedIp = new BlockedIp
        {
            IpAddress = incident.SourceIp,
            IncidentId = incident.Id,
            Status = BlockActionStatus.Pending,
            Reason = request.Reason,
            RequestedByUserId = requestedByUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.BlockedIps.Add(blockedIp);

        await _dbContext.SaveChangesAsync();

        var firewallSuccess =
            await _firewallService.BlockIpAsync(
                blockedIp.IpAddress);

        if (firewallSuccess)
        {
            blockedIp.Status =
                BlockActionStatus.Blocked;

            blockedIp.BlockedAt =
                DateTimeOffset.UtcNow;

            incident.Status =
                IncidentStatus.Contained;

            incident.UpdatedAt =
                DateTimeOffset.UtcNow;
        }
        else
        {
            blockedIp.Status =
                BlockActionStatus.Failed;
        }

        await _dbContext.SaveChangesAsync();

        if (firewallSuccess)
        {
            await _auditLogService.LogAsync(
                requestedByUserId,
                "BlockIp",
                "BlockedIp",
                blockedIp.Id.ToString(),
                $"IP address {blockedIp.IpAddress} was blocked " +
                $"for Incident #{incident.Id}.",
                null);
        }
        else
        {
            await _auditLogService.LogAsync(
                requestedByUserId,
                "BlockIpFailed",
                "BlockedIp",
                blockedIp.Id.ToString(),
                $"Failed to block IP address " +
                $"{blockedIp.IpAddress} for Incident #{incident.Id}.",
                null);
        }

        return await BuildResponseAsync(
            blockedIp.Id);
    }

    public async Task<BlockedIpResponse> UnblockAsync(
        int blockedIpId,
        int? requestedByUserId)
    {
        var blockedIp =
            await _dbContext.BlockedIps
                .FirstOrDefaultAsync(
                    x => x.Id == blockedIpId);

        if (blockedIp == null)
        {
            throw new KeyNotFoundException(
                "Blocked IP record not found.");
        }

        if (blockedIp.Status !=
            BlockActionStatus.Blocked)
        {
            throw new InvalidOperationException(
                "Only an active blocked IP can be unblocked.");
        }

        var firewallSuccess =
            await _firewallService.UnblockIpAsync(
                blockedIp.IpAddress);

        if (!firewallSuccess)
        {
            await _auditLogService.LogAsync(
                requestedByUserId,
                "UnblockIpFailed",
                "BlockedIp",
                blockedIp.Id.ToString(),
                $"Failed to unblock IP address " +
                $"{blockedIp.IpAddress}.",
                null);

            throw new InvalidOperationException(
                "Failed to unblock the IP address.");
        }

        blockedIp.Status =
            BlockActionStatus.Unblocked;

        blockedIp.UnblockedAt =
            DateTimeOffset.UtcNow;

        var incident =
            await _dbContext.Incidents
                .FirstOrDefaultAsync(
                    x => x.Id == blockedIp.IncidentId);

        if (incident != null &&
            incident.Status == IncidentStatus.Contained)
        {
            incident.Status =
                IncidentStatus.InProgress;

            incident.UpdatedAt =
                DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            requestedByUserId,
            "UnblockIp",
            "BlockedIp",
            blockedIp.Id.ToString(),
            $"IP address {blockedIp.IpAddress} was unblocked.",
            null);

        return await BuildResponseAsync(
            blockedIp.Id);
    }

    public async Task<List<BlockedIpResponse>> GetAllAsync()
    {
        return await _dbContext.BlockedIps
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BlockedIpResponse
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                IncidentId = x.IncidentId,
                Status = x.Status.ToString(),
                Reason = x.Reason,
                BlockedAt = x.BlockedAt,
                UnblockedAt = x.UnblockedAt,
                RequestedByUserId =
                    x.RequestedByUserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<BlockedIpResponse?> GetByIdAsync(
        int id)
    {
        return await _dbContext.BlockedIps
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BlockedIpResponse
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                IncidentId = x.IncidentId,
                Status = x.Status.ToString(),
                Reason = x.Reason,
                BlockedAt = x.BlockedAt,
                UnblockedAt = x.UnblockedAt,
                RequestedByUserId =
                    x.RequestedByUserId,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    private async Task<BlockedIpResponse> BuildResponseAsync(
        int id)
    {
        var result =
            await GetByIdAsync(id);

        return result!;
    }
}