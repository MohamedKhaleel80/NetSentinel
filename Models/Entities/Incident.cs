using NetSentinel.API.Models.Enums;
using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Models.Entities;

public class Incident
{
    public int Id { get; set; }

    public long AlertId { get; set; }

    public Alert Alert { get; set; } = null!;

    public string SourceIp { get; set; } = string.Empty;

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public string? Description { get; set; }

    public int? AssignedToUserId { get; set; }

    public ApplicationUser? AssignedToUser { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public ICollection<BlockedIp> BlockedIps { get; set; }
        = new List<BlockedIp>();
}