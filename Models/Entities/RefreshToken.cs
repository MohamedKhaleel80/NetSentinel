using System.ComponentModel.DataAnnotations.Schema;
using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Models.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    [NotMapped]
    public bool IsActive =>
        RevokedAt == null &&
        ExpiresAt > DateTimeOffset.UtcNow;
}