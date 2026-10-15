namespace NetSentinel.API.DTOs.BlockedIps;

public class BlockedIpResponse
{
    public int Id { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public int IncidentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset? BlockedAt { get; set; }
    public DateTimeOffset? UnblockedAt { get; set; }
    public int? RequestedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}