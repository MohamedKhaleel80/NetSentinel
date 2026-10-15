using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Models.Entities;

public class FcmToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;

    public string? DeviceIdentifier { get; set; }

    public string? Platform { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}