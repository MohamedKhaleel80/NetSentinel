using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Devices;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Models.Enums;
using NetSentinel.API.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace NetSentinel.API.Services;

public class DeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly NetSentinelDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogService _audit;

    public DeviceDiscoveryService(
        NetSentinelDbContext db,
        IConfiguration configuration,
        IAuditLogService audit)
    {
        _db = db;
        _configuration = configuration;
        _audit = audit;
    }

    public async Task<DeviceScanResponse> ScanAsync(
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var subnet = _configuration["DeviceDiscovery:AllowedSubnet"];

        if (string.IsNullOrWhiteSpace(subnet))
        {
            subnet = DetectLocalSubnet();
        }

        var hosts = GetPrivateSubnetHosts(subnet);

        var timeout = int.TryParse(
            _configuration["DeviceDiscovery:PingTimeoutMs"],
            out var configuredTimeout)
            ? Math.Clamp(configuredTimeout, 200, 3000)
            : 600;

        var concurrency = int.TryParse(
            _configuration["DeviceDiscovery:MaxConcurrency"],
            out var configuredConcurrency)
            ? Math.Clamp(configuredConcurrency, 1, 128)
            : 64;

        var startedAt = DateTimeOffset.UtcNow;
        var responding = new ConcurrentBag<(string Ip, string? Hostname)>();

        using var semaphore = new SemaphoreSlim(concurrency);

        var tasks = hosts.Select(async ip =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, timeout);

                if (reply.Status == IPStatus.Success)
                {
                    string? hostname = null;
                    try
                    {
                        var entry = await Dns.GetHostEntryAsync(ip);
                        hostname = entry.HostName;
                    }
                    catch
                    {
                        // Hostname resolution is best effort
                    }

                    responding.Add((ip.ToString(), hostname));
                }
            }
            catch (PingException)
            {
                // A failed ping is not proof that the device is offline.
            }
            catch (SocketException)
            {
                // Continue checking other addresses.
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        cancellationToken.ThrowIfCancellationRequested();

        var discoveredDevices = responding
            .GroupBy(x => x.Ip, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Ip: g.Key, Hostname: g.First().Hostname))
            .OrderBy(x => x.Ip)
            .ToList();

        var discoveredIps = discoveredDevices.Select(x => x.Ip).ToList();

        var macAddresses = await ReadArpTableAsync(cancellationToken);

        var existingDevices = discoveredIps.Count == 0
            ? new List<Device>()
            : await _db.Devices
                .Where(d => discoveredIps.Contains(d.IpAddress))
                .ToListAsync(cancellationToken);

        var existingByIp = existingDevices.ToDictionary(
            d => d.IpAddress,
            StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var added = 0;
        var updated = 0;

        foreach (var (ip, hostname) in discoveredDevices)
        {
            macAddresses.TryGetValue(ip, out var mac);

            if (existingByIp.TryGetValue(ip, out var device))
            {
                if (!string.IsNullOrWhiteSpace(mac))
                {
                    device.MacAddress = mac;
                }

                if (!string.IsNullOrWhiteSpace(hostname) && string.IsNullOrWhiteSpace(device.Hostname))
                {
                    device.Hostname = hostname;
                }

                if (device.Status != DeviceStatus.Monitoring)
                {
                    device.Status = DeviceStatus.Online;
                }

                device.LastSeen = now;
                device.UpdatedAt = now;
                updated++;
            }
            else
            {
                var deviceName = !string.IsNullOrWhiteSpace(hostname)
                    ? hostname
                    : (ip.EndsWith(".1") ? $"Gateway ({ip})" : $"Device {ip}");

                var newDevice = new Device
                {
                    Name = deviceName,
                    IpAddress = ip,
                    MacAddress = mac,
                    Hostname = hostname,
                    DeviceType = ip.EndsWith(".1") ? "Router/Gateway" : null,
                    OperatingSystem = null,
                    Status = DeviceStatus.Online,
                    LastSeen = now,
                    CreatedAt = now
                };

                _db.Devices.Add(newDevice);
                added++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            userId,
            "ScanNetwork",
            "DeviceDiscovery",
            subnet,
            $"Scan completed. Responding: {discoveredIps.Count}, added: {added}, updated: {updated}.",
            null);

        var devices = discoveredIps.Count == 0
            ? new List<DeviceResponse>()
            : await _db.Devices
                .AsNoTracking()
                .Where(d => discoveredIps.Contains(d.IpAddress))
                .OrderBy(d => d.IpAddress)
                .Select(d => new DeviceResponse
                {
                    Id = d.Id,
                    Name = d.Name,
                    IpAddress = d.IpAddress,
                    MacAddress = d.MacAddress,
                    Hostname = d.Hostname,
                    OperatingSystem = d.OperatingSystem,
                    DeviceType = d.DeviceType,
                    Status = d.Status,
                    LastSeen = d.LastSeen,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .ToListAsync(cancellationToken);

        return new DeviceScanResponse
        {
            Subnet = subnet,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            DiscoveredCount = discoveredIps.Count,
            AddedCount = added,
            UpdatedCount = updated,
            Devices = devices
        };
    }

    private static string DetectLocalSubnet()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ua.Address))
                    {
                        var bytes = ua.Address.GetAddressBytes();
                        if (IsPrivateIPv4(bytes))
                        {
                            var mask = ua.IPv4Mask;
                            var prefix = 24;
                            if (mask != null)
                            {
                                var mBytes = mask.GetAddressBytes();
                                uint mUint = ((uint)mBytes[0] << 24) | ((uint)mBytes[1] << 16) | ((uint)mBytes[2] << 8) | mBytes[3];
                                prefix = System.Numerics.BitOperations.PopCount(mUint);
                                if (prefix < 22 || prefix > 30) prefix = 24;
                            }

                            uint ipUint = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
                            var maskUint = uint.MaxValue << (32 - prefix);
                            var netUint = ipUint & maskUint;

                            var netIp = new IPAddress([
                                (byte)(netUint >> 24),
                                (byte)(netUint >> 16),
                                (byte)(netUint >> 8),
                                (byte)netUint
                            ]);

                            return $"{netIp}/{prefix}";
                        }
                    }
                }
            }
        }
        catch
        {
        }

        return "192.168.1.0/24";
    }

    private static List<IPAddress> GetPrivateSubnetHosts(string cidr)
    {
        var raw = cidr.Trim();
        if (!raw.Contains('/'))
        {
            raw = $"{raw}.0/24";
        }

        var parts = raw.Split('/');

        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var address) ||
            address.AddressFamily != AddressFamily.InterNetwork ||
            !int.TryParse(parts[1], out var prefix) ||
            prefix < 20 ||
            prefix > 30)
        {
            raw = DetectLocalSubnet();
            parts = raw.Split('/');
            IPAddress.TryParse(parts[0], out address!);
            int.TryParse(parts[1], out prefix);
        }

        var bytes = address.GetAddressBytes();

        var value =
            ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];

        var mask = uint.MaxValue << (32 - prefix);
        var network = value & mask;

        var count = 1u << (32 - prefix);
        var hosts = new List<IPAddress>();

        for (uint offset = 1; offset < count - 1; offset++)
        {
            var current = network + offset;

            hosts.Add(new IPAddress([
                (byte)(current >> 24),
                (byte)(current >> 16),
                (byte)(current >> 8),
                (byte)current
            ]));
        }

        return hosts;
    }

    private static bool IsPrivateIPv4(byte[] b)
    {
        return b[0] == 10 ||
               (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
               (b[0] == 192 && b[1] == 168);
    }

    private static async Task<Dictionary<string, string>> ReadArpTableAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows())
        {
            return await ReadWindowsArpTableAsync(ct);
        }

        if (File.Exists("/proc/net/arp"))
        {
            try
            {
                var lines = await File.ReadAllLinesAsync("/proc/net/arp", ct);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 && parts[2] != "0x0" && parts[3] != "00:00:00:00:00:00")
                    {
                        result[parts[0]] = parts[3];
                    }
                }
            }
            catch
            {
            }
        }

        return result;
    }

    private static async Task<Dictionary<string, string>> ReadWindowsArpTableAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "arp.exe",
                    Arguments = "-a",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var pattern = @"(?<ip>\d{1,3}(?:\.\d{1,3}){3})\s+(?<mac>(?:[0-9a-fA-F]{2}[-:]){5}[0-9a-fA-F]{2})";

            foreach (Match match in Regex.Matches(output, pattern))
            {
                var ip = match.Groups["ip"].Value;
                var mac = match.Groups["mac"].Value;

                if (IPAddress.TryParse(ip, out var parsed) &&
                    parsed.AddressFamily == AddressFamily.InterNetwork)
                {
                    result[ip] = mac;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // MAC lookup is best-effort
        }

        return result;
    }
}
