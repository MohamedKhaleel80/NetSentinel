using NetSentinel.API.Models.Enums;
using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Models.Entities;

public class BlockedIp
{
    public int Id { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public int IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public BlockActionStatus Status { get; set; }
        = BlockActionStatus.Pending;

    public string? Reason { get; set; }

    public DateTimeOffset? BlockedAt { get; set; }

    public DateTimeOffset? UnblockedAt { get; set; }

    public int? RequestedByUserId { get; set; }

    public ApplicationUser? RequestedByUser { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}