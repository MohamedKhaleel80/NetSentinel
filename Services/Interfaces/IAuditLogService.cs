using NetSentinel.API.DTOs.AuditLogs;

namespace NetSentinel.API.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        int? userId,
        string action,
        string entityName,
        string? entityId = null,
        string? description = null,
        string? ipAddress = null);

    Task<List<AuditLogResponse>> GetAllAsync();

    Task<List<AuditLogResponse>> GetByUserIdAsync(
        int userId);
}