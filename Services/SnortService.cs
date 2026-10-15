using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Data;
using NetSentinel.API.DTOs.Alerts;
using NetSentinel.API.DTOs.Snort;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Models.Enums;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Services;

public class SnortService : ISnortService
{
    private readonly NetSentinelDbContext _dbContext;
    private readonly IAlertNotificationService _notificationService;
    private readonly ILogger<SnortService> _logger;

    public SnortService(
        NetSentinelDbContext dbContext,
        IAlertNotificationService notificationService,
        ILogger<SnortService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<long> ProcessAlertAsync(
        SnortAlertDto snortAlert)
    {
        if (snortAlert == null)
        {
            throw new ArgumentNullException(
                nameof(snortAlert));
        }

        var source =
            ParseAddress(
                snortAlert.SourceAddress ??
                snortAlert.LegacySourceAddress);

        var destination =
            ParseAddress(
                snortAlert.DestinationAddress ??
                snortAlert.LegacyDestinationAddress);

        var occurredAt =
            ParseTimestamp(
                snortAlert.Timestamp);

        var protocol =
            ParseProtocol(
                snortAlert.Protocol);

        _logger.LogInformation(
            "Received Snort alert from {SourceIp} to {DestinationIp} for rule {SnortRule}.",
            source.Ip,
            destination.Ip,
            snortAlert.Rule);

        var alert = new Alert
        {
            OccurredAt = occurredAt,

            PacketNumber =
                snortAlert.PacketNumber,

            Protocol =
                protocol,

            PacketGenerator =
                snortAlert.PacketGenerator
                ?? string.Empty,

            PacketLength =
                snortAlert.PacketLength,

            Direction =
                snortAlert.Direction
                ?? string.Empty,

            SourceIp =
                source.Ip,

            SourcePort =
                source.Port,

            DestinationIp =
                destination.Ip,

            DestinationPort =
                destination.Port,

            SnortRule =
                snortAlert.Rule
                ?? string.Empty,

            Message =
                snortAlert.Message,

            Action =
                snortAlert.Action
                ?? string.Empty,

            Severity =
                DetermineSeverity(
                    snortAlert.Rule,
                    snortAlert.Message),

            Status =
                AlertStatus.New,

            RawPayload =
                JsonSerializer.Serialize(
                    snortAlert),

            CreatedAt =
                DateTimeOffset.UtcNow
        };

        var matchedDevice = await _dbContext.Devices
            .FirstOrDefaultAsync(d => d.IpAddress == source.Ip || d.IpAddress == destination.Ip);

        if (matchedDevice != null)
        {
            alert.DeviceId = matchedDevice.Id;
            matchedDevice.LastSeen = DateTimeOffset.UtcNow;
        }

        _dbContext.Alerts.Add(alert);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist Snort alert from {SourceIp} to {DestinationIp}.",
                source.Ip,
                destination.Ip);
            throw;
        }

        _logger.LogInformation(
            "Persisted Snort alert {AlertId} for rule {SnortRule}.",
            alert.Id,
            alert.SnortRule);

        var alertResponse = new AlertResponse
        {
            Id =
                alert.Id,

            OccurredAt =
                alert.OccurredAt,

            PacketNumber =
                alert.PacketNumber,

            Protocol =
                alert.Protocol.ToString(),

            SourceIp =
                alert.SourceIp,

            SourcePort =
                alert.SourcePort,

            DestinationIp =
                alert.DestinationIp,

            DestinationPort =
                alert.DestinationPort,

            SnortRule =
                alert.SnortRule,

            Action =
                alert.Action,

            Message =
                alert.Message,

            Severity =
                alert.Severity.ToString(),

            Status =
                alert.Status.ToString(),

            CreatedAt =
                alert.CreatedAt
        };

        try
        {
            await _notificationService
                .NotifyNewAlertAsync(
                    alertResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Snort alert {AlertId} was persisted, but downstream notification delivery failed.",
                alert.Id);
        }

