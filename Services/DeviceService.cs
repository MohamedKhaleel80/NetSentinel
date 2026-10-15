using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Devices;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class DeviceService : IDeviceService
{
    private readonly NetSentinelDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public DeviceService(
        NetSentinelDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public async Task<DeviceResponse> CreateAsync(
        CreateDeviceRequest request,
        int? userId)
    {
        var exists =
            await _dbContext.Devices.AnyAsync(
                x => x.IpAddress == request.IpAddress);

        if (exists)
        {
            throw new InvalidOperationException(
                "A device with this IP address already exists.");
        }

        var device = new Device
        {
            Name = request.Name,
            IpAddress = request.IpAddress,
            MacAddress = request.MacAddress,
            Hostname = request.Hostname,
            OperatingSystem = request.OperatingSystem,
            DeviceType = request.DeviceType,
            Status = Models.Enums.DeviceStatus.Unknown,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Devices.Add(device);

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId,
            "CreateDevice",
            "Device",
            device.Id.ToString(),
            $"Device {device.Name} was created.",
            null);

        return await BuildResponseAsync(device.Id);
    }

    public async Task<List<DeviceResponse>> GetAllAsync()
    {
        return await _dbContext.Devices
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DeviceResponse
            {
                Id = x.Id,
                Name = x.Name,
                IpAddress = x.IpAddress,
                MacAddress = x.MacAddress,
                Hostname = x.Hostname,
                OperatingSystem = x.OperatingSystem,
                DeviceType = x.DeviceType,
                Status = x.Status,
                LastSeen = x.LastSeen,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<DeviceResponse?> GetByIdAsync(
        int id)
    {
        return await _dbContext.Devices
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DeviceResponse
            {
                Id = x.Id,
                Name = x.Name,
                IpAddress = x.IpAddress,
                MacAddress = x.MacAddress,
                Hostname = x.Hostname,
                OperatingSystem = x.OperatingSystem,
                DeviceType = x.DeviceType,
                Status = x.Status,
                LastSeen = x.LastSeen,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DeviceResponse> UpdateAsync(
        int id,
        UpdateDeviceRequest request,
        int? userId)
    {
        var device =
            await _dbContext.Devices
                .FirstOrDefaultAsync(x => x.Id == id);

        if (device == null)
        {
            throw new KeyNotFoundException(
                "Device not found.");
        }

        var duplicate =
            await _dbContext.Devices.AnyAsync(
                x => x.Id != id &&
                     x.IpAddress == request.IpAddress);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "Another device already uses this IP address.");
        }

        device.Name = request.Name;
        device.IpAddress = request.IpAddress;
        device.MacAddress = request.MacAddress;
        device.Hostname = request.Hostname;
        device.OperatingSystem = request.OperatingSystem;
        device.DeviceType = request.DeviceType;
        device.Status = request.Status;
        device.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId,
            "UpdateDevice",
            "Device",
            device.Id.ToString(),
            $"Device {device.Name} was updated.",
            null);

        return await BuildResponseAsync(device.Id);
    }

    public async Task DeleteAsync(
        int id,
        int? userId)
    {
        var device =
            await _dbContext.Devices
                .FirstOrDefaultAsync(x => x.Id == id);

        if (device == null)
        {
            throw new KeyNotFoundException(
                "Device not found.");
        }

        _dbContext.Devices.Remove(device);

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId,
            "DeleteDevice",
            "Device",
            id.ToString(),
            $"Device {device.Name} was deleted.",
            null);
    }

    private async Task<DeviceResponse> BuildResponseAsync(
        int id)
    {
        var result = await GetByIdAsync(id);

        return result!;
    }
}