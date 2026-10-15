using Microsoft.AspNetCore.Identity;
using NetSentinel.API.Models.Entities;

namespace NetSentinel.API.Models.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();

    public ICollection<FcmToken> FcmTokens { get; set; }
        = new List<FcmToken>();

    public ICollection<AuditLog> AuditLogs { get; set; }
        = new List<AuditLog>();

    public ICollection<Incident> AssignedIncidents { get; set; }
        = new List<Incident>();

    public ICollection<BlockedIp> RequestedBlocks { get; set; }
        = new List<BlockedIp>();
}