        return alert.Id;
    }

    // =====================================================
    // Parse IP + Port
    // =====================================================

    private static (
        string Ip,
        int? Port) ParseAddress(
        string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return (
                string.Empty,
                null);
        }

        address = address.Trim();

        // IPv6 format:
        // [2001:db8::1]:443
        if (address.StartsWith("["))
        {
            var closingBracket =
                address.IndexOf(']');

            if (closingBracket > 0)
            {
                var ip =
                    address.Substring(
                        1,
                        closingBracket - 1);

                int? port = null;

                if (
                    address.Length >
                    closingBracket + 1 &&
                    address[closingBracket + 1] == ':')
                {
                    var portText =
                        address[
                            (closingBracket + 2)..];

                    if (int.TryParse(
                        portText,
                        out var parsedPort))
                    {
                        port = parsedPort;
                    }
                }

                return (
                    ip,
                    port);
            }
        }

        // Normal IPv4:
        // 192.168.1.10:443
        var lastColon =
            address.LastIndexOf(':');

        if (lastColon > 0)
        {
            var possibleIp =
                address[..lastColon];

            var possiblePort =
                address[(lastColon + 1)..];

            if (
                IPAddress.TryParse(
                    possibleIp,
                    out _) &&
                int.TryParse(
                    possiblePort,
                    out var parsedPort))
            {
                return (
                    possibleIp,
                    parsedPort);
            }
        }

        // Address without port
        if (IPAddress.TryParse(
            address,
            out _))
        {
            return (
                address,
                null);
        }

        return (
            address,
            null);
    }

    // =====================================================
    // Parse Timestamp
    // =====================================================

    private static DateTimeOffset ParseTimestamp(
        string? timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return DateTimeOffset.UtcNow;
        }

        // Snort timestamp example:
        // 10/05-07:01:53.373222

        var formats = new[]
        {
            "MM/dd-HH:mm:ss.ffffff",
            "MM/dd-HH:mm:ss.fffff",
            "MM/dd-HH:mm:ss.ffff",
            "MM/dd-HH:mm:ss.fff",
            "MM/dd-HH:mm:ss"
        };

        var currentYear =
            DateTime.UtcNow.Year;

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(
                timestamp,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            {
                parsed =
                    new DateTime(
                        currentYear,
                        parsed.Month,
                        parsed.Day,
                        parsed.Hour,
                        parsed.Minute,
                        parsed.Second,
                        parsed.Millisecond,
                        DateTimeKind.Utc);

                return new DateTimeOffset(
                    parsed);
            }
        }

        return DateTimeOffset.UtcNow;
    }

    // =====================================================
    // Parse Protocol
    // =====================================================

    private static ProtocolType ParseProtocol(
        string? protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
        {
            return ProtocolType.Unknown;
        }

        return protocol
            .Trim()
            .ToUpperInvariant()
            switch
        {
            "TCP" =>
                ProtocolType.TCP,

            "UDP" =>
                ProtocolType.UDP,

            "ICMP" =>
                ProtocolType.ICMP,

            "HTTP" =>
                ProtocolType.HTTP,

            "HTTPS" =>
                ProtocolType.HTTPS,

            "DNS" =>
                ProtocolType.DNS,

            "ARP" =>
                ProtocolType.ARP,

            _ =>
                ProtocolType.Other
        };
    }

    // =====================================================
    // Determine Severity
    // =====================================================

    private static AlertSeverity DetermineSeverity(
        string? rule,
        string? message)
    {
        var text = $"{rule} {message}".ToLowerInvariant();

        if (text.Contains("exploit") || text.Contains("overflow") || text.Contains("root") || text.Contains("rce") || text.Contains("critical"))
        {
            return AlertSeverity.Critical;
        }

        if (text.Contains("dos") || text.Contains("denial") || text.Contains("attack") || text.Contains("trojan") || text.Contains("malware") || text.Contains("injection") || text.Contains("high"))
        {
            return AlertSeverity.High;
        }

        if (text.Contains("scan") || text.Contains("sweep") || text.Contains("recon") || text.Contains("suspicious") || text.Contains("medium"))
        {
            return AlertSeverity.Medium;
        }

        if (rule != null && rule.StartsWith("1:1000001"))
        {
            return AlertSeverity.Low;
        }

        return AlertSeverity.Low;
    }
}