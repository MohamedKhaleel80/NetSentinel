using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.AuditLogs;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class AuditLogService : IAuditLogService
{
    private readonly NetSentinelDbContext _dbContext;

    public AuditLogService(
        NetSentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        int? userId,
        string action,
        string entityName,
        string? entityId = null,
        string? description = null,
        string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            IpAddress = ipAddress,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<AuditLogResponse>> GetAllAsync()
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName =
                    x.User == null
                        ? null
                        : x.User.DisplayName
                          ?? x.User.Email,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Description = x.Description,
                IpAddress = x.IpAddress,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<AuditLogResponse>> GetByUserIdAsync(
        int userId)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName =
                    x.User == null
                        ? null
                        : x.User.DisplayName
                          ?? x.User.Email,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Description = x.Description,
                IpAddress = x.IpAddress,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }
}