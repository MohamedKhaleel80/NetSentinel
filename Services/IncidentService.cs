using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Incidents;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Models.Enums;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class IncidentService : IIncidentService
{
    private readonly NetSentinelDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public IncidentService(
        NetSentinelDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public async Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
        int? userId)
    {
        var alert = await _dbContext.Alerts
            .FirstOrDefaultAsync(
                x => x.Id == request.AlertId);

        if (alert == null)
        {
            throw new KeyNotFoundException(
                "Alert not found.");
        }

        var existingIncident =
            await _dbContext.Incidents
                .FirstOrDefaultAsync(
                    x => x.AlertId == request.AlertId);

        if (existingIncident != null)
        {
            throw new InvalidOperationException(
                "An incident already exists for this alert.");
        }

        var incident = new Incident
        {
            AlertId = alert.Id,
            SourceIp = alert.SourceIp,
            Description = request.Description,
            Status = IncidentStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Incidents.Add(incident);

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId,
            "CreateIncident",
            "Incident",
            incident.Id.ToString(),
            $"Incident created for Alert #{alert.Id}.",
            null);

        return await BuildResponseAsync(incident.Id);
    }

    public async Task<List<IncidentResponse>> GetAllAsync()
    {
        return await _dbContext.Incidents
            .AsNoTracking()
            .Include(x => x.AssignedToUser)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new IncidentResponse
            {
                Id = x.Id,
                AlertId = x.AlertId,
                SourceIp = x.SourceIp,
                Status = x.Status.ToString(),
                Description = x.Description,
                AssignedToUserId = x.AssignedToUserId,
                AssignedToUserName =
                    x.AssignedToUser == null
                        ? null
                        : x.AssignedToUser.DisplayName
                          ?? x.AssignedToUser.Email,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                ResolvedAt = x.ResolvedAt
            })
            .ToListAsync();
    }

    public async Task<IncidentResponse?> GetByIdAsync(
        int id)
    {
        return await _dbContext.Incidents
            .AsNoTracking()
            .Include(x => x.AssignedToUser)
            .Where(x => x.Id == id)
            .Select(x => new IncidentResponse
            {
                Id = x.Id,
                AlertId = x.AlertId,
                SourceIp = x.SourceIp,
                Status = x.Status.ToString(),
                Description = x.Description,
                AssignedToUserId = x.AssignedToUserId,
                AssignedToUserName =
                    x.AssignedToUser == null
                        ? null
                        : x.AssignedToUser.DisplayName
                          ?? x.AssignedToUser.Email,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                ResolvedAt = x.ResolvedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IncidentResponse> UpdateStatusAsync(
        int id,
        UpdateIncidentStatusRequest request,
        int? userId)
    {
        var incident =
            await _dbContext.Incidents
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (incident == null)
        {
            throw new KeyNotFoundException(
                "Incident not found.");
        }

        ValidateStatusTransition(
            incident.Status,
            request.Status);

        var oldStatus = incident.Status;

        incident.Status = request.Status;
        incident.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Status == IncidentStatus.Resolved ||
            request.Status == IncidentStatus.Closed)
        {
            incident.ResolvedAt =
                DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId,
            "UpdateIncidentStatus",
            "Incident",
            incident.Id.ToString(),
            $"Status changed from {oldStatus} to {request.Status}.",
            null);

        return await BuildResponseAsync(
            incident.Id);
    }

    public async Task<IncidentResponse> AssignAsync(
        int id,
        AssignIncidentRequest request,
        int? userId)
    {
        var incident =
            await _dbContext.Incidents
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (incident == null)
        {
            throw new KeyNotFoundException(
                "Incident not found.");
        }

        var assignedUser =
            await _dbContext.Users
                .FirstOrDefaultAsync(
                    x => x.Id == request.UserId);

        if (assignedUser == null)
        {
            throw new KeyNotFoundException(
                "User not found.");
        }

        if (!assignedUser.IsActive)
        {
            throw new InvalidOperationException(
                "The selected user is inactive.");
        }

        var previousAssignedUserId =
            incident.AssignedToUserId;

        incident.AssignedToUserId =
            assignedUser.Id;

        incident.UpdatedAt =
            DateTimeOffset.UtcNow;

        if (incident.Status == IncidentStatus.Open)
        {
            incident.Status =
                IncidentStatus.InProgress;
        }

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId,
            "AssignIncident",
            "Incident",
            incident.Id.ToString(),
            $"Incident assigned to User #{assignedUser.Id}. " +
            $"Previous assigned user: " +
            $"{previousAssignedUserId?.ToString() ?? "None"}.",
            null);

        return await BuildResponseAsync(
            incident.Id);
    }

    private async Task<IncidentResponse> BuildResponseAsync(
        int id)
    {
        var result =
            await GetByIdAsync(id);

        return result!;
    }

    private static void ValidateStatusTransition(
        IncidentStatus current,
        IncidentStatus next)
    {
        if (current == next)
        {
            return;
        }

        var valid = current switch
        {
            IncidentStatus.Open =>
                next == IncidentStatus.InProgress,

            IncidentStatus.InProgress =>
                next == IncidentStatus.Contained,

            IncidentStatus.Contained =>
                next == IncidentStatus.Resolved,

            IncidentStatus.Resolved =>
                next == IncidentStatus.Closed,

            IncidentStatus.Closed =>
                false,

            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                $"Invalid incident status transition: " +
                $"{current} -> {next}.");
        }
    }
}