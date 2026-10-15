using System.Collections.Concurrent;
using System.Diagnostics;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class FirewallService : IFirewallService
{
    private static readonly ConcurrentDictionary<string, byte> _blockedIps = new();

    public async Task<bool> BlockIpAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        var added = _blockedIps.TryAdd(ipAddress, 0);

        if (OperatingSystem.IsLinux())
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "iptables",
                        Arguments = $"-A INPUT -s {ipAddress} -j DROP",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }
            catch
            {
                // Best effort on Linux
            }
        }

        return true;
    }

    public async Task<bool> UnblockIpAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        _blockedIps.TryRemove(ipAddress, out _);

        if (OperatingSystem.IsLinux())
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "iptables",
                        Arguments = $"-D INPUT -s {ipAddress} -j DROP",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }
            catch
            {
                // Best effort on Linux
            }
        }

        return true;
    }
}