using NetSentinel.API.Models.Enums;

namespace NetSentinel.API.Models.Entities;

public class Alert
{
    public long Id { get; set; }

    // Time when Snort detected the event
    public DateTimeOffset OccurredAt { get; set; }

    // Snort packet number
    public long PacketNumber { get; set; }

    public ProtocolType Protocol { get; set; } = ProtocolType.Unknown;

    // Example: raw
    public string PacketGenerator { get; set; } = string.Empty;

    public int PacketLength { get; set; }

    // Example: C2S / S2C
    public string Direction { get; set; } = string.Empty;

    public string SourceIp { get; set; } = string.Empty;

    public int? SourcePort { get; set; }

    public string DestinationIp { get; set; } = string.Empty;

    public int? DestinationPort { get; set; }

    // Example: 1:1000001:1
    public string SnortRule { get; set; } = string.Empty;

    // Example: allow
    public string Action { get; set; } = string.Empty;

    // Human-readable rule message
    public string? Message { get; set; }

    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    public AlertStatus Status { get; set; } = AlertStatus.New;

    // Original Snort JSON
    public string? RawPayload { get; set; }

    // Optional related network device
    public int? DeviceId { get; set; }

    public Device? Device { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Incident> Incidents { get; set; }
        = new List<Incident>();
}