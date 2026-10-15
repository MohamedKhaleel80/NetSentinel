using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Models.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Description { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}