namespace NetSentinel.API.DTOs.Alerts;

public class AlertResponse
{
    public long Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public long PacketNumber { get; set; }

    public string Protocol { get; set; } = string.Empty;

    public string SourceIp { get; set; } = string.Empty;

    public int? SourcePort { get; set; }

    public string DestinationIp { get; set; } = string.Empty;

    public int? DestinationPort { get; set; }

    public string SnortRule { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}