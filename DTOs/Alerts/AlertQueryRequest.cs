using NetSentinel.API.Models.Enums;

namespace NetSentinel.API.DTOs.Alerts;

public class AlertQueryRequest
{
    public AlertSeverity? Severity { get; set; }

    public AlertStatus? Status { get; set; }

    public ProtocolType? Protocol { get; set; }

    public string? SourceIp { get; set; }

    public string? DestinationIp { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